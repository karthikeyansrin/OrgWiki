using OrgWiki.Application.Ingestion;
using OrgWiki.Domain.Ingestion;

namespace OrgWiki.Infrastructure.Ingestion;

public abstract class DocumentParser(DocumentType documentType, params string[] extensions) : IDocumentParser
{
    public bool Supports(string extension) => extensions.Contains(extension.TrimStart('.'), StringComparer.OrdinalIgnoreCase);

    public virtual async Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
        => new(await ParseContentAsync(filePath, cancellationToken), documentType);

    protected abstract Task<string> ParseContentAsync(string filePath, CancellationToken cancellationToken);
}
