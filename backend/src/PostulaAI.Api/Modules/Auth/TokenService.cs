using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PostulaAI.Api.Modules.Users.Domain;

namespace PostulaAI.Api.Modules.Auth;

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

public sealed record GeneratedRefreshToken(string Token, string TokenHash);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);

    GeneratedRefreshToken GenerateRefreshToken();

    string HashRefreshToken(string token);
}

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider time) : ITokenService
{
    private const int RefreshTokenBytes = 32;

    private readonly JsonWebTokenHandler handler = new();

    public AccessToken CreateAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var jwt = options.Value;
        var now = time.GetUtcNow();
        var expiresAt = now.AddMinutes(jwt.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(jwt.GetSigningKeyBytes()),
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            },
        };

        return new AccessToken(handler.CreateToken(descriptor), expiresAt);
    }

    public GeneratedRefreshToken GenerateRefreshToken()
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
        return new GeneratedRefreshToken(token, HashRefreshToken(token));
    }

    public string HashRefreshToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
