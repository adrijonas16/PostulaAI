using System.Text;

namespace PostulaAI.Api.Modules.Auth;

/// <summary>Bound from the "Jwt" configuration section. The signing key must come from a secret store or environment.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumSigningKeyBytes = 32;

    public string Issuer { get; set; } = "postulaai";

    public string Audience { get; set; } = "postulaai-web";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 14;

    public byte[] GetSigningKeyBytes() => Encoding.UTF8.GetBytes(SigningKey);

    public bool HasValidSigningKey() => Encoding.UTF8.GetByteCount(SigningKey) >= MinimumSigningKeyBytes;
}

/// <summary>Bound from "RateLimiting:Auth".</summary>
public sealed class AuthRateLimitOptions
{
    public const string SectionName = "RateLimiting:Auth";
    public const string PolicyName = "auth";

    public int PermitLimit { get; set; } = 10;

    public int WindowSeconds { get; set; } = 60;
}

public static class RefreshTokenCookie
{
    public const string Name = "postulaai_refresh";

    /// <summary>The cookie is only sent to auth endpoints.</summary>
    public const string Path = "/api/v1/auth";
}
