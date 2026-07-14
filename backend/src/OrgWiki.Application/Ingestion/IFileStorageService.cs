namespace OrgWiki.Application.Ingestion;

public interface IFileStorageService
{
    Task<string> SaveArchiveAsync(string fileName, Stream content, CancellationToken cancellationToken);
}
