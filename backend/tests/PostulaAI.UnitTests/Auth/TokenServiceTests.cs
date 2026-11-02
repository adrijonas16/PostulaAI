using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PostulaAI.Api.Modules.Auth;
using PostulaAI.Api.Modules.Users.Domain;

namespace PostulaAI.UnitTests.Auth;

public sealed class TokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly JwtOptions options = new() { SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)) };

    private TokenService CreateService() => new(Options.Create(options), new FixedTimeProvider(Now));

    private static User SampleUser() => new() { Email = "persona@example.test", PasswordHash = "x", DisplayName = "Persona" };

    [Fact]
    public async Task CreateAccessToken_ProducesSignedJwt_WithSubjectIssuerAudienceAndShortLifetime()
    {
        var user = SampleUser();

        var token = CreateService().CreateAccessToken(user);

        Assert.Equal(Now.AddMinutes(options.AccessTokenMinutes), token.ExpiresAt);
        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, new TokenValidationParameters
        {
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(options.GetSigningKeyBytes()),
            ValidateLifetime = false,
        });
        Assert.True(validation.IsValid, validation.Exception?.Message);
        Assert.Equal(user.Id.ToString(), validation.Claims[JwtRegisteredClaimNames.Sub]);
    }

    [Fact]
    public async Task CreateAccessToken_IsRejected_WithADifferentKey()
    {
        var token = CreateService().CreateAccessToken(SampleUser());

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, new TokenValidationParameters
        {
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(48)),
            ValidateLifetime = false,
        });

        Assert.False(validation.IsValid);
    }

    [Fact]
    public void AccessToken_DoesNotContainEmailOrDisplayName()
    {
        var token = CreateService().CreateAccessToken(SampleUser());

        var payload = new JsonWebToken(token.Token);

        Assert.DoesNotContain(payload.Claims, claim => claim.Value.Contains("persona@example.test", StringComparison.Ordinal));
        Assert.DoesNotContain(payload.Claims, claim => claim.Value == "Persona");
    }

    [Fact]
    public void GenerateRefreshToken_IsRandom_AndOnlyTheHashIsDeterministic()
    {
        var service = CreateService();

        var first = service.GenerateRefreshToken();
        var second = service.GenerateRefreshToken();

        Assert.NotEqual(first.Token, second.Token);
        Assert.True(first.Token.Length >= 43, "Refresh tokens must carry at least 256 bits of entropy.");
        Assert.NotEqual(first.Token, first.TokenHash);
        Assert.Equal(64, first.TokenHash.Length);
        Assert.Equal(first.TokenHash, service.HashRefreshToken(first.Token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("corta")]
    [InlineData("1234567890123456789012345678901")]
    public void JwtOptions_RejectsSigningKeysShorterThan32Bytes(string key)
    {
        Assert.False(new JwtOptions { SigningKey = key }.HasValidSigningKey());
    }
}
