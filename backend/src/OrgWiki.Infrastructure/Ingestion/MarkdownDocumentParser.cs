using OrgWiki.Domain.Ingestion;

namespace OrgWiki.Infrastructure.Ingestion;

public sealed class MarkdownDocumentParser() : DocumentParser(DocumentType.Markdown, "md", "markdown")
{
    protected override async Task<string> ParseContentAsync(string filePath, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(filePath);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
