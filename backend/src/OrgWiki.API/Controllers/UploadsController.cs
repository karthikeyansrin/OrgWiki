using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OrgWiki.Application.Ingestion;

namespace OrgWiki.API.Controllers;

[ApiController]
[Authorize]
[Route("api/uploads")]
public sealed class UploadsController(IIngestionService ingestion, IOptions<IngestionOptions> options, ILogger<UploadsController> logger) : ControllerBase
{
    private static readonly HashSet<string> AllowedZipContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/zip",
        "application/x-zip-compressed",
        "application/x-compressed",
        "application/octet-stream"
    };

    [HttpPost]
    [EnableRateLimiting("upload")]
    public async Task<ActionResult<IngestionResult>> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return BadRequest(new { error = "Please select a ZIP archive." });
        if (!Path.GetExtension(file.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only ZIP archives are supported." });
        if (!IsAllowedZipContentType(file.ContentType))
            return BadRequest(new { error = "The uploaded file must have a ZIP content type." });
        if (file.Length > options.Value.MaxArchiveBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = "The knowledge archive exceeds the 10 MB MVP upload limit." });

        var fileName = Path.GetFileName(file.FileName.Replace('\\', '/')).Trim();
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255)
            return BadRequest(new { error = "The uploaded archive name is invalid." });

        try
        {
            await using var stream = file.OpenReadStream();
            if (!await HasZipSignatureAsync(stream, cancellationToken))
                return BadRequest(new { error = "The uploaded file is not a valid ZIP archive." });
            return Ok(await ingestion.IngestAsync(fileName, stream, cancellationToken));
        }
        catch (InvalidDataException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Archive ingestion failed for file {FileName}", fileName);
            return Problem("The archive could not be processed.");
        }
    }

    [HttpGet("{uploadId:guid}")]
    public async Task<ActionResult<IngestionResult>> Get(Guid uploadId, CancellationToken cancellationToken)
        => await ingestion.GetAsync(uploadId, cancellationToken) is { } result ? Ok(result) : NotFound();

    [HttpGet]
    public Task<IReadOnlyList<UploadHistoryItem>> List(CancellationToken cancellationToken)
        => ingestion.ListAsync(cancellationToken);

    private static bool IsAllowedZipContentType(string? contentType)
    {
        var mediaType = contentType?.Split(';', 2)[0].Trim();
        return !string.IsNullOrWhiteSpace(mediaType) && AllowedZipContentTypes.Contains(mediaType);
    }

    private static async Task<bool> HasZipSignatureAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanSeek) return false;

        var originalPosition = stream.Position;
        var header = new byte[4];
        var offset = 0;
        try
        {
            while (offset < header.Length)
            {
                var read = await stream.ReadAsync(header.AsMemory(offset), cancellationToken);
                if (read == 0) return false;
                offset += read;
            }

            return header[0] == 0x50 && header[1] == 0x4B
                && ((header[2] == 0x03 && header[3] == 0x04)
                    || (header[2] == 0x05 && header[3] == 0x06)
                    || (header[2] == 0x07 && header[3] == 0x08));
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }
}
