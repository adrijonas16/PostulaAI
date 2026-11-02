namespace PostulaAI.Api.Modules.Auth.Contracts;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserResponse(Guid Id, string Email, string DisplayName);

/// <summary>The refresh token is never in the body; it travels only in an HttpOnly cookie.</summary>
public sealed record AuthResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, UserResponse User);

/// <summary>Internal result of a successful authentication: response body plus the cookie value.</summary>
public sealed record AuthSession(AuthResponse Response, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);
