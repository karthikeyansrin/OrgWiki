using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OrgWiki.Domain.Ingestion;
using DomainDocumentType = OrgWiki.Domain.Ingestion.DocumentType;

namespace OrgWiki.Infrastructure.Ingestion;

public sealed class DocxDocumentParser() : DocumentParser(DomainDocumentType.Docx, "docx")
{
    protected override Task<string> ParseContentAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var document = WordprocessingDocument.Open(filePath, false);
        var body = document.MainDocumentPart?.Document.Body
            ?? throw new InvalidDataException("The DOCX document has no readable body.");

        var lines = body.Elements().Select(element => element switch
        {
            Paragraph paragraph => string.Concat(paragraph.Descendants<Text>().Select(text => text.Text)),
            Table table => string.Join(" | ", table.Descendants<TableCell>()
                .Select(cell => string.Join(" ", cell.Descendants<Paragraph>()
                    .Select(p => string.Concat(p.Descendants<Text>().Select(t => t.Text))).Where(x => !string.IsNullOrWhiteSpace(x))))),
            _ => string.Concat(element.Descendants<Text>().Select(text => text.Text))
        }).Where(line => !string.IsNullOrWhiteSpace(line));

        return Task.FromResult(string.Join("\n", lines));
    }
}
