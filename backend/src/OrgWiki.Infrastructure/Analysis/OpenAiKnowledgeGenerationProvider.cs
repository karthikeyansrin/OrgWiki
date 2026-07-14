using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OrgWiki.Application.Analysis;

namespace OrgWiki.Infrastructure.Analysis;

public sealed class OpenAiKnowledgeGenerationProvider(IHttpClientFactory clients, IOptions<KnowledgeAnalysisOptions> options) : IKnowledgeGenerationProvider
{
    public async Task<KnowledgeGenerationResponse> GenerateAsync(KnowledgeGenerationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey)) throw new InvalidOperationException("OpenAI API key is not configured.");
        var body = JsonSerializer.Serialize(new { model = options.Value.Model, max_completion_tokens = options.Value.KnowledgeGenerationMaxOutputTokens, response_format = KnowledgeGenerationSchema.CreateResponseFormat(), messages = new[] { new { role = "system", content = KnowledgeGenerationPrompt.System }, new { role = "user", content = KnowledgeGenerationPrompt.User(request) } } });
        using var http = clients.CreateClient("OpenAI"); http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
        var watch = Stopwatch.StartNew(); using var response = await http.PostAsync("v1/chat/completions", new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken); var json = await response.Content.ReadAsStringAsync(cancellationToken); watch.Stop();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("OpenAI knowledge generation request failed.");
        using var document = JsonDocument.Parse(json); var content = document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? throw new InvalidDataException("OpenAI returned an empty knowledge generation response.");
        var result = JsonSerializer.Deserialize<KnowledgeGenerationResult>(content, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new InvalidDataException("OpenAI returned invalid knowledge generation JSON.");
        var usage = document.RootElement.TryGetProperty("usage", out var u) ? new ProviderUsage(u.GetProperty("prompt_tokens").GetInt32(), u.GetProperty("completion_tokens").GetInt32(), u.GetProperty("total_tokens").GetInt32()) : null;
        return new KnowledgeGenerationResponse(result, usage);
    }
}
