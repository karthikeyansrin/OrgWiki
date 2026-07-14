using System.Text;
using OrgWiki.Domain.Ingestion;

namespace OrgWiki.Infrastructure.Ingestion;

public sealed class TextDocumentParser() : DocumentParser(DocumentType.Text, "txt")
{
    protected override async Task<string> ParseContentAsync(string filePath, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(filePath, new UTF8Encoding(false, false), detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
