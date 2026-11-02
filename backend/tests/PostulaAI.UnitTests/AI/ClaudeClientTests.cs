using System.Net;
using System.Text.Json;
using PostulaAI.AI.Clients;

namespace PostulaAI.UnitTests.AI;

public sealed class ClaudeClientTests
{
    private const string SuccessBody = """
        {"id":"msg_1","type":"message","role":"assistant","model":"claude-sonnet-5-5","stop_reason":"end_turn",
         "content":[{"type":"thinking","thinking":"..."},{"type":"text","text":"{\"ok\":true}"}],
         "usage":{"input_tokens":120,"output_tokens":45}}
        """;

    private static readonly AICompletionRequest Request = new(
        "system prompt", [new AIMessage(AIMessageRole.User, "hola")], 2000, """{"type":"object"}""");

    private sealed class ScriptedHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private int index;

        public List<(HttpRequestMessage Request, string Body)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add((request, request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));
            return responses[index++];
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body, string? retryAfter = null)
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(body) };
        if (retryAfter is not null)
        {
            response.Headers.TryAddWithoutValidation("retry-after", retryAfter);
        }

        return response;
    }

    private static (ClaudeClient Client, ScriptedHandler Handler) Create(string? apiKey, params HttpResponseMessage[] responses)
    {
        var handler = new ScriptedHandler(responses);
        var options = new ClaudeOptions { ApiKey = apiKey, Model = "claude-sonnet-5-5", BaseRetryDelay = TimeSpan.Zero };
        return (new ClaudeClient(new HttpClient(handler), options, TimeProvider.System), handler);
    }

    [Fact]
    public async Task SendsMessagesRequest_WithHeadersAndStructuredOutputFormat()
    {
        var (client, handler) = Create("sk-test-not-real", Json(HttpStatusCode.OK, SuccessBody));

        await client.CompleteAsync(Request, CancellationToken.None);

        var (request, body) = Assert.Single(handler.Calls);
        Assert.Equal("https://api.anthropic.com/v1/messages", request.RequestUri?.ToString());
        Assert.Equal("sk-test-not-real", request.Headers.GetValues("x-api-key").Single());
        Assert.Equal("2023-06-01", request.Headers.GetValues("anthropic-version").Single());
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        Assert.Equal("claude-sonnet-5-5", root.GetProperty("model").GetString());
        Assert.Equal(2000, root.GetProperty("max_tokens").GetInt32());
        Assert.Equal("system prompt", root.GetProperty("system").GetString());
        Assert.Equal("user", root.GetProperty("messages")[0].GetProperty("role").GetString());
        Assert.Equal("json_schema", root.GetProperty("output_config").GetProperty("format").GetProperty("type").GetString());
        Assert.Equal("object", root.GetProperty("output_config").GetProperty("format").GetProperty("schema").GetProperty("type").GetString());
    }

    [Fact]
    public async Task ReturnsTextBlocksOnly_AndUsage()
    {
        var (client, _) = Create("sk-test-not-real", Json(HttpStatusCode.OK, SuccessBody));

        var result = await client.CompleteAsync(Request, CancellationToken.None);

        Assert.Equal("{\"ok\":true}", result.Content);
        Assert.Equal(("claude-sonnet-5-5", 120, 45, "end_turn"), (result.Model, result.InputTokens, result.OutputTokens, result.StopReason));
    }

    [Fact]
    public async Task RetriesRateLimitAndOverload_HonouringRetryAfter_ThenSucceeds()
    {
        var (client, handler) = Create(
            "sk-test-not-real",
            Json((HttpStatusCode)429, """{"type":"error"}""", retryAfter: "0"),
            Json((HttpStatusCode)529, """{"type":"error"}"""),
            Json(HttpStatusCode.OK, SuccessBody));

        var result = await client.CompleteAsync(Request, CancellationToken.None);

        Assert.Equal(3, handler.Calls.Count);
        Assert.Equal("{\"ok\":true}", result.Content);
    }

    [Fact]
    public async Task GivesUpAfterMaxRetries_WithATypedError()
    {
        var (client, handler) = Create(
            "sk-test-not-real",
            Json((HttpStatusCode)529, "{}"), Json((HttpStatusCode)529, "{}"), Json((HttpStatusCode)529, "{}"));

        var error = await Assert.ThrowsAsync<AIProviderException>(() => client.CompleteAsync(Request, CancellationToken.None));

        Assert.Equal(AIProviderErrorKind.Overloaded, error.Kind);
        Assert.Equal(3, handler.Calls.Count);
    }

    [Theory]
    [InlineData(400, AIProviderErrorKind.InvalidRequest)]
    [InlineData(401, AIProviderErrorKind.Authentication)]
    public async Task ClientErrors_AreNotRetried(int status, AIProviderErrorKind kind)
    {
        var (client, handler) = Create("sk-test-not-real", Json((HttpStatusCode)status, "{}"));

        var error = await Assert.ThrowsAsync<AIProviderException>(() => client.CompleteAsync(Request, CancellationToken.None));

        Assert.Equal(kind, error.Kind);
        Assert.Single(handler.Calls);
        Assert.DoesNotContain("sk-test-not-real", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingApiKey_FailsFast_WithoutCallingTheProvider()
    {
        var (client, handler) = Create(null);

        var error = await Assert.ThrowsAsync<AIProviderException>(() => client.CompleteAsync(Request, CancellationToken.None));

        Assert.Equal(AIProviderErrorKind.NotConfigured, error.Kind);
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public void Backoff_IsCappedByMaxRetryDelay()
    {
        var client = new ClaudeClient(new HttpClient(), new ClaudeOptions { BaseRetryDelay = TimeSpan.FromSeconds(10), MaxRetryDelay = TimeSpan.FromSeconds(15) }, TimeProvider.System);

        Assert.Equal(TimeSpan.FromSeconds(15), client.Backoff(attempt: 3, retryAfter: null));
        Assert.Equal(TimeSpan.FromSeconds(15), client.Backoff(attempt: 0, retryAfter: TimeSpan.FromMinutes(5)));
    }
}
