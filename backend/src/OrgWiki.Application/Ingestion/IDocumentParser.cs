using OrgWiki.Domain.Ingestion;

namespace OrgWiki.Application.Ingestion;

public sealed record ParsedDocument(string Content, DocumentType DocumentType, int? PageCount = null);

public interface IDocumentParser
{
    bool Supports(string extension);
    Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken);
}
