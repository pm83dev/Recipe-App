namespace RecipeApi.Dtos;

public class IngredientDto
{
    public string Name { get; set; } = string.Empty;
    public double? Amount { get; set; }
    public string? Unit { get; set; }
}

public class StepDto
{
    public string Text { get; set; } = string.Empty;
}

public class RecipeDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int BaseServings { get; set; } = 4;
    public int? PrepMinutes { get; set; }
    public int? CookMinutes { get; set; }
    public string? Method { get; set; }
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public string? SourceUrl { get; set; }
    public List<IngredientDto> Ingredients { get; set; } = [];
    public List<StepDto> Steps { get; set; } = [];
}

public class RecipeDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int BaseServings { get; set; }
    public int? PrepMinutes { get; set; }
    public int? CookMinutes { get; set; }
    public string? Method { get; set; }
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public string? SourceUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<IngredientDto> Ingredients { get; set; } = [];
    public List<StepDto> Steps { get; set; } = [];
}

public class ExtractRequest
{
    public string? Text { get; set; }
    public string? Url { get; set; }
}

public class ExtractResult
{
    public string? Engine { get; set; }
    public string? Error { get; set; }
    public RecipeDto Recipe { get; set; } = new();
}
