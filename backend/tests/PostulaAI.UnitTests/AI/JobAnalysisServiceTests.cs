using PostulaAI.AI.Clients;
using PostulaAI.AI.Contracts;
using PostulaAI.AI.Services;

namespace PostulaAI.UnitTests.AI;

public sealed class JobAnalysisServiceTests
{
    private static JobAnalysisService Service(FakeAIClient client) => new(client, new JobAnalysisOptions());

    [Fact]
    public async Task ValidFirstResponse_Succeeds_WithoutRepair_AndRecordsMetadata()
    {
        var client = new FakeAIClient().Returns(AIFixtures.Completion(AIFixtures.ValidJson()));

        var outcome = await Service(client).AnalyzeAsync(AIFixtures.Request(), CancellationToken.None);

        Assert.True(outcome.Succeeded);
        Assert.False(outcome.RepairAttempted);
        Assert.Equal(72, outcome.Result?.CompatibilityScore);
        Assert.Equal(("job-analysis", "v1", "claude-test", "es"), (outcome.PromptId, outcome.PromptVersion, outcome.Model, outcome.Language));
        Assert.Equal((1000, 500), (outcome.InputTokens, outcome.OutputTokens));
        Assert.Equal(TimeSpan.FromSeconds(2), outcome.Latency);
        Assert.NotNull(outcome.ResultJson);
        var request = Assert.Single(client.Requests);
        Assert.NotNull(request.OutputJsonSchema);
        Assert.Contains("Spanish", request.SystemPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidFirstResponse_TriggersOneRepair_WithTheValidationErrors_ThenSucceeds()
    {
        var invented = AIFixtures.ValidResult() with
        {
            Strengths = [new AnalysisStrength("Kubernetes", "Administró clústeres de Kubernetes.", RequirementImportance.Preferred)],
        };
        var client = new FakeAIClient()
            .Returns(AIFixtures.Completion(JobAnalysisJson.Serialize(invented)))
            .Returns(AIFixtures.Completion(AIFixtures.ValidJson()));

        var outcome = await Service(client).AnalyzeAsync(AIFixtures.Request(), CancellationToken.None);

        Assert.True(outcome.Succeeded);
        Assert.True(outcome.RepairAttempted);
        Assert.Equal((2000, 1000), (outcome.InputTokens, outcome.OutputTokens));
        var repair = client.Requests[1];
        Assert.Equal(new[] { AIMessageRole.User, AIMessageRole.Assistant, AIMessageRole.User }, repair.Messages.Select(message => message.Role));
        Assert.Contains("strengths[0].evidence mentions \"Kubernetes\"", repair.Messages[2].Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidAfterRepair_Fails_WithoutResult()
    {
        var client = new FakeAIClient()
            .Returns(AIFixtures.Completion("{ not json"))
            .Returns(AIFixtures.Completion(JobAnalysisJson.Serialize(AIFixtures.ValidResult() with { CompatibilityScore = 150 })));

        var outcome = await Service(client).AnalyzeAsync(AIFixtures.Request(), CancellationToken.None);

        Assert.False(outcome.Succeeded);
        Assert.True(outcome.RepairAttempted);
        Assert.Equal(AnalysisErrorCodes.InvalidOutput, outcome.ErrorCode);
        Assert.Null(outcome.Result);
        Assert.Null(outcome.ResultJson);
        Assert.Contains(outcome.ValidationIssues, issue => issue.StartsWith("compatibilityScore", StringComparison.Ordinal));
        Assert.Equal(2, client.Requests.Count);
    }

    [Fact]
    public async Task TruncatedResponse_IsRepaired()
    {
        var client = new FakeAIClient()
            .Returns(AIFixtures.Completion("{\"compatibilityScore\": 7", stopReason: "max_tokens"))
            .Returns(AIFixtures.Completion(AIFixtures.ValidJson()));

        var outcome = await Service(client).AnalyzeAsync(AIFixtures.Request(), CancellationToken.None);

        Assert.True(outcome.Succeeded);
        Assert.True(outcome.RepairAttempted);
    }

    [Theory]
    [InlineData(AIProviderErrorKind.NotConfigured, AnalysisErrorCodes.NotConfigured, false)]
    [InlineData(AIProviderErrorKind.RateLimited, AnalysisErrorCodes.RateLimited, true)]
    [InlineData(AIProviderErrorKind.Overloaded, AnalysisErrorCodes.RateLimited, true)]
    [InlineData(AIProviderErrorKind.Unavailable, AnalysisErrorCodes.Unavailable, true)]
    public async Task ProviderFailures_MapToStableErrorCodes(AIProviderErrorKind kind, string errorCode, bool providerCalled)
    {
        var outcome = await Service(new FakeAIClient().Throws(kind)).AnalyzeAsync(AIFixtures.Request(), CancellationToken.None);

        Assert.False(outcome.Succeeded);
        Assert.Equal(errorCode, outcome.ErrorCode);
        Assert.Equal(providerCalled, outcome.ProviderCalled);
        Assert.Null(outcome.ResultJson);
    }

    [Fact]
    public async Task UnsupportedLanguage_IsRejectedBeforeCallingTheProvider()
    {
        var client = new FakeAIClient();

        await Assert.ThrowsAsync<ArgumentException>(() => Service(client).AnalyzeAsync(AIFixtures.Request("xx"), CancellationToken.None));

        Assert.Empty(client.Requests);
    }
}
