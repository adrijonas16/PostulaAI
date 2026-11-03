using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PostulaAI.AI.Clients;
using PostulaAI.Api.Modules.Analyses.Contracts;
using PostulaAI.Api.Modules.Analyses.Domain;
using PostulaAI.Api.Modules.JobPostings.Contracts;
using PostulaAI.Api.Modules.JobPostings.Domain;
using PostulaAI.Api.Modules.UsageLimits;
using PostulaAI.IntegrationTests.Infrastructure;
using static PostulaAI.IntegrationTests.Infrastructure.ApiTestClient;

namespace PostulaAI.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public sealed class AnalysisEndpointsTests : IDisposable
{
    private const string AnalysesPath = "/api/v1/analyses";
    private readonly DatabaseApiFactory factory;

    public AnalysisEndpointsTests(DatabaseApiFactory factory)
    {
        this.factory = factory;
        factory.AI.Reset();
    }

    public void Dispose() => factory.AI.Reset();

    private async Task<(TestSession Session, JobPostingResponse Posting)> UserWithProfileAndPostingAsync(HttpClient client)
    {
        var session = await RegisterAsync(client);
        using var profile = await client.SendAsync(WithBearer(HttpMethod.Put, "/api/v1/profiles/me", session.Auth.AccessToken, ProfileEndpointsTests.SampleProfile()));
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        var posting = await CreatePostingAsync(client, session);
        return (session, posting);
    }

    private static async Task<JobPostingResponse> CreatePostingAsync(HttpClient client, TestSession session, string title = "Backend .NET")
    {
        var request = new JobPostingRequest(title, "Empresa Contratante", "Remoto", WorkMode.Remote,
            "Requisitos: C#, ASP.NET Core y PostgreSQL. Deseable: Azure. Inglés intermedio.", null);
        using var response = await client.SendAsync(WithBearer(HttpMethod.Post, "/api/v1/job-postings", session.Auth.AccessToken, request));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JobPostingResponse>(Json) ?? throw new InvalidOperationException();
    }

    private static Task<HttpResponseMessage> AnalyzeAsync(HttpClient client, TestSession session, Guid postingId) =>
        client.SendAsync(WithBearer(HttpMethod.Post, AnalysesPath, session.Auth.AccessToken, new CreateAnalysisRequest(postingId, "es")));

    [Fact]
    public async Task Analyze_StoresValidatedResult_WithScoreContextMetadataAndSnapshots()
    {
        using var client = factory.CreateApiClient();
        var (session, posting) = await UserWithProfileAndPostingAsync(client);

        using var response = await AnalyzeAsync(client, session, posting.Id);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var analysis = await response.Content.ReadFromJsonAsync<AnalysisResponse>(Json);
        Assert.NotNull(analysis);
        Assert.Equal(AnalysisStatus.Completed, analysis.Status);
        Assert.Equal(78, analysis.Result?.CompatibilityScore);
        Assert.Equal(new ScoreContext("high", 2, 0, 0, 1), analysis.Score);
        Assert.Equal(("job-analysis", "v1", "claude-integration-fake", false), (analysis.PromptId, analysis.PromptVersion, analysis.Model, analysis.RepairAttempted));
        Assert.Contains("\"importance\":\"required\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var stored = await factory.WithDbAsync(db => db.Analyses.AsNoTracking().SingleAsync(item => item.Id == analysis.Id));
        Assert.Contains("Laura Gómez", stored.ProfileSnapshot, StringComparison.Ordinal);
        Assert.Contains("Empresa Contratante", stored.JobPostingSnapshot, StringComparison.Ordinal);
        Assert.Equal((1200, 800), (stored.InputTokens, stored.OutputTokens));
        Assert.True(stored.ProviderCalled);
        Assert.NotNull(stored.ResultJson);
    }

    [Fact]
    public async Task Analysis_KeepsItsSnapshot_WhenThePostingIsLaterDeleted()
    {
        using var client = factory.CreateApiClient();
        var (session, posting) = await UserWithProfileAndPostingAsync(client);
        using var created = await AnalyzeAsync(client, session, posting.Id);
        var analysis = await created.Content.ReadFromJsonAsync<AnalysisResponse>(Json) ?? throw new InvalidOperationException();

        using var delete = await client.SendAsync(WithBearer(HttpMethod.Delete, $"/api/v1/job-postings/{posting.Id}", session.Auth.AccessToken));
        using var get = await client.SendAsync(WithBearer(HttpMethod.Get, $"{AnalysesPath}/{analysis.Id}", session.Auth.AccessToken));
        var after = await get.Content.ReadFromJsonAsync<AnalysisResponse>(Json);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Null(after?.JobPostingId);
        Assert.Equal("Backend .NET", after?.JobTitle);
        Assert.Equal(78, after?.Result?.CompatibilityScore);
    }

    [Fact]
    public async Task InvalidOutputTwice_FailsClearly_AndNeverStoresAResult()
    {
        using var client = factory.CreateApiClient();
        var (session, posting) = await UserWithProfileAndPostingAsync(client);
        factory.AI.Enqueue("{ invalid");
        factory.AI.Enqueue(ScriptedAIClient.ValidResult() with { CompatibilityScore = 250 });

        using var response = await AnalyzeAsync(client, session, posting.Id);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("ai_invalid_output", (await ReadProblemAsync(response)).GetProperty("errorCode").GetString());
        var stored = await factory.WithDbAsync(db => db.Analyses.AsNoTracking().Where(item => item.UserId == session.UserId).SingleAsync());
        Assert.Equal(AnalysisStatus.Failed, stored.Status);
        Assert.Null(stored.ResultJson);
        Assert.Null(stored.CompatibilityScore);
        Assert.True(stored.RepairAttempted);
    }

    [Fact]
    public async Task InventedTechnology_IsRepaired_BeforeSaving()
    {
        using var client = factory.CreateApiClient();
        var (session, posting) = await UserWithProfileAndPostingAsync(client);
        factory.AI.Enqueue(ScriptedAIClient.ValidResult() with { AdaptedProfessionalSummary = "Experta en Kubernetes y AWS." });
        factory.AI.Enqueue(ScriptedAIClient.ValidResult());

        using var response = await AnalyzeAsync(client, session, posting.Id);
        var analysis = await response.Content.ReadFromJsonAsync<AnalysisResponse>(Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(analysis?.RepairAttempted);
        Assert.DoesNotContain("Kubernetes", analysis?.Result?.AdaptedProfessionalSummary ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingApiKey_Returns503_AndDoesNotConsumeUsage()
    {
        using var client = factory.CreateApiClient();
        var (session, posting) = await UserWithProfileAndPostingAsync(client);
        factory.AI.EnqueueError(AIProviderErrorKind.NotConfigured);

        using var response = await AnalyzeAsync(client, session, posting.Id);
        using var usage = await client.SendAsync(WithBearer(HttpMethod.Get, "/api/v1/usage/me", session.Auth.AccessToken));
        var usageBody = await usage.Content.ReadFromJsonAsync<UsageResponse>(Json);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("ai_not_configured", (await ReadProblemAsync(response)).GetProperty("errorCode").GetString());
        Assert.Equal(0, usageBody?.Used);
    }

    [Fact]
    public async Task UsageLimit_BlocksFurtherAnalyses_InTheWindow()
    {
        using var client = factory.CreateApiClient();
        var (session, posting) = await UserWithProfileAndPostingAsync(client);
        for (var i = 0; i < DatabaseApiFactory.AnalysisLimit; i++)
        {
            using var allowed = await AnalyzeAsync(client, session, posting.Id);
            Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
        }

        var callsBefore = factory.AI.Calls;
        using var blocked = await AnalyzeAsync(client, session, posting.Id);
        using var usage = await client.SendAsync(WithBearer(HttpMethod.Get, "/api/v1/usage/me", session.Auth.AccessToken));
        var usageBody = await usage.Content.ReadFromJsonAsync<UsageResponse>(Json);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal("usage_limit_reached", (await ReadProblemAsync(blocked)).GetProperty("errorCode").GetString());
        Assert.Equal(callsBefore, factory.AI.Calls);
        Assert.Equal(0, usageBody?.Remaining);
        Assert.NotNull(usageBody?.NextSlotAt);
    }

    [Fact]
    public async Task RequiresProfile_AndOwnPosting()
    {
        using var client = factory.CreateApiClient();
        var withoutProfile = await RegisterAsync(client);
        var ownPosting = await CreatePostingAsync(client, withoutProfile);
        var (other, _) = await UserWithProfileAndPostingAsync(client);
        var callsBefore = factory.AI.Calls;

        using var noProfile = await AnalyzeAsync(client, withoutProfile, ownPosting.Id);
        using var foreignPosting = await AnalyzeAsync(client, other, ownPosting.Id);

        Assert.Equal(HttpStatusCode.Conflict, noProfile.StatusCode);
        Assert.Equal("profile_required", (await ReadProblemAsync(noProfile)).GetProperty("errorCode").GetString());
        Assert.Equal(HttpStatusCode.NotFound, foreignPosting.StatusCode);
        Assert.Equal(callsBefore, factory.AI.Calls);
    }

    [Fact]
    public async Task OtherUsersAnalyses_AreInvisible()
    {
        using var client = factory.CreateApiClient();
        var (owner, posting) = await UserWithProfileAndPostingAsync(client);
        var intruder = await RegisterAsync(client);
        using var created = await AnalyzeAsync(client, owner, posting.Id);
        var analysis = await created.Content.ReadFromJsonAsync<AnalysisResponse>(Json) ?? throw new InvalidOperationException();
        var path = $"{AnalysesPath}/{analysis.Id}";

        using var get = await client.SendAsync(WithBearer(HttpMethod.Get, path, intruder.Auth.AccessToken));
        using var delete = await client.SendAsync(WithBearer(HttpMethod.Delete, path, intruder.Auth.AccessToken));
        using var list = await client.SendAsync(WithBearer(HttpMethod.Get, AnalysesPath, intruder.Auth.AccessToken));
        var page = await list.Content.ReadFromJsonAsync<PagedResponse<AnalysisSummary>>(Json);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Empty(page?.Items ?? []);
    }

    [Fact]
    public async Task List_ShowsNewestFirst_WithBandAndFailures()
    {
        using var client = factory.CreateApiClient();
        var (session, posting) = await UserWithProfileAndPostingAsync(client);
        using var first = await AnalyzeAsync(client, session, posting.Id);
        factory.AI.EnqueueError(AIProviderErrorKind.Unavailable);
        using var second = await AnalyzeAsync(client, session, posting.Id);

        using var list = await client.SendAsync(WithBearer(HttpMethod.Get, AnalysesPath, session.Auth.AccessToken));
        var page = await list.Content.ReadFromJsonAsync<PagedResponse<AnalysisSummary>>(Json);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, second.StatusCode);
        Assert.NotNull(page);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(AnalysisStatus.Failed, page.Items[0].Status);
        Assert.Equal("ai_unavailable", page.Items[0].ErrorCode);
        Assert.Equal(("high", 78), (page.Items[1].ScoreBand, page.Items[1].CompatibilityScore));
    }

    [Fact]
    public async Task AiStatus_RequiresAuthentication_AndReportsWhetherTheProviderIsConfigured()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);

        using var anonymous = await client.GetAsync(new Uri("/api/v1/ai/status", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        try
        {
            factory.AI.IsConfigured = false;
            using var missing = await client.SendAsync(WithBearer(HttpMethod.Get, "/api/v1/ai/status", session.Auth.AccessToken));
            Assert.Equal(HttpStatusCode.OK, missing.StatusCode);
            Assert.Equal("{\"configured\":false}", await missing.Content.ReadAsStringAsync());
        }
        finally
        {
            factory.AI.IsConfigured = true;
        }

        using var configured = await client.SendAsync(WithBearer(HttpMethod.Get, "/api/v1/ai/status", session.Auth.AccessToken));
        Assert.Equal("{\"configured\":true}", await configured.Content.ReadAsStringAsync());
    }
}
