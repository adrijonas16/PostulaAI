using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PostulaAI.Api.Modules.AuditLogs.Domain;
using PostulaAI.Api.Modules.Auth.Contracts;
using PostulaAI.IntegrationTests.Infrastructure;
using static PostulaAI.IntegrationTests.Infrastructure.ApiTestClient;

namespace PostulaAI.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public sealed class AuthEndpointsTests(DatabaseApiFactory factory)
{
    private static readonly Uri RegisterUri = new("/api/v1/auth/register", UriKind.Relative);
    private static readonly Uri LoginUri = new("/api/v1/auth/login", UriKind.Relative);

    [Fact]
    public async Task Register_ReturnsAccessToken_AndSetsSecureHttpOnlyStrictRefreshCookie()
    {
        using var client = factory.CreateApiClient();
        var email = UniqueEmail();

        using var response = await client.PostAsJsonAsync(RegisterUri, new RegisterRequest($"  {email.ToUpperInvariant()} ", DefaultPassword, " Ana Pérez "), Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = await ReadSessionAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(session.Auth.AccessToken));
        Assert.Equal(email, session.Auth.User.Email);
        Assert.Equal("Ana Pérez", session.Auth.User.DisplayName);

        var cookie = (GetRefreshCookieHeader(response) ?? throw new InvalidOperationException("Missing refresh cookie.")).ToUpperInvariant();
        Assert.Contains("HTTPONLY", cookie, StringComparison.Ordinal);
        Assert.Contains("SECURE", cookie, StringComparison.Ordinal);
        Assert.Contains("SAMESITE=STRICT", cookie, StringComparison.Ordinal);
        Assert.Contains("PATH=/API/V1/AUTH", cookie, StringComparison.Ordinal);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(session.RefreshToken, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_StoresPasswordHash_AndRefreshTokenHash_NeverPlainValues()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);

        var (passwordHash, tokenHashes) = await factory.WithDbAsync(async db =>
        {
            var user = await db.Users.SingleAsync(candidate => candidate.Id == session.UserId);
            var hashes = await db.RefreshTokens.Where(token => token.UserId == session.UserId).Select(token => token.TokenHash).ToListAsync();
            return (user.PasswordHash, hashes);
        });

        Assert.NotEqual(DefaultPassword, passwordHash);
        Assert.DoesNotContain(DefaultPassword, passwordHash, StringComparison.Ordinal);
        var tokenHash = Assert.Single(tokenHashes);
        Assert.NotEqual(session.RefreshToken, tokenHash);
        Assert.Equal(64, tokenHash.Length);
    }

    [Fact]
    public async Task Register_DuplicateEmailIgnoringCase_Returns409()
    {
        using var client = factory.CreateApiClient();
        var email = UniqueEmail();
        await RegisterAsync(client, email);

        using var response = await client.PostAsJsonAsync(RegisterUri, new RegisterRequest(email.ToUpperInvariant(), DefaultPassword, "Otra"), Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal("email_already_registered", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Register_InvalidInput_ReturnsValidationProblemWithFieldErrors()
    {
        using var client = factory.CreateApiClient();

        using var response = await client.PostAsJsonAsync(RegisterUri, new RegisterRequest("no-es-correo", "corta", new string('a', 101)), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal("validation_failed", problem.GetProperty("errorCode").GetString());
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("email", out _));
        Assert.True(errors.TryGetProperty("password", out _));
        Assert.True(errors.TryGetProperty("displayName", out _));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSession()
    {
        using var client = factory.CreateApiClient();
        var email = UniqueEmail();
        await RegisterAsync(client, email);

        using var response = await client.PostAsJsonAsync(LoginUri, new LoginRequest(email, DefaultPassword), Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await ReadSessionAsync(response);
        Assert.Equal(email, session.Auth.User.Email);
    }

    [Fact]
    public async Task Login_WrongPasswordOrUnknownEmail_ReturnSameGenericError()
    {
        using var client = factory.CreateApiClient();
        var email = UniqueEmail();
        await RegisterAsync(client, email);

        using var wrongPassword = await client.PostAsJsonAsync(LoginUri, new LoginRequest(email, "otra-contraseña-123"), Json);
        using var unknownEmail = await client.PostAsJsonAsync(LoginUri, new LoginRequest(UniqueEmail(), DefaultPassword), Json);

        foreach (var response in new[] { wrongPassword, unknownEmail })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var problem = await ReadProblemAsync(response);
            Assert.Equal("invalid_credentials", problem.GetProperty("errorCode").GetString());
            Assert.Null(GetRefreshCookieHeader(response));
        }
    }

    [Fact]
    public async Task Me_RequiresAccessToken()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);

        using var anonymous = await client.GetAsync(new Uri("/api/v1/auth/me", UriKind.Relative));
        using var invalidToken = await client.SendAsync(WithBearer(HttpMethod.Get, "/api/v1/auth/me", "not-a-jwt"));
        using var authenticated = await client.SendAsync(WithBearer(HttpMethod.Get, "/api/v1/auth/me", session.Auth.AccessToken));

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, invalidToken.StatusCode);
        Assert.Equal(HttpStatusCode.OK, authenticated.StatusCode);
        var user = await authenticated.Content.ReadFromJsonAsync<UserResponse>(Json);
        Assert.Equal(session.UserId, user?.Id);
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndReuseOfOldTokenRevokesTheWholeFamily()
    {
        using var client = factory.CreateApiClient();
        var original = await RegisterAsync(client);

        using var firstRefresh = await client.SendAsync(WithRefreshCookie(HttpMethod.Post, "/api/v1/auth/refresh", original.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);
        var rotated = await ReadSessionAsync(firstRefresh);
        Assert.NotEqual(original.RefreshToken, rotated.RefreshToken);

        // Replaying the already-rotated token is treated as theft: the family is revoked.
        using var replay = await client.SendAsync(WithRefreshCookie(HttpMethod.Post, "/api/v1/auth/refresh", original.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal("invalid_refresh_token", (await ReadProblemAsync(replay)).GetProperty("errorCode").GetString());

        using var afterReuse = await client.SendAsync(WithRefreshCookie(HttpMethod.Post, "/api/v1/auth/refresh", rotated.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);

        var reuseAudited = await factory.WithDbAsync(db => db.AuditLogs.AnyAsync(log =>
            log.UserId == original.UserId && log.Action == AuditActions.RefreshTokenReuseDetected));
        Assert.True(reuseAudited);
    }

    [Fact]
    public async Task Refresh_WithoutOrWithUnknownCookie_Returns401()
    {
        using var client = factory.CreateApiClient();

        using var missing = await client.PostAsync(new Uri("/api/v1/auth/refresh", UriKind.Relative), content: null);
        using var unknown = await client.SendAsync(WithRefreshCookie(HttpMethod.Post, "/api/v1/auth/refresh", "token-inexistente"));

        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken_AndClearsCookie()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);

        using var logout = await client.SendAsync(WithRefreshCookie(HttpMethod.Post, "/api/v1/auth/logout", session.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        var cleared = GetRefreshCookieHeader(logout);
        Assert.NotNull(cleared);
        Assert.Contains("expires=Thu, 01 Jan 1970", cleared, StringComparison.OrdinalIgnoreCase);

        using var refresh = await client.SendAsync(WithRefreshCookie(HttpMethod.Post, "/api/v1/auth/refresh", session.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task AuditLog_RecordsAuthEvents_WithoutPersonalContent()
    {
        using var client = factory.CreateApiClient();
        var email = UniqueEmail();
        var session = await RegisterAsync(client, email);
        using var login = await client.PostAsJsonAsync(LoginUri, new LoginRequest(email, DefaultPassword), Json);

        var logs = await factory.WithDbAsync(db => db.AuditLogs.Where(log => log.UserId == session.UserId).ToListAsync());

        Assert.Contains(logs, log => log.Action == AuditActions.UserRegistered);
        Assert.Contains(logs, log => log.Action == AuditActions.LoginSucceeded);
        Assert.All(logs, log =>
        {
            Assert.DoesNotContain(email, log.Action, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(email, log.EntityType ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        });
    }
}
