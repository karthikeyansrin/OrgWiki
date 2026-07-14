using Microsoft.Extensions.Options;
using OrgWiki.Application.Ingestion;

namespace OrgWiki.Infrastructure.Storage;

public sealed class LocalFileStorageService(IOptions<IngestionOptions> options) : IFileStorageService
{
    public async Task<string> SaveArchiveAsync(string fileName, Stream content, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(options.Value.LocalStoragePath);
        Directory.CreateDirectory(root);
        var key = $"{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        var path = Path.Combine(root, key);
        await using var output = File.Create(path);
        await content.CopyToAsync(output, cancellationToken);
        return key;
    }
}
