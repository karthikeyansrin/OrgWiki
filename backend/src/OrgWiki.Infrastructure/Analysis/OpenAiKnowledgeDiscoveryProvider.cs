using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Http;
using OrgWiki.Application.Analysis;

namespace OrgWiki.Infrastructure.Analysis;

public sealed class OpenAiKnowledgeDiscoveryProvider(IHttpClientFactory clients, IOptions<KnowledgeAnalysisOptions> options) : IKnowledgeDiscoveryProvider
{
    public async Task<KnowledgeDiscoveryResponse> DiscoverAsync(KnowledgeDiscoveryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey)) throw new InvalidOperationException("OpenAI API key is not configured.");
        var body = JsonSerializer.Serialize(new { model = options.Value.Model, max_completion_tokens = options.Value.KnowledgeDiscoveryMaxOutputTokens, response_format = KnowledgeDiscoverySchema.CreateResponseFormat(), messages = new[] { new { role = "system", content = KnowledgeDiscoveryPrompt.System }, new { role = "user", content = KnowledgeDiscoveryPrompt.User(request) } } });
        using var http = clients.CreateClient("OpenAI"); http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
        var watch = Stopwatch.StartNew(); using var response = await http.PostAsync("v1/chat/completions", new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken); var json = await response.Content.ReadAsStringAsync(cancellationToken); watch.Stop();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("OpenAI knowledge discovery request failed.");
        using var document = JsonDocument.Parse(json); var content = document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? throw new InvalidDataException("OpenAI returned an empty knowledge discovery response.");
        var result = JsonSerializer.Deserialize<KnowledgeDiscoveryResult>(content, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new InvalidDataException("OpenAI returned invalid knowledge discovery JSON.");
        var usage = document.RootElement.TryGetProperty("usage", out var u) ? new ProviderUsage(u.GetProperty("prompt_tokens").GetInt32(), u.GetProperty("completion_tokens").GetInt32(), u.GetProperty("total_tokens").GetInt32()) : null;
        return new KnowledgeDiscoveryResponse(result, usage);
    }
}
