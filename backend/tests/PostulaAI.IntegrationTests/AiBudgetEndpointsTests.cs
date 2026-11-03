using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PostulaAI.Api.Modules.Analyses;
using PostulaAI.IntegrationTests.Infrastructure;
using static PostulaAI.IntegrationTests.Infrastructure.ApiTestClient;

namespace PostulaAI.IntegrationTests;

[Collection(AiBudgetCollection.Name)]
public sealed class AiBudgetEndpointsTests(AiBudgetApiFactory factory)
{
    [Fact]
    public async Task SiteWideDailyBudget_StopsAiCallsForEveryone_AndIsReportedByStatus()
    {
        using var client = factory.CreateApiClient();
        var first = await RegisterAsync(client);
        var second = await RegisterAsync(client);
        var cv = CvImportEndpointsTests.Docx(CvImportEndpointsTests.CvText);

        for (var i = 0; i < AiBudgetApiFactory.DailyBudget; i++)
        {
            using var allowed = await client.SendAsync(CvImportEndpointsTests.Upload(first.Auth.AccessToken, cv));
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        var callsBefore = factory.AI.Calls;
        // A different user is stopped too: the budget is for the whole site.
        using var blocked = await client.SendAsync(CvImportEndpointsTests.Upload(second.Auth.AccessToken, cv));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, blocked.StatusCode);
        Assert.Contains("ai_daily_budget_reached", await blocked.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(callsBefore, factory.AI.Calls);

        using var status = await client.SendAsync(WithBearer(HttpMethod.Get, "/api/v1/ai/status", second.Auth.AccessToken));
        var body = await status.Content.ReadFromJsonAsync<AiStatusResponse>(Json);
        Assert.True(body?.DailyLimitReached);

        var recorded = await factory.WithDbAsync(db => db.AiUsageEvents.AsNoTracking().CountAsync());
        Assert.Equal(AiBudgetApiFactory.DailyBudget, recorded);
    }
}
