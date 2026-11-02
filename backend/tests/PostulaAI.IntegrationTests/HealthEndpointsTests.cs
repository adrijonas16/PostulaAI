using System.Net;
using System.Net.Http.Json;
using PostulaAI.Api.Modules.Auth.Contracts;
using PostulaAI.IntegrationTests.Infrastructure;

namespace PostulaAI.IntegrationTests;

public sealed class HealthEndpointsTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task Live_ReturnsOk_WithoutTouchingTheDatabase()
    {
        using var client = factory.CreateApiClient();

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_IsServed_InDevelopment()
    {
        using var client = factory.CreateApiClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnknownRoute_ReturnsProblemDetails()
    {
        using var client = factory.CreateApiClient();

        using var response = await client.GetAsync(new Uri("/api/v1/does-not-exist", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}

public sealed class RateLimitingTests(RateLimitedApiFactory factory) : IClassFixture<RateLimitedApiFactory>
{
    [Fact]
    public async Task AuthEndpoints_AreRateLimitedPerClient()
    {
        using var client = factory.CreateApiClient();
        var uri = new Uri("/api/v1/auth/login", UriKind.Relative);
        var invalid = new LoginRequest(string.Empty, string.Empty);

        for (var attempt = 0; attempt < RateLimitedApiFactory.PermitLimit; attempt++)
        {
            using var allowed = await client.PostAsJsonAsync(uri, invalid, ApiTestClient.Json);
            Assert.Equal(HttpStatusCode.BadRequest, allowed.StatusCode);
        }

        using var rejected = await client.PostAsJsonAsync(uri, invalid, ApiTestClient.Json);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        var problem = await ApiTestClient.ReadProblemAsync(rejected);
        Assert.Equal("rate_limited", problem.GetProperty("errorCode").GetString());
    }
}
