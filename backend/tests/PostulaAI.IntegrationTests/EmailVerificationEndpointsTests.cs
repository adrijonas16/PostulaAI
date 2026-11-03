using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PostulaAI.Api.Modules.Auth.Contracts;
using PostulaAI.IntegrationTests.Infrastructure;
using static PostulaAI.IntegrationTests.Infrastructure.ApiTestClient;

namespace PostulaAI.IntegrationTests;

[Collection(EmailVerificationCollection.Name)]
public sealed class EmailVerificationEndpointsTests(EmailVerificationApiFactory factory)
{
    private static async Task<UserResponse> MeAsync(HttpClient client, TestSession session)
    {
        using var response = await client.SendAsync(WithBearer(HttpMethod.Get, "/api/v1/auth/me", session.Auth.AccessToken));
        return await response.Content.ReadFromJsonAsync<UserResponse>(Json) ?? throw new InvalidOperationException();
    }

    [Fact]
    public async Task Register_SendsALink_AndAiFeaturesStayLockedUntilItIsUsed()
    {
        using var client = factory.CreateApiClient();
        var email = UniqueEmail("verifica");
        var session = await RegisterAsync(client, email);

        Assert.False(session.Auth.User.EmailVerified);
        Assert.True(session.Auth.User.EmailVerificationPending);
        var message = Assert.Single(factory.Emails.Sent, sent => sent.To == email);
        Assert.Contains("https://app.test/verificar-correo?token=", message.TextBody, StringComparison.Ordinal);

        using var locked = await client.SendAsync(CvImportEndpointsTests.Upload(
            session.Auth.AccessToken, CvImportEndpointsTests.Docx(CvImportEndpointsTests.CvText)));
        Assert.Equal(HttpStatusCode.Forbidden, locked.StatusCode);
        Assert.Contains("email_not_verified", await locked.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        using var verify = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/verify-email", UriKind.Relative),
            new VerifyEmailRequest(factory.Emails.LatestVerificationToken(email)),
            Json);
        Assert.Equal(HttpStatusCode.NoContent, verify.StatusCode);

        var me = await MeAsync(client, session);
        Assert.True(me.EmailVerified);
        Assert.False(me.EmailVerificationPending);

        using var unlocked = await client.SendAsync(CvImportEndpointsTests.Upload(
            session.Auth.AccessToken, CvImportEndpointsTests.Docx(CvImportEndpointsTests.CvText)));
        Assert.Equal(HttpStatusCode.OK, unlocked.StatusCode);
    }

    [Fact]
    public async Task VerificationLink_IsSingleUse_AndUnknownTokensAreRejected()
    {
        using var client = factory.CreateApiClient();
        var email = UniqueEmail("unuso");
        await RegisterAsync(client, email);
        var token = factory.Emails.LatestVerificationToken(email);

        using var first = await client.PostAsJsonAsync(new Uri("/api/v1/auth/verify-email", UriKind.Relative), new VerifyEmailRequest(token), Json);
        using var second = await client.PostAsJsonAsync(new Uri("/api/v1/auth/verify-email", UriKind.Relative), new VerifyEmailRequest(token), Json);
        using var forged = await client.PostAsJsonAsync(new Uri("/api/v1/auth/verify-email", UriKind.Relative), new VerifyEmailRequest("inventado"), Json);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);
        Assert.Contains("invalid_verification_token", await forged.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnlyTheTokenHash_IsStored()
    {
        using var client = factory.CreateApiClient();
        var email = UniqueEmail("hash");
        var session = await RegisterAsync(client, email);
        var token = factory.Emails.LatestVerificationToken(email);

        var hashes = await factory.WithDbAsync(db => db.EmailVerificationTokens.AsNoTracking()
            .Where(stored => stored.UserId == session.UserId).Select(stored => stored.TokenHash).ToListAsync());

        Assert.Single(hashes);
        Assert.NotEqual(token, hashes[0]);
    }

    [Fact]
    public async Task Resend_RequiresAuthentication_AndHasACooldown()
    {
        using var client = factory.CreateApiClient();
        var email = UniqueEmail("reenvio");
        var session = await RegisterAsync(client, email);

        using var anonymous = await client.PostAsync(new Uri("/api/v1/auth/resend-verification", UriKind.Relative), null);
        using var tooSoon = await client.SendAsync(WithBearer(HttpMethod.Post, "/api/v1/auth/resend-verification", session.Auth.AccessToken));

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, tooSoon.StatusCode);
        Assert.Contains("verification_email_cooldown", await tooSoon.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Single(factory.Emails.Sent, sent => sent.To == email);
    }
}
