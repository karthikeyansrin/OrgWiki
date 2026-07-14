using OrgWiki.Domain.Ingestion;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using OrgWiki.Application.Ingestion;

namespace OrgWiki.Infrastructure.Ingestion;

public sealed class PdfDocumentParser(IOptions<IngestionOptions> options) : DocumentParser(DocumentType.Pdf, "pdf")
{
    public override Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var pdf = PdfDocument.Open(filePath);
        var pages = pdf.GetPages().ToList();
        if (pages.Count > options.Value.MaxPdfPages)
            throw new InvalidDataException($"PDF contains {pages.Count} pages. The current MVP supports PDFs up to {options.Value.MaxPdfPages} pages.");
        var content = pages.Select((page, index) => $"[Page {index + 1}]\n\n{page.Text}");
        return Task.FromResult(new ParsedDocument(string.Join("\n\n", content), DocumentType.Pdf, pages.Count));
    }

    protected override Task<string> ParseContentAsync(string filePath, CancellationToken cancellationToken)
        => throw new NotSupportedException("PDF parsing uses the page-aware ParseAsync implementation.");
}
