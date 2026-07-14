using System.Text;
using OrgWiki.Application.Analysis;
using OrgWiki.Domain.Ingestion;

namespace OrgWiki.Infrastructure.Analysis;

public sealed class DeterministicCorpusBuilder
{
    public KnowledgeDiscoveryRequest Build(Guid uploadId, IEnumerable<Document> documents)
    {
        var sources = documents.Where(x => x.ProcessingStatus == DocumentProcessingStatus.Parsed && x.Content is not null)
            .OrderBy(x => x.OriginalPath, StringComparer.Ordinal).ThenBy(x => x.Id).Select(x => new CorpusDocument(x.Id, x.FileName, x.OriginalPath, x.DocumentType.ToString(), x.Content!, x.CharacterCount)).ToList();
        var builder = new StringBuilder("<ORGWIKI_CORPUS>\n");
        foreach (var source in sources)
        {
            builder.Append("<SOURCE id=\"").Append(source.Id).Append("\" file=\"").Append(source.FileName.Replace("\"", "&quot;"))
                .Append("\" path=\"").Append(source.OriginalPath.Replace("\"", "&quot;")).Append("\" type=\"").Append(source.DocumentType)
                .Append("\" characters=\"").Append(source.CharacterCount).Append("\">\n").Append(source.Content).Append("\n</SOURCE>\n");
        }
        builder.Append("</ORGWIKI_CORPUS>");
        return new KnowledgeDiscoveryRequest(uploadId, sources, builder.ToString());
    }
}
