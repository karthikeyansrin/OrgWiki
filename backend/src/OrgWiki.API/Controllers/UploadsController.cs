using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OrgWiki.Application.Ingestion;

namespace OrgWiki.API.Controllers;

[ApiController]
[Authorize]
[Route("api/uploads")]
public sealed class UploadsController(IIngestionService ingestion, IOptions<IngestionOptions> options) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<IngestionResult>> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return BadRequest(new { error = "Please select a ZIP archive." });
        if (!Path.GetExtension(file.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only ZIP archives are supported." });
        if (file.Length > options.Value.MaxArchiveBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = "The knowledge archive exceeds the 10 MB MVP upload limit." });
        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await ingestion.IngestAsync(Path.GetFileName(file.FileName), stream, cancellationToken));
        }
        catch (InvalidDataException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception) { return Problem("The archive could not be processed."); }
    }

    [HttpGet("{uploadId:guid}")]
    public async Task<ActionResult<IngestionResult>> Get(Guid uploadId, CancellationToken cancellationToken)
        => await ingestion.GetAsync(uploadId, cancellationToken) is { } result ? Ok(result) : NotFound();

    [HttpGet]
    public Task<IReadOnlyList<UploadHistoryItem>> List(CancellationToken cancellationToken)
        => ingestion.ListAsync(cancellationToken);
}
