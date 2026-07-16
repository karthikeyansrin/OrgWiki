using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrgWiki.Application.Analysis;
using OrgWiki.Domain.Analysis;
using OrgWiki.Infrastructure.Persistence;
using OrgWiki.Application.Authentication;

namespace OrgWiki.Infrastructure.Analysis;

public sealed class KnowledgeGenerationService(OrgWikiDbContext db, ICurrentUser currentUser, KnowledgeGenerationContextBuilder contextBuilder, IEnumerable<IKnowledgeGenerationProvider> providers, IKnowledgeGenerationValidator validator, IOptions<KnowledgeAnalysisOptions> options, ILogger<KnowledgeGenerationService> logger) : IKnowledgeGenerationService
{
    public async Task<KnowledgeGenerationSummary?> StartAsync(Guid analysisId, bool explicitRetry, CancellationToken cancellationToken)
    {
        var analysis = await OwnedAnalyses().SingleOrDefaultAsync(x => x.Id == analysisId, cancellationToken);
        if (analysis is null) return null;
        if (analysis.Status != KnowledgeAnalysisStatus.Completed || string.IsNullOrWhiteSpace(analysis.ResultJson)) throw new InvalidOperationException("Knowledge discovery must complete before articles can be generated.");
        var latest = await db.KnowledgeGenerations.Where(x => x.AnalysisId == analysisId).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (latest is { IsCurrent: true } && (latest.Status == KnowledgeGenerationStatus.Processing || latest.Status == KnowledgeGenerationStatus.Completed)) return ToSummary(latest);
        if (latest is { Status: KnowledgeGenerationStatus.Failed } && !explicitRetry) return ToSummary(latest);
        var mode = Enum.TryParse<AiMode>(options.Value.Mode, true, out var parsedMode) ? parsedMode : AiMode.Replay;
        var generation = new KnowledgeGeneration(analysisId, mode, options.Value.Model); generation.Start(); db.KnowledgeGenerations.Add(generation);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            var concurrent = await db.KnowledgeGenerations.Where(x => x.AnalysisId == analysisId && x.IsCurrent).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
            if (concurrent is not null) return ToSummary(concurrent);
            throw;
        }
        var watch = Stopwatch.StartNew();
        try
        {
            var discovery = JsonSerializer.Deserialize<KnowledgeDiscoveryResult>(analysis.ResultJson, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new InvalidDataException("Stored knowledge map is invalid.");
            var uploadDocuments = await db.Documents.Where(x => x.UploadId == analysis.UploadId).ToListAsync(cancellationToken);
            var request = contextBuilder.Build(analysisId, discovery, uploadDocuments) with { GenerationId = generation.Id };
            if (request.Articles.Count == 0) throw new InvalidOperationException("The knowledge map contains no suggested articles.");
            var provider = providers.Single(x => mode == AiMode.Replay ? x is ReplayKnowledgeGenerationProvider : x is OpenAiKnowledgeGenerationProvider);
            var response = await provider.GenerateAsync(request, cancellationToken); // exactly one provider invocation
            generation.RecordUsage(response.Usage?.InputTokens, response.Usage?.OutputTokens, response.Usage?.TotalTokens);
            validator.Validate(response.Result, request);
            if (options.Value.VerboseLogging && mode == AiMode.Live)
            {
                var articles = response.Result.Articles;
                var confidences = articles.Select(article => article.Confidence).ToList();
                logger.LogInformation("Knowledge generation top-level JSON schema validation result: Passed. GenerationId {GenerationId}; article count {ArticleCount}; citation count {CitationCount}; related links {RelatedLinks}; confidence minimum {MinimumConfidence}; confidence maximum {MaximumConfidence}; confidence average {AverageConfidence}", generation.Id, articles.Count, articles.Sum(article => article.Citations.Count), articles.Sum(article => article.RelatedArticleKeys.Count), confidences.Count == 0 ? null : confidences.Min(), confidences.Count == 0 ? null : confidences.Max(), confidences.Count == 0 ? null : confidences.Average());
            }
            var json = JsonSerializer.Serialize(response.Result);
            watch.Stop();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            generation.Complete(json, response.Usage?.InputTokens, response.Usage?.OutputTokens, response.Usage?.TotalTokens, watch.ElapsedMilliseconds);
            foreach (var draft in response.Result.Articles)
            {
                var article = new GeneratedArticle(generation.Id, draft.Key, draft.Title, draft.Summary, draft.MarkdownContent, draft.Difficulty, draft.EstimatedReadingMinutes, JsonSerializer.Serialize(draft.Tags), JsonSerializer.Serialize(draft.RelatedArticleKeys), draft.Confidence);
                db.GeneratedArticles.Add(article);
                foreach (var citation in draft.Citations) db.GeneratedArticleCitations.Add(new GeneratedArticleCitation(article.Id, citation.SourceDocumentId, citation.EvidenceSnippet));
            }
            await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Knowledge generation completed for AnalysisId {AnalysisId}, GenerationId {GenerationId}, mode {AiMode}, model {Model}, input tokens {InputTokens}, output tokens {OutputTokens}, total tokens {TotalTokens}, duration {DurationMilliseconds}ms", analysisId, generation.Id, mode, generation.Model, response.Usage?.InputTokens, response.Usage?.OutputTokens, response.Usage?.TotalTokens, watch.ElapsedMilliseconds);
            return ToSummary(generation);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            watch.Stop(); generation.Fail(ex is InvalidDataException or InvalidOperationException ? ex.Message : "OrgWiki couldn't generate articles for this analysis.", watch.ElapsedMilliseconds); await db.SaveChangesAsync(cancellationToken);
            if (options.Value.VerboseLogging && mode == AiMode.Live && ex is InvalidDataException)
                logger.LogWarning("Knowledge generation top-level JSON schema validation result: Failed. GenerationId {GenerationId}; error {ErrorMessage}", generation.Id, ex.Message);
            logger.LogError("Knowledge generation failed for AnalysisId {AnalysisId}, GenerationId {GenerationId}, mode {AiMode}: {ErrorMessage}", analysisId, generation.Id, mode, ex.Message);
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug(ex, "Knowledge generation failure details for GenerationId {GenerationId}", generation.Id);
            throw;
        }
    }

    public async Task<KnowledgeGenerationSummary?> GetAsync(Guid generationId, CancellationToken cancellationToken)
        => await db.KnowledgeGenerations.Where(x => x.Id == generationId)
            .Join(OwnedAnalyses(), generation => generation.AnalysisId, analysis => analysis.Id, (generation, _) => generation)
            .SingleOrDefaultAsync(cancellationToken) is { } generation ? ToSummary(generation) : null;

    private IQueryable<KnowledgeAnalysis> OwnedAnalyses()
        => db.KnowledgeAnalyses.Where(analysis => db.Uploads.Any(upload => upload.Id == analysis.UploadId && upload.UserId == currentUser.Id));

    static KnowledgeGenerationSummary ToSummary(KnowledgeGeneration generation)
    { KnowledgeGenerationResult? result = string.IsNullOrWhiteSpace(generation.ResultJson) ? null : JsonSerializer.Deserialize<KnowledgeGenerationResult>(generation.ResultJson); return new(generation.Id, generation.AnalysisId, generation.Status.ToString(), generation.AiMode.ToString(), generation.Model, generation.InputTokens ?? 0, generation.OutputTokens ?? 0, generation.TotalTokens ?? 0, generation.DurationMilliseconds, generation.ErrorMessage, result); }
}
