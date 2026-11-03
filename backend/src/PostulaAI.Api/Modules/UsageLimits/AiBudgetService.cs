using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PostulaAI.AI.Clients;
using PostulaAI.Api.Modules.Auth;
using PostulaAI.Api.Modules.UsageLimits.Domain;
using PostulaAI.Api.Shared.Errors;
using PostulaAI.Api.Shared.Persistence;

namespace PostulaAI.Api.Modules.UsageLimits;

/// <summary>Bound from "UsageLimits:AiDaily". A ceiling on AI requests for the whole site, so costs cannot run away.</summary>
public sealed class AiDailyBudgetOptions
{
    public const string SectionName = "UsageLimits:AiDaily";

    /// <summary>Requests (analyses + CV imports, all users) allowed in any rolling 24 hours. 0 disables the ceiling.</summary>
    public int MaxRequests { get; set; } = 200;
}

/// <summary>
/// Gatekeeper for every feature that calls the AI provider: requires a verified e-mail (when verification is on)
/// and enforces the site-wide daily budget. A successful reservation is recorded before the provider is called,
/// so failed or repaired calls still count. Two simultaneous requests may exceed the budget by one (documented).
/// </summary>
public sealed class AiBudgetService(
    AppDbContext db,
    EmailVerificationService emailVerification,
    IAIClient aiClient,
    IOptions<AiDailyBudgetOptions> options,
    TimeProvider time)
{
    private static readonly TimeSpan Window = TimeSpan.FromHours(24);

    public async Task<bool> IsDailyBudgetReachedAsync(CancellationToken cancellationToken)
    {
        var limit = options.Value.MaxRequests;
        if (limit <= 0)
        {
            return false;
        }

        var windowStart = time.GetUtcNow() - Window;
        var used = await db.AiUsageEvents.CountAsync(usage => usage.CreatedAt >= windowStart, cancellationToken);
        return used >= limit;
    }

    /// <summary>Returns null when the request may call the AI provider (and records it); otherwise the error to return.</summary>
    public async Task<AppError?> TryReserveAsync(Guid userId, AiFeature feature, CancellationToken cancellationToken)
    {
        if (await emailVerification.IsPendingAsync(userId, cancellationToken))
        {
            return new AppError(ErrorCodes.EmailNotVerified, StatusCodes.Status403Forbidden,
                "Confirma tu correo para usar el análisis y la importación del CV. Revisa tu bandeja de entrada.");
        }

        // Without a key the provider fails immediately at no cost; let that error through without spending budget.
        if (!aiClient.IsConfigured)
        {
            return null;
        }

        if (await IsDailyBudgetReachedAsync(cancellationToken))
        {
            return new AppError(ErrorCodes.AiDailyBudgetReached, StatusCodes.Status503ServiceUnavailable,
                "PostulaAI alcanzó su límite diario de uso de IA. Vuelve a intentarlo mañana.");
        }

        db.AiUsageEvents.Add(new AiUsageEvent { UserId = userId, Feature = feature, CreatedAt = time.GetUtcNowForStorage() });
        await db.SaveChangesAsync(cancellationToken);
        return null;
    }
}
