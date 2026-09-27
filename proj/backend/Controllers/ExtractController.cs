using RecipeApi.Dtos;
using RecipeApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace RecipeApi.Controllers;

[ApiController]
[Route("api/extract")]
public class ExtractController : ControllerBase
{
    private readonly RecipeExtractor _extractor;

    public ExtractController(RecipeExtractor extractor)
    {
        _extractor = extractor;
    }

    [HttpPost]
    public async Task<ActionResult<ExtractResult>> Extract([FromBody] ExtractRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text) && string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("Fornire text o url");
        try
        {
            var result = await _extractor.ExtractAsync(request, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = "Estrazione non riuscita: " + ex.Message });
        }
    }
}
