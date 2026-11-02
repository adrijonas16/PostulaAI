using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PostulaAI.Api.Modules.Auth;
using PostulaAI.Api.Modules.Auth.Contracts;

namespace PostulaAI.IntegrationTests.Infrastructure;

public sealed record TestSession(AuthResponse Auth, string RefreshToken)
{
    public Guid UserId => Auth.User.Id;
}

/// <summary>Helpers that speak the public HTTP contract, exactly as the frontend does.</summary>
public static class ApiTestClient
{
    public const string DefaultPassword = "contraseña-de-prueba-123";

    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    public static string UniqueEmail(string prefix = "usuario") => $"{prefix}.{Guid.NewGuid():N}@example.test";

    public static async Task<TestSession> RegisterAsync(HttpClient client, string? email = null, string password = DefaultPassword)
    {
        ArgumentNullException.ThrowIfNull(client);

        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/register", UriKind.Relative),
            new RegisterRequest(email ?? UniqueEmail(), password, "Persona de Prueba"),
            Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadSessionAsync(response);
    }

    public static async Task<TestSession> ReadSessionAsync(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
        Assert.NotNull(auth);
        var refreshToken = GetRefreshCookieValue(response)
            ?? throw new InvalidOperationException("The response did not set the refresh-token cookie.");
        return new TestSession(auth, refreshToken);
    }

    public static string? GetRefreshCookieHeader(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(value => value.StartsWith($"{RefreshTokenCookie.Name}=", StringComparison.Ordinal))
            : null;
    }

    public static string? GetRefreshCookieValue(HttpResponseMessage response)
    {
        var header = GetRefreshCookieHeader(response);
        if (header is null)
        {
            return null;
        }

        var nameValue = header.Split(';')[0];
        return nameValue[(RefreshTokenCookie.Name.Length + 1)..];
    }

    public static HttpRequestMessage WithRefreshCookie(HttpMethod method, string path, string refreshToken)
    {
        var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        request.Headers.Add("Cookie", $"{RefreshTokenCookie.Name}={refreshToken}");
        return request;
    }

    public static HttpRequestMessage WithBearer(HttpMethod method, string path, string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        return request;
    }

    public static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }
}
