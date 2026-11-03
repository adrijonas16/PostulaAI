using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostulaAI.AI.Clients;
using PostulaAI.Api.Modules.UsageLimits;
using PostulaAI.Api.Shared.Api;

namespace PostulaAI.Api.Modules.Analyses;

/// <summary>Whether AI features (analysis, CV import) can run, so the UI can say so before the user waits.</summary>
public sealed record AiStatusResponse(bool Configured, bool DailyLimitReached);

[Route("api/v1/ai/status")]
[Authorize]
public sealed class AiStatusController(IAIClient client, AiBudgetService budget) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<AiStatusResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(new AiStatusResponse(client.IsConfigured, await budget.IsDailyBudgetReachedAsync(cancellationToken)));
}
