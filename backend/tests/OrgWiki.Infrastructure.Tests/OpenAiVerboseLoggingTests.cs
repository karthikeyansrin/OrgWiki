using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrgWiki.Application.Analysis;
using OrgWiki.Infrastructure.Analysis;
using Xunit;

namespace OrgWiki.Infrastructure.Tests;

public sealed class OpenAiVerboseLoggingTests
{
    [Fact]
    public void Strict_structured_output_schemas_omit_unsupported_uniqueItems()
    {
        Assert.DoesNotContain("uniqueItems", KnowledgeDiscoverySchema.CreateResponseFormat().ToJsonString(), StringComparison.Ordinal);
        Assert.DoesNotContain("uniqueItems", KnowledgeGenerationSchema.CreateResponseFormat().ToJsonString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Discovery_logging_is_opt_in_and_does_not_add_provider_calls(bool verboseLogging)
    {
        var handler = new StubHandler("""{"choices":[{"finish_reason":"stop","message":{"content":"{\"domains\":[],\"topics\":[],\"relationships\":[],\"duplicateGroups\":[],\"conflicts\":[],\"outdatedCandidates\":[],\"suggestedArticles\":[]}"}}],"usage":{"prompt_tokens":12,"completion_tokens":8,"total_tokens":20}}""");
        var logger = new CapturingLogger<OpenAiKnowledgeDiscoveryProvider>();
        var provider = new OpenAiKnowledgeDiscoveryProvider(new StubHttpClientFactory(handler), Options.Create(new KnowledgeAnalysisOptions { ApiKey = "test-key", Model = "test-model", VerboseLogging = verboseLogging }), logger);
        var marker = "MIDDLE_CONTENT_MUST_NOT_APPEAR_IN_A_TRUNCATED_PREVIEW";
        var corpus = new string('a', 750) + marker + new string('b', 750);

        await provider.DiscoverAsync(new KnowledgeDiscoveryRequest(Guid.NewGuid(), [], corpus), default);

        Assert.Equal(1, handler.CallCount);
        if (!verboseLogging)
        {
            Assert.Empty(logger.Messages);
            return;
        }

        Assert.Contains(logger.Messages, message => message.Contains("ORGWIKI DISCOVERY", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("OPENAI SUMMARY", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains(marker, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Generation_verbose_logging_includes_generation_diagnostics()
    {
        var handler = new StubHandler("""{"choices":[{"finish_reason":"stop","message":{"content":"{\"articles\":[]}"}}],"usage":{"prompt_tokens":12,"completion_tokens":8,"total_tokens":20}}""");
        var logger = new CapturingLogger<OpenAiKnowledgeGenerationProvider>();
        var provider = new OpenAiKnowledgeGenerationProvider(new StubHttpClientFactory(handler), Options.Create(new KnowledgeAnalysisOptions { ApiKey = "test-key", Model = "test-model", VerboseLogging = true }), logger);

        await provider.GenerateAsync(new KnowledgeGenerationRequest(Guid.NewGuid(), []), default);

        Assert.Equal(1, handler.CallCount);
        Assert.Contains(logger.Messages, message => message.Contains("ORGWIKI GENERATION", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("OPENAI SUMMARY", StringComparison.Ordinal));
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.openai.com/") };
    }

    private sealed class StubHandler(string payload) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
            response.Headers.Add("x-request-id", "req_test");
            return Task.FromResult(response);
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
