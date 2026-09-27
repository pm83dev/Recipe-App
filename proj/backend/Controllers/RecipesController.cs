using RecipeApi.Data;
using RecipeApi.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using File = System.IO.File;

namespace RecipeApi.Controllers;

[ApiController]
[Route("api")]
public class RecipesController : ControllerBase
{
    private readonly RecipeDbContext _db;
    private readonly string _uploadsDir;

    public RecipesController(RecipeDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _uploadsDir = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(_uploadsDir);
    }

    [HttpGet("recipes")]
    public async Task<ActionResult<List<RecipeDetailDto>>> List([FromQuery] string? search)
    {
        var query = _db.Recipes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => EF.Functions.Like(r.Title, "%" + search + "%")
                || (r.Category != null && EF.Functions.Like(r.Category, "%" + search + "%")));
        var recipes = await query.OrderByDescending(r => r.UpdatedAt)
            .Include(r => r.Ingredients).ThenInclude(i => i.Recipe)
            .Include(r => r.Steps).ThenInclude(s => s.Recipe)
            .Select(r => new RecipeDetailDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                BaseServings = r.BaseServings,
                PrepMinutes = r.PrepMinutes,
                CookMinutes = r.CookMinutes,
                Method = r.Method,
                Category = r.Category,
                ImageUrl = r.ImagePath,
                SourceUrl = r.SourceUrl,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                Ingredients = r.Ingredients.OrderBy(i => i.Order).Select(i => new IngredientDto
                {
                    Name = i.Name,
                    Amount = i.Amount,
                    Unit = i.Unit
                }).ToList(),
                Steps = r.Steps.OrderBy(s => s.Order).Select(s => new StepDto { Text = s.Text }).ToList()
            }).ToListAsync();
        return Ok(recipes);
    }

    [HttpGet("recipes/{id:int}")]
    public async Task<ActionResult<RecipeDetailDto>> Get(int id)
    {
        var r = await _db.Recipes.Include(x => x.Ingredients).Include(x => x.Steps).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        return Ok(ToDto(r));
    }

    [HttpPost("recipes")]
    public async Task<ActionResult<RecipeDetailDto>> Create([FromBody] RecipeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Il titolo e' obbligatorio");

        var recipe = new Recipe
        {
            Title = dto.Title.Trim(),
            Description = dto.Description,
            BaseServings = dto.BaseServings > 0 ? dto.BaseServings : 4,
            PrepMinutes = dto.PrepMinutes,
            CookMinutes = dto.CookMinutes,
            Method = dto.Method,
            Category = dto.Category,
            SourceUrl = dto.SourceUrl
        };
        recipe.Ingredients = dto.Ingredients
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .Select((i, idx) => new Ingredient { Name = i.Name.Trim(), Amount = i.Amount, Unit = i.Unit, Order = idx })
            .ToList();
        recipe.Steps = dto.Steps
            .Where(s => !string.IsNullOrWhiteSpace(s.Text))
            .Select((s, idx) => new Step { Text = s.Text.Trim(), Order = idx })
            .ToList();

        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = recipe.Id }, ToDto(recipe));
    }

    [HttpPut("recipes/{id:int}")]
    public async Task<ActionResult<RecipeDetailDto>> Update(int id, [FromBody] RecipeDto dto)
    {
        var r = await _db.Recipes.Include(x => x.Ingredients).Include(x => x.Steps).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Il titolo e' obbligatorio");

        r.Title = dto.Title.Trim();
        r.Description = dto.Description;
        r.BaseServings = dto.BaseServings > 0 ? dto.BaseServings : 4;
        r.PrepMinutes = dto.PrepMinutes;
        r.CookMinutes = dto.CookMinutes;
        r.Method = dto.Method;
        r.Category = dto.Category;
        r.SourceUrl = dto.SourceUrl;
        if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
            r.ImagePath = Path.GetFileName(dto.ImageUrl);
        r.UpdatedAt = DateTime.UtcNow;

        _db.Ingredients.RemoveRange(r.Ingredients);
        r.Ingredients = dto.Ingredients
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .Select((i, idx) => new Ingredient { Name = i.Name.Trim(), Amount = i.Amount, Unit = i.Unit, Order = idx })
            .ToList();
        _db.Steps.RemoveRange(r.Steps);
        r.Steps = dto.Steps
            .Where(s => !string.IsNullOrWhiteSpace(s.Text))
            .Select((s, idx) => new Step { Text = s.Text.Trim(), Order = idx })
            .ToList();

        await _db.SaveChangesAsync();
        return Ok(ToDto(r));
    }

    [HttpDelete("recipes/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _db.Recipes.Include(x => x.Ingredients).Include(x => x.Steps).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        if (!string.IsNullOrEmpty(r.ImagePath))
        {
            var path = Path.Combine(_uploadsDir, Path.GetFileName(r.ImagePath));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
        _db.Recipes.Remove(r);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("recipes/{id:int}/photo")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> Photo(int id, IFormFile file)
    {
        var r = await _db.Recipes.FindAsync(id);
        if (r == null) return NotFound();
        if (file == null || file.Length == 0) return BadRequest("Nessun file");
        if (file.Length > 15 * 1024 * 1024) return BadRequest("File troppo grande (max 15 MB)");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".gif"))
            ext = ".jpg";
        var name = $"{Guid.NewGuid():N}{ext}";

        if (!string.IsNullOrEmpty(r.ImagePath))
        {
            var old = Path.Combine(_uploadsDir, Path.GetFileName(r.ImagePath));
            if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
        }

        await using var fs = new FileStream(Path.Combine(_uploadsDir, name), FileMode.Create);
        await file.CopyToAsync(fs);
        r.ImagePath = name;
        r.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { imageUrl = $"/uploads/{name}" });
    }

    private static RecipeDetailDto ToDto(Recipe r) => new()
    {
        Id = r.Id,
        Title = r.Title,
        Description = r.Description,
        BaseServings = r.BaseServings,
        PrepMinutes = r.PrepMinutes,
        CookMinutes = r.CookMinutes,
        Method = r.Method,
        Category = r.Category,
        ImageUrl = r.ImagePath,
        SourceUrl = r.SourceUrl,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        Ingredients = r.Ingredients.OrderBy(i => i.Order).Select(i => new IngredientDto
        {
            Name = i.Name,
            Amount = i.Amount,
            Unit = i.Unit
        }).ToList(),
        Steps = r.Steps.OrderBy(s => s.Order).Select(s => new StepDto { Text = s.Text }).ToList()
    };
}


