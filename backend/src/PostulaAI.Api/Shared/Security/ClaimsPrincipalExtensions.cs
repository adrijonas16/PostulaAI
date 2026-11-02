using System.Security.Claims;

namespace PostulaAI.Api.Shared.Security;

public static class ClaimsPrincipalExtensions
{
    /// <summary>JWT "sub" claim. Inbound claim mapping is disabled, so the raw name is used.</summary>
    public const string SubjectClaim = "sub";

    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var subject = principal.FindFirst(SubjectClaim)?.Value;
        return Guid.TryParse(subject, out var userId)
            ? userId
            : throw new InvalidOperationException("The current principal has no valid subject claim.");
    }
}
