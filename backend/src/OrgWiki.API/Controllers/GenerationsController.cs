using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrgWiki.Application.Analysis;

namespace OrgWiki.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class GenerationsController(IKnowledgeGenerationService generations) : ControllerBase
{
    [HttpPost("analyses/{analysisId:guid}/generate")]
    public async Task<ActionResult<KnowledgeGenerationSummary>> Start(Guid analysisId, [FromQuery] bool retry = false, CancellationToken cancellationToken = default)
    {
        try { var result = await generations.StartAsync(analysisId, retry, cancellationToken); return result is null ? NotFound() : Ok(result); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        catch (InvalidDataException) { return Problem("OrgWiki couldn't validate the generated articles.", statusCode: 502); }
        catch (Exception) { return Problem("OrgWiki couldn't generate articles for this analysis."); }
    }

    [HttpGet("generations/{generationId:guid}")]
    public async Task<ActionResult<KnowledgeGenerationSummary>> Get(Guid generationId, CancellationToken cancellationToken)
        => await generations.GetAsync(generationId, cancellationToken) is { } result ? Ok(result) : NotFound();
}
