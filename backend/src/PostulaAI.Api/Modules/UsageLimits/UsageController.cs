using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostulaAI.Api.Shared.Api;

namespace PostulaAI.Api.Modules.UsageLimits;

[Route("api/v1/usage")]
[Authorize]
public sealed class UsageController(UsageLimitService usage) : ApiControllerBase
{
    /// <summary>Analyses used and remaining in the current rolling window.</summary>
    [HttpGet("me")]
    [ProducesResponseType<UsageResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await usage.GetUsageAsync(CurrentUserId, cancellationToken));
}
