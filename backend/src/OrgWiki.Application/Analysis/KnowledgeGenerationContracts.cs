using OrgWiki.Domain.Analysis;

namespace OrgWiki.Application.Analysis;

public sealed record GenerationSource(Guid Id, string FileName, string OriginalPath, string DocumentType, string Content);
public sealed record GenerationArticleContext(
    DiscoverySuggestedArticle Article,
    DiscoveryDomain Domain,
    IReadOnlyList<DiscoveryTopic> Topics,
    IReadOnlyList<DiscoveryDuplicateGroup> DuplicateGroups,
    IReadOnlyList<DiscoveryConflict> Conflicts,
    IReadOnlyList<DiscoveryOutdatedCandidate> OutdatedCandidates,
    IReadOnlyList<GenerationSource> Sources);
public sealed record KnowledgeGenerationRequest(Guid AnalysisId, IReadOnlyList<GenerationArticleContext> Articles, Guid? GenerationId = null);
public sealed record GeneratedCitation(Guid SourceDocumentId, string EvidenceSnippet);
public sealed record GeneratedArticleDraft(
    string Key,
    string Title,
    string Summary,
    string MarkdownContent,
    string Difficulty,
    int EstimatedReadingMinutes,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> RelatedArticleKeys,
    double Confidence,
    IReadOnlyList<GeneratedCitation> Citations);
public sealed record KnowledgeGenerationResult(IReadOnlyList<GeneratedArticleDraft> Articles);
public sealed record KnowledgeGenerationResponse(KnowledgeGenerationResult Result, ProviderUsage? Usage);
public interface IKnowledgeGenerationProvider
{ Task<KnowledgeGenerationResponse> GenerateAsync(KnowledgeGenerationRequest request, CancellationToken cancellationToken); }
public interface IKnowledgeGenerationValidator
{ void Validate(KnowledgeGenerationResult result, KnowledgeGenerationRequest request); }
public interface IKnowledgeGenerationService
{ Task<KnowledgeGenerationSummary?> StartAsync(Guid analysisId, bool explicitRetry, CancellationToken cancellationToken); Task<KnowledgeGenerationSummary?> GetAsync(Guid generationId, CancellationToken cancellationToken); }
public sealed record KnowledgeGenerationSummary(Guid GenerationId, Guid AnalysisId, string Status, string AiMode, string Model, int InputTokens, int OutputTokens, int TotalTokens, long? DurationMilliseconds, string? ErrorMessage, KnowledgeGenerationResult? Result);
