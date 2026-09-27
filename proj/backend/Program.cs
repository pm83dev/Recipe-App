using RecipeApi.Data;
using RecipeApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var contentRoot = builder.Environment.ContentRootPath;
builder.Services.AddDbContext<RecipeDbContext>(o =>
    o.UseSqlite($"Data Source={Path.Combine(contentRoot, "recipes.db")}"));

builder.Services.AddHttpClient("fetch", c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; RecipeApp/1.0)");
});
var aiSection = builder.Configuration.GetSection("Ai");
builder.Services.AddHttpClient("ai", c =>
{
    if (!string.IsNullOrWhiteSpace(aiSection["BaseUrl"]))
    {
        var baseUrl = aiSection["BaseUrl"]!.TrimEnd('/');
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            // URI senza scheme (es. "100.90.112.22:9000"): assume http
            uri = new Uri("http://" + baseUrl);
        }
        c.BaseAddress = new Uri(uri, "/");
        c.Timeout = TimeSpan.FromSeconds(int.TryParse(aiSection["TimeoutSeconds"], out var t) ? t : 180);
        if (!string.IsNullOrWhiteSpace(aiSection["ApiKey"]))
            c.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", aiSection["ApiKey"]);
    }
});

builder.Services.Configure<AiOptions>(aiSection);
builder.Services.AddSingleton<RecipeExtractor>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecipeDbContext>();
    db.Database.EnsureCreated();
}

var uploadsDir = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsDir);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsDir),
    RequestPath = "/uploads"
});

app.UseCors(p => p.AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.MapOpenApi();

app.MapControllers();

app.Run();
