using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PostulaAI.Api.Shared.Persistence;

namespace PostulaAI.Api.Modules.UsageLimits;

public sealed class AnalysisUsageOptions
{
    public const string SectionName = "UsageLimits:Analyses";

    public int MaxPerWindow { get; set; } = 10;

    public int WindowHours { get; set; } = 24;
}

public sealed record UsageResponse(int Limit, int Used, int Remaining, int WindowHours, DateTimeOffset? NextSlotAt);

/// <summary>
/// Rolling-window limit on analyses that reached the AI provider. Counting is derived from the analyses table,
/// so there is no separate counter to drift. Two simultaneous requests may exceed the limit by one (documented).
/// </summary>
public sealed class UsageLimitService(AppDbContext db, IOptions<AnalysisUsageOptions> options, TimeProvider time)
{
    public async Task<UsageResponse> GetUsageAsync(Guid userId, CancellationToken cancellationToken)
    {
        var limits = options.Value;
        var windowStart = time.GetUtcNow().AddHours(-limits.WindowHours);

        var attempts = await db.Analyses.AsNoTracking()
            .Where(analysis => analysis.UserId == userId && analysis.ProviderCalled && analysis.CreatedAt >= windowStart)
            .OrderBy(analysis => analysis.CreatedAt)
            .Select(analysis => analysis.CreatedAt)
            .ToListAsync(cancellationToken);

        var remaining = Math.Max(0, limits.MaxPerWindow - attempts.Count);
        DateTimeOffset? nextSlot = remaining == 0 && attempts.Count > 0
            ? attempts[attempts.Count - limits.MaxPerWindow].AddHours(limits.WindowHours)
            : null;

        return new UsageResponse(limits.MaxPerWindow, attempts.Count, remaining, limits.WindowHours, nextSlot);
    }
}
