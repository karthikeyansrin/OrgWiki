using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Http;
using OrgWiki.Application.Analysis;

namespace OrgWiki.Infrastructure.Analysis;

public sealed class OpenAiKnowledgeDiscoveryProvider(IHttpClientFactory clients, IOptions<KnowledgeAnalysisOptions> options, ILogger<OpenAiKnowledgeDiscoveryProvider> logger) : IKnowledgeDiscoveryProvider
{
    public async Task<KnowledgeDiscoveryResponse> DiscoverAsync(KnowledgeDiscoveryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey)) throw new InvalidOperationException("OpenAI API key is not configured.");
        var systemPrompt = KnowledgeDiscoveryPrompt.System;
        var userPrompt = KnowledgeDiscoveryPrompt.User(request);
        var prompt = $"{systemPrompt}\n\n{userPrompt}";
        var body = JsonSerializer.Serialize(new { model = options.Value.Model, max_completion_tokens = options.Value.KnowledgeDiscoveryMaxOutputTokens, response_format = KnowledgeDiscoverySchema.CreateResponseFormat(), messages = new[] { new { role = "system", content = systemPrompt }, new { role = "user", content = userPrompt } } });
        using var http = clients.CreateClient("OpenAI"); http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
        var watch = Stopwatch.StartNew();
        try
        {
            using var response = await http.PostAsync("v1/chat/completions", new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            watch.Stop();
            var requestId = response.Headers.TryGetValues("x-request-id", out var requestIds) ? requestIds.FirstOrDefault() : null;
            if (!response.IsSuccessStatusCode)
            {
                var error = ReadError(json);
                OpenAiVerboseLogger.LogFailure(logger, options.Value.VerboseLogging, "DISCOVERY", requestId, options.Value.Model, (int)response.StatusCode, error.Code, error.Message, watch.ElapsedMilliseconds);
                throw new OpenAiProviderException("knowledge discovery", (int)response.StatusCode, requestId, error.Code, error.Message);
            }

            using var document = JsonDocument.Parse(json);
            var choice = document.RootElement.GetProperty("choices")[0];
            var content = choice.GetProperty("message").GetProperty("content").GetString() ?? throw new InvalidDataException("OpenAI returned an empty knowledge discovery response.");
            var result = JsonSerializer.Deserialize<KnowledgeDiscoveryResult>(content, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new InvalidDataException("OpenAI returned invalid knowledge discovery JSON.");
            var usage = document.RootElement.TryGetProperty("usage", out var u) ? new ProviderUsage(u.GetProperty("prompt_tokens").GetInt32(), u.GetProperty("completion_tokens").GetInt32(), u.GetProperty("total_tokens").GetInt32()) : null;
            OpenAiVerboseLogger.LogRequest(logger, options.Value.VerboseLogging, "DISCOVERY", requestId, request.UploadId, request.AnalysisId, null, options.Value.Model, prompt, usage, watch.ElapsedMilliseconds, choice.TryGetProperty("finish_reason", out var finishReason) ? finishReason.GetString() : null, json.Length, options.Value);
            return new KnowledgeDiscoveryResponse(result, usage);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            watch.Stop();
            OpenAiVerboseLogger.LogFailure(logger, options.Value.VerboseLogging, "DISCOVERY", null, options.Value.Model, null, "timeout", "The OpenAI request timed out.", watch.ElapsedMilliseconds);
            throw new TimeoutException("The OpenAI knowledge discovery request timed out.");
        }
    }

    private static (string? Code, string? Message) ReadError(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("error", out var error)) return (null, null);
            return (error.TryGetProperty("code", out var code) ? code.GetString() : null, error.TryGetProperty("message", out var message) ? message.GetString() : null);
        }
        catch (JsonException) { return (null, null); }
    }
}
