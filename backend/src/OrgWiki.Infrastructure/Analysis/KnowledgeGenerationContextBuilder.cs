using OrgWiki.Application.Analysis;
using OrgWiki.Domain.Ingestion;

namespace OrgWiki.Infrastructure.Analysis;

public sealed class KnowledgeGenerationContextBuilder
{
    public KnowledgeGenerationRequest Build(Guid analysisId, KnowledgeDiscoveryResult discovery, IReadOnlyList<Document> documents)
    {
        var docs = documents.Where(x => x.ProcessingStatus == DocumentProcessingStatus.Parsed && x.Content is not null).ToDictionary(x => x.Id);
        var contexts = discovery.SuggestedArticles.Select(article =>
        {
            var domain = discovery.Domains.Single(x => x.Key == article.DomainKey);
            var topics = discovery.Topics.Where(x => article.TopicKeys.Contains(x.Key)).ToList();
            var duplicates = discovery.DuplicateGroups.Where(x => x.TopicKeys.Intersect(article.TopicKeys).Any()).ToList();
            var conflicts = discovery.Conflicts.Where(x => x.TopicKeys.Intersect(article.TopicKeys).Any()).ToList();
            var outdated = discovery.OutdatedCandidates.Where(x => x.TopicKeys.Intersect(article.TopicKeys).Any()).ToList();
            var sources = article.SourceDocumentIds.Distinct().Select(id => docs.TryGetValue(id, out var document) ? new GenerationSource(id, document.FileName, document.OriginalPath, document.DocumentType.ToString(), document.Content!) : throw new InvalidDataException("Suggested article references a source document outside the upload.")).ToList();
            return new GenerationArticleContext(article, domain, topics, duplicates, conflicts, outdated, sources);
        }).ToList();
        return new KnowledgeGenerationRequest(analysisId, contexts);
    }
}
