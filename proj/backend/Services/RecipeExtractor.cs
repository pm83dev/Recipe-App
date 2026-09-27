using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RecipeApi.Dtos;

namespace RecipeApi.Services;

public class AiOptions
{
    public string BaseUrl { get; set; } = "";
    public string Model { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public int MaxTokens { get; set; } = 1024;
    public int TimeoutSeconds { get; set; } = 180;

    public bool Enabled => !string.IsNullOrWhiteSpace(BaseUrl);
}

public class RecipeExtractor
{
    private const string SystemPrompt = """
        You are a recipe data extraction assistant. Read the provided recipe text (it may be raw text or stripped HTML from a webpage) and return ONLY a JSON object, no markdown, no code fences, with exactly this shape:
        {"title": string, "description": string or null, "servings": number, "prep_minutes": number or null, "cook_minutes": number or null, "method": string or null, "category": string or null, "ingredients": [{"name": string, "amount": number or null, "unit": string or null}], "steps": [string]}
        Rules:
        - servings: the number of people the recipe is written for; use 4 if not stated.
        - amount must be a plain number (convert fractions/percentages to decimals, e.g. 1/2 -> 0.5, 10% -> 10).
        - unit: short unit (g, kg, ml, l, pcs, tsp, tbsp, cup, slice...) or null.
        - steps: the cooking procedure, one string per step, in order, no numbering.
        - If a field is not present, use null (or 0 for servings, never omit keys).
        - Respond in the same language as the recipe.
        """;

    private readonly AiOptions _options;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<RecipeExtractor> _logger;

    public RecipeExtractor(IOptions<AiOptions> options, IHttpClientFactory httpFactory, ILogger<RecipeExtractor> logger)
    {
        _options = options.Value;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<ExtractResult> ExtractAsync(ExtractRequest request, CancellationToken ct)
    {
        string text;
        if (!string.IsNullOrWhiteSpace(request.Text))
        {
            text = request.Text!;
        }
        else if (!string.IsNullOrWhiteSpace(request.Url))
        {
            text = await FetchUrlAsync(request.Url!, ct);
        }
        else
        {
            throw new ArgumentException("Né testo né URL forniti");
        }

        if (_options.Enabled)
        {
            try
            {
                var dto = await LlamaExtractAsync(text, ct);
                return new ExtractResult { Engine = "llama", Recipe = dto };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Estrazione con modello locale non riuscita");
                return new ExtractResult
                {
                    Engine = "errore",
                    Recipe = new RecipeDto { Title = "Ricetta (compila manualmente)" },
                    Error = "Il modello IA locale non ha risposto: " + ex.Message
                };
            }
        }

        return new ExtractResult
        {
            Engine = "disabilitato",
            Recipe = new RecipeDto { Title = "Ricetta (compila manualmente)" },
            Error = "IA non configurata: imposta Ai:BaseUrl in appsettings.json"
        };
    }

    private async Task<string> FetchUrlAsync(string url, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient("fetch");
        var html = await client.GetStringAsync(url, ct);
        return HtmlToText(html);
    }

    private static string HtmlToText(string html)
    {
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);
        foreach (var node in new[] { "script", "style", "nav", "footer", "form", "aside" })
        {
            var els = doc.DocumentNode.SelectNodes($"//{node}");
            if (els != null)
                foreach (var el in els)
                    el.Remove();
        }
        var text = doc.DocumentNode.InnerText;
        return string.Join(' ', text.Split(new[] { '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries));
    }

    private async Task<RecipeDto> LlamaExtractAsync(string text, CancellationToken ct)
    {
        var input = text.Length > 12000 ? text[..12000] : text;
        var client = _httpFactory.CreateClient("ai");

        // Primo tentativo con JSON mode; se il server non lo supporta (400) si riprova senza.
        var jsonMode = true;
        string? content = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var payload = JsonSerializer.Serialize(new
            {
                model = string.IsNullOrWhiteSpace(_options.Model) ? (string?)null : _options.Model,
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = $"Recipe text:\n<<<\n{input}\n>>>\nReturn the JSON object now:" }
                },
                temperature = 0,
                max_tokens = _options.MaxTokens,
                response_format = jsonMode ? new { type = "json_object" } : null
            });

            using var resp = await client.PostAsync("chat/completions",
                new StringContent(payload, Encoding.UTF8, "application/json"), ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                if (jsonMode && (int)resp.StatusCode == 400)
                {
                    jsonMode = false;
                    continue;
                }
                throw new InvalidOperationException($"llama-server: {(int)resp.StatusCode} {body[..Math.Min(300, body.Length)]}");
            }

            using var doc = JsonDocument.Parse(body);
            content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            break;
        }

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("Output vuoto dal modello");

        var json = ExtractJson(content!);
        JsonDocument jdoc;
        try
        {
            jdoc = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            // Output troncato (max_tokens) o malformato: chiudi gli oggetti/array aperti
            // e riprova a parsare.
            var repaired = RepairTruncatedJson(json);
            _logger.LogWarning("JSON del modello non valido, riparato: {Repaired}", repaired);
            jdoc = JsonDocument.Parse(repaired);
        }
        using (jdoc)
        {
            var root = jdoc.RootElement;
            return new RecipeDto
            {
                Title = GetStr(root, "title") ?? "Ricetta importata",
                Description = GetStr(root, "description"),
                BaseServings = int.TryParse(GetStr(root, "servings"), out var s) && s > 0 ? s : 4,
                PrepMinutes = GetInt(root, "prep_minutes"),
                CookMinutes = GetInt(root, "cook_minutes"),
                Method = GetStr(root, "method"),
                Category = GetStr(root, "category"),
                Ingredients = ParseIngredients(GetArr(root, "ingredients")),
                Steps = ParseSteps(GetArr(root, "steps"))
            };
        }
    }

    private static string RepairTruncatedJson(string json)
    {
        var stack = new Stack<char>();
        bool inString = false, escaped = false;
        foreach (var c in json)
        {
            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }
            switch (c)
            {
                case '"': inString = true; break;
                case '{': stack.Push('{'); break;
                case '[': stack.Push('['); break;
                case '}': if (stack.Count > 0 && stack.Peek() == '{') stack.Pop(); break;
                case ']': if (stack.Count > 0 && stack.Peek() == '[') stack.Pop(); break;
            }
        }
        // Stringa aperta senza chiusura: chiudila
        if (inString) json += '"';
        // Rimuovi virgola finale orfana
        var trimmed = json.TrimEnd();
        while (trimmed.EndsWith(",")) trimmed = trimmed[..^1].TrimEnd();
        // Chiudi array/oggetti aperti, dal più interno
        foreach (var open in stack)
            trimmed += open == '{' ? "}" : "]";
        return trimmed;
    }

    private static List<IngredientDto> ParseIngredients(JsonElement? arr)
    {
        var list = new List<IngredientDto>();
        if (arr == null) return list;
        foreach (var e in arr.Value.EnumerateArray())
        {
            var name = GetStr(e, "name");
            if (string.IsNullOrWhiteSpace(name)) continue;
            list.Add(new IngredientDto { Name = name, Amount = GetNum(e, "amount"), Unit = GetStr(e, "unit") });
        }
        return list;
    }

    private static List<StepDto> ParseSteps(JsonElement? arr)
    {
        var list = new List<StepDto>();
        if (arr == null) return list;
        foreach (var e in arr.Value.EnumerateArray())
        {
            var s = e.ValueKind == JsonValueKind.String ? e.GetString() : GetStr(e, "text");
            if (!string.IsNullOrWhiteSpace(s)) list.Add(new StepDto { Text = s! });
        }
        return list;
    }

    private static string ExtractJson(string output)
    {
        var start = output.IndexOf('{');
        var end = output.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException("Nessun JSON trovato nell'output del modello");
        return output[start..(end + 1)];
    }

    private static string? GetStr(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int? GetInt(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v)
            ? (v.ValueKind == JsonValueKind.Number ? v.GetInt32()
              : int.TryParse(v.GetString(), out var i) ? i : null)
            : null;

    private static double? GetNum(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v)
            ? (v.ValueKind == JsonValueKind.Number ? v.GetDouble()
              : double.TryParse(v.GetString(), out var d) ? d : null)
            : null;

    private static JsonElement? GetArr(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array ? v : null;
}
