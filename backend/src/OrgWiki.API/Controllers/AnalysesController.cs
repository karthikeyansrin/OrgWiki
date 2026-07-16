using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrgWiki.Application.Analysis;

namespace OrgWiki.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class AnalysesController(IKnowledgeAnalysisService analyses) : ControllerBase
{
    [HttpPost("uploads/{uploadId:guid}/analysis")]
    public async Task<ActionResult<KnowledgeAnalysisResult>> Start(Guid uploadId, [FromQuery] bool retry = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await analyses.StartAsync(uploadId, retry, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        catch (InvalidDataException) { return Problem("OrgWiki couldn't validate the knowledge discovery response.", statusCode: 502); }
        catch (Exception) { return Problem("OrgWiki couldn't complete knowledge analysis for this archive."); }
    }

    [HttpGet("analyses/{analysisId:guid}")]
    public async Task<ActionResult<KnowledgeAnalysisResult>> Get(Guid analysisId, CancellationToken cancellationToken)
        => await analyses.GetAsync(analysisId, cancellationToken) is { } result ? Ok(result) : NotFound();
}
