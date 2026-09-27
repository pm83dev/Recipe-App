using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RecipeApi.Data;

public class Recipe
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int BaseServings { get; set; } = 4;
    public int? PrepMinutes { get; set; }
    public int? CookMinutes { get; set; }
    public string? Method { get; set; }
    public string? Category { get; set; }
    public string? ImagePath { get; set; }
    public string? SourceUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Ingredient> Ingredients { get; set; } = [];
    public List<Step> Steps { get; set; } = [];
}

public class Ingredient
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double? Amount { get; set; }
    public string? Unit { get; set; }
    public int Order { get; set; }
    public Recipe? Recipe { get; set; }
}

public class Step
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
    public Recipe? Recipe { get; set; }
}

public class RecipeDbContext : DbContext
{
    public RecipeDbContext(DbContextOptions<RecipeDbContext> options) : base(options) { }

    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Step> Steps => Set<Step>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Ingredient>().HasIndex(i => i.RecipeId);
        b.Entity<Step>().HasIndex(s => s.RecipeId);
    }
}
