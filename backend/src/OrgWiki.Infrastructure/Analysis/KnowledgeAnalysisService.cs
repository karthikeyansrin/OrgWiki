using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrgWiki.Application.Analysis;
using OrgWiki.Domain.Analysis;
using OrgWiki.Domain.Ingestion;
using OrgWiki.Infrastructure.Persistence;
using OrgWiki.Application.Authentication;

namespace OrgWiki.Infrastructure.Analysis;

public sealed class KnowledgeAnalysisService(OrgWikiDbContext db, ICurrentUser currentUser, DeterministicCorpusBuilder corpusBuilder, IEnumerable<IKnowledgeDiscoveryProvider> providers, IKnowledgeDiscoveryValidator validator, IOptions<KnowledgeAnalysisOptions> options, ILogger<KnowledgeAnalysisService> logger) : IKnowledgeAnalysisService
{
    public async Task<KnowledgeAnalysisResult?> StartAsync(Guid uploadId, bool explicitRetry, CancellationToken cancellationToken)
    {
        var upload = await db.Uploads.Include(x => x.Documents).SingleOrDefaultAsync(x => x.Id == uploadId && x.UserId == currentUser.Id, cancellationToken);
        if (upload is null) return null;
        if (!upload.IsEligibleForAnalysis) throw new InvalidOperationException(upload.AnalysisEligibilityReason ?? "This upload is not eligible for analysis.");
        var latest = await db.KnowledgeAnalyses.Where(x => x.UploadId == uploadId).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        var existing = latest is { IsCurrent: true } ? latest : null;
        if (existing is { Status: KnowledgeAnalysisStatus.Completed } || existing is { Status: KnowledgeAnalysisStatus.Processing }) return ToResult(existing, upload.Documents.Count(x => x.ProcessingStatus == DocumentProcessingStatus.Parsed), upload.TotalCharacterCount);
        if (latest is { Status: KnowledgeAnalysisStatus.Failed } && !explicitRetry) return ToResult(latest, 0, upload.TotalCharacterCount);
        var mode = Enum.TryParse<AiMode>(options.Value.Mode, true, out var parsedMode) ? parsedMode : AiMode.Replay;
        var analysis = new KnowledgeAnalysis(uploadId, mode, options.Value.Model); analysis.Start(); db.KnowledgeAnalyses.Add(analysis);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            var concurrent = await db.KnowledgeAnalyses.Where(x => x.UploadId == uploadId && x.IsCurrent).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
            if (concurrent is not null) return ToResult(concurrent, upload.Documents.Count(x => x.ProcessingStatus == DocumentProcessingStatus.Parsed), upload.TotalCharacterCount);
            throw;
        }
        var watch = Stopwatch.StartNew();
        try
        {
            var request = corpusBuilder.Build(uploadId, upload.Documents) with { AnalysisId = analysis.Id };
            if (request.Documents.Count == 0) throw new InvalidOperationException("This upload has no successfully parsed documents.");
            var provider = providers.Single(x => mode == AiMode.Replay ? x is ReplayKnowledgeDiscoveryProvider : x is OpenAiKnowledgeDiscoveryProvider);
            var response = await provider.DiscoverAsync(request, cancellationToken); // exactly one provider invocation
            analysis.RecordUsage(response.Usage?.InputTokens, response.Usage?.OutputTokens, response.Usage?.TotalTokens);
            validator.Validate(response.Result, request.Documents);
            if (options.Value.VerboseLogging && mode == AiMode.Live)
            {
                logger.LogInformation("Knowledge discovery top-level JSON schema validation result: Passed. AnalysisId {AnalysisId}; domains {Domains}; topics {Topics}; relationships {Relationships}; duplicate groups {DuplicateGroups}; conflicts {Conflicts}; outdated candidates {OutdatedCandidates}; suggested articles {SuggestedArticles}", analysis.Id, response.Result.Domains.Count, response.Result.Topics.Count, response.Result.Relationships.Count, response.Result.DuplicateGroups.Count, response.Result.Conflicts.Count, response.Result.OutdatedCandidates.Count, response.Result.SuggestedArticles.Count);
            }
            var json = JsonSerializer.Serialize(response.Result);
            watch.Stop();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            analysis.Complete(json, response.Usage?.InputTokens, response.Usage?.OutputTokens, response.Usage?.TotalTokens, watch.ElapsedMilliseconds);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Knowledge discovery completed for UploadId {UploadId}, AnalysisId {AnalysisId}, mode {AiMode}, model {Model}, input tokens {InputTokens}, output tokens {OutputTokens}, total tokens {TotalTokens}, duration {DurationMilliseconds}ms", uploadId, analysis.Id, mode, analysis.Model, response.Usage?.InputTokens, response.Usage?.OutputTokens, response.Usage?.TotalTokens, watch.ElapsedMilliseconds);
            return ToResult(analysis, request.Documents.Count, request.Documents.Sum(x => x.CharacterCount));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            watch.Stop(); analysis.Fail(ex is InvalidDataException or InvalidOperationException ? ex.Message : "OrgWiki couldn't complete knowledge analysis for this archive.", watch.ElapsedMilliseconds); await db.SaveChangesAsync(cancellationToken);
            if (options.Value.VerboseLogging && mode == AiMode.Live && ex is InvalidDataException)
                logger.LogWarning("Knowledge discovery top-level JSON schema validation result: Failed. AnalysisId {AnalysisId}; error {ErrorMessage}", analysis.Id, ex.Message);
            logger.LogError("Knowledge discovery failed for UploadId {UploadId}, AnalysisId {AnalysisId}, mode {AiMode}: {ErrorMessage}", uploadId, analysis.Id, mode, ex.Message);
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug(ex, "Knowledge discovery failure details for AnalysisId {AnalysisId}", analysis.Id);
            throw;
        }
    }

    public async Task<KnowledgeAnalysisResult?> GetAsync(Guid analysisId, CancellationToken cancellationToken)
    {
        var owned = await db.KnowledgeAnalyses.Where(x => x.Id == analysisId)
            .Join(db.Uploads, analysis => analysis.UploadId, upload => upload.Id, (analysis, upload) => new { analysis, upload })
            .SingleOrDefaultAsync(x => x.upload.UserId == currentUser.Id, cancellationToken);
        return owned is null ? null : ToResult(owned.analysis, await db.Documents.CountAsync(x => x.UploadId == owned.upload.Id && x.ProcessingStatus == DocumentProcessingStatus.Parsed, cancellationToken), owned.upload.TotalCharacterCount);
    }

    static KnowledgeAnalysisResult ToResult(KnowledgeAnalysis analysis, int documents, int characters)
    { KnowledgeDiscoveryResult? discovery = null; if (!string.IsNullOrWhiteSpace(analysis.ResultJson)) discovery = JsonSerializer.Deserialize<KnowledgeDiscoveryResult>(analysis.ResultJson); return new(analysis.Id, analysis.UploadId, analysis.Status.ToString(), analysis.AiMode.ToString(), analysis.Model, documents, characters, analysis.InputTokens, analysis.OutputTokens, analysis.TotalTokens, analysis.DurationMilliseconds, analysis.ErrorMessage, discovery); }
}
