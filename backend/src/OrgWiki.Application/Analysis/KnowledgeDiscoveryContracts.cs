using OrgWiki.Domain.Analysis;

namespace OrgWiki.Application.Analysis;

public sealed record CorpusDocument(Guid Id, string FileName, string OriginalPath, string DocumentType, string Content, int CharacterCount);
public sealed record KnowledgeDiscoveryRequest(Guid UploadId, IReadOnlyList<CorpusDocument> Documents, string CorpusText, Guid? AnalysisId = null);
public sealed record ProviderUsage(int? InputTokens, int? OutputTokens, int? TotalTokens);
public sealed record KnowledgeDiscoveryResponse(KnowledgeDiscoveryResult Result, ProviderUsage? Usage);
public sealed record KnowledgeDiscoveryResult(
    IReadOnlyList<DiscoveryDomain> Domains,
    IReadOnlyList<DiscoveryTopic> Topics,
    IReadOnlyList<DiscoveryRelationship> Relationships,
    IReadOnlyList<DiscoveryDuplicateGroup> DuplicateGroups,
    IReadOnlyList<DiscoveryConflict> Conflicts,
    IReadOnlyList<DiscoveryOutdatedCandidate> OutdatedCandidates,
    IReadOnlyList<DiscoverySuggestedArticle> SuggestedArticles);
public sealed record DiscoveryDomain(string Key, string Name, string Description, double Confidence);
public sealed record DiscoveryTopic(string Key, string Name, string Description, string DomainKey, double Confidence, IReadOnlyList<Guid> SourceDocumentIds);
public sealed record DiscoveryRelationship(string SourceTopicKey, string TargetTopicKey, string Type, string Explanation, double Confidence);
public sealed record DiscoveryDuplicateGroup(string Title, string Explanation, double Confidence, IReadOnlyList<string> TopicKeys, IReadOnlyList<Guid> SourceDocumentIds);
public sealed record DiscoveryConflict(string Title, string Description, IReadOnlyList<string> TopicKeys, string ClaimA, string ClaimB, Guid SourceDocumentIdA, Guid SourceDocumentIdB, string EvidenceSnippetA, string EvidenceSnippetB, string Recommendation, string RecommendationReasoning, double Confidence);
public sealed record DiscoveryOutdatedCandidate(string Description, string Reason, IReadOnlyList<string> TopicKeys, IReadOnlyList<Guid> SourceDocumentIds, double Confidence);
public sealed record DiscoverySuggestedArticle(string Key, string Title, string Summary, string DomainKey, IReadOnlyList<string> TopicKeys, IReadOnlyList<Guid> SourceDocumentIds, string Reason, double Confidence);
public interface IKnowledgeDiscoveryProvider
{ Task<KnowledgeDiscoveryResponse> DiscoverAsync(KnowledgeDiscoveryRequest request, CancellationToken cancellationToken); }
public interface IKnowledgeDiscoveryValidator
{ void Validate(KnowledgeDiscoveryResult result, IReadOnlyList<CorpusDocument> documents); }
public interface IKnowledgeAnalysisService
{ Task<KnowledgeAnalysisResult?> StartAsync(Guid uploadId, bool explicitRetry, CancellationToken cancellationToken); Task<KnowledgeAnalysisResult?> GetAsync(Guid analysisId, CancellationToken cancellationToken); }
public sealed record KnowledgeAnalysisResult(Guid AnalysisId, Guid UploadId, string Status, string AiMode, string Model, int DocumentsAnalyzed, int CorpusCharacterCount, int? InputTokens, int? OutputTokens, int? TotalTokens, long? DurationMilliseconds, string? ErrorMessage, KnowledgeDiscoveryResult? Discovery);
