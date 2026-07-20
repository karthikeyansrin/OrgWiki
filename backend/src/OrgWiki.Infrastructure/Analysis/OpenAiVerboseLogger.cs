using Microsoft.Extensions.Logging;
using OrgWiki.Application.Analysis;
using System.Security.Cryptography;
using System.Text;

namespace OrgWiki.Infrastructure.Analysis;

internal static class OpenAiVerboseLogger
{
    public static void LogRequest(
        ILogger logger,
        bool enabled,
        string stage,
        string? requestId,
        Guid? uploadId,
        Guid? analysisId,
        Guid? generationId,
        string model,
        string prompt,
        ProviderUsage? usage,
        long latencyMilliseconds,
        string? finishReason,
        int responseCharacterCount,
        KnowledgeAnalysisOptions options)
    {
        if (!enabled) return;

        logger.LogInformation(
            "========== ORGWIKI {Stage} ==========\nRequestId: {RequestId}\nUploadId: {UploadId}\nAnalysisId: {AnalysisId}\nGenerationId: {GenerationId}\nModel: {Model}\nMode: Live\nPrompt size (characters): {PromptCharacters}\nPrompt fingerprint: {PromptFingerprint}\nEstimated prompt tokens: {EstimatedPromptTokens}\nActual prompt tokens: {PromptTokens}\nCompletion tokens: {CompletionTokens}\nTotal tokens: {TotalTokens}\nLatency (ms): {LatencyMilliseconds}\nFinish reason: {FinishReason}\nResponse size (characters): {ResponseCharacters}",
            stage,
            requestId ?? "Unavailable",
            uploadId?.ToString() ?? "N/A",
            analysisId?.ToString() ?? "N/A",
            generationId?.ToString() ?? "N/A",
            model,
            prompt.Length,
            PromptFingerprint(prompt),
            prompt.Length / 4,
            usage?.InputTokens?.ToString() ?? "Unavailable",
            usage?.OutputTokens?.ToString() ?? "Unavailable",
            usage?.TotalTokens?.ToString() ?? "Unavailable",
            latencyMilliseconds,
            finishReason ?? "Unavailable",
            responseCharacterCount);

        LogSummary(logger, stage, model, usage, latencyMilliseconds, options);
    }

    public static void LogFailure(
        ILogger logger,
        bool enabled,
        string stage,
        string? requestId,
        string model,
        int? statusCode,
        string? openAiErrorCode,
        string? openAiErrorMessage,
        long latencyMilliseconds)
    {
        if (!enabled) return;

        logger.LogWarning(
            "========== ORGWIKI {Stage} FAILURE ==========\nRequestId: {RequestId}\nModel: {Model}\nHTTP Status: {StatusCode}\nOpenAI error code: {OpenAiErrorCode}\nError message: {OpenAiErrorMessage}\nLatency (ms): {LatencyMilliseconds}",
            stage,
            requestId ?? "Unavailable",
            model,
            statusCode?.ToString() ?? "Unavailable",
            openAiErrorCode ?? "Unavailable",
            openAiErrorMessage ?? "Unavailable",
            latencyMilliseconds);
    }

    public static void LogSummary(ILogger logger, string stage, string model, ProviderUsage? usage, long latencyMilliseconds, KnowledgeAnalysisOptions options)
    {
        logger.LogInformation(
            "========== OPENAI SUMMARY ==========\nStage: {Stage}\nModel: {Model}\nPrompt tokens: {PromptTokens}\nCompletion tokens: {CompletionTokens}\nTotal tokens: {TotalTokens}\nLatency (ms): {LatencyMilliseconds}\nEstimated cost: {EstimatedCost}\n=====================================",
            stage,
            model,
            usage?.InputTokens?.ToString() ?? "Unavailable",
            usage?.OutputTokens?.ToString() ?? "Unavailable",
            usage?.TotalTokens?.ToString() ?? "Unavailable",
            latencyMilliseconds,
            CalculateCost(usage, options));
    }

    private static string PromptFingerprint(string prompt)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(prompt)))[..16];

    private static string CalculateCost(ProviderUsage? usage, KnowledgeAnalysisOptions options)
    {
        if (usage?.InputTokens is not int inputTokens || usage.OutputTokens is not int outputTokens ||
            options.InputTokenCostPerMillionUsd is not decimal inputRate || options.OutputTokenCostPerMillionUsd is not decimal outputRate)
            return "Unknown";

        var cost = (inputTokens / 1_000_000m * inputRate) + (outputTokens / 1_000_000m * outputRate);
        return $"USD {cost:0.######}";
    }
}
