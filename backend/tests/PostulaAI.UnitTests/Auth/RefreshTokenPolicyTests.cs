using PostulaAI.Api.Modules.Auth.Domain;

namespace PostulaAI.UnitTests.Auth;

public sealed class RefreshTokenPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private static RefreshToken Token(DateTimeOffset? revokedAt = null, Guid? replacedBy = null, int expiresInMinutes = 60) => new()
    {
        TokenHash = new string('a', 64),
        FamilyId = Guid.NewGuid(),
        CreatedAt = Now.AddMinutes(-5),
        ExpiresAt = Now.AddMinutes(expiresInMinutes),
        RevokedAt = revokedAt,
        ReplacedByTokenId = replacedBy,
    };

    [Fact]
    public void Evaluate_FreshToken_IsActive() =>
        Assert.Equal(RefreshTokenState.Active, RefreshTokenPolicy.Evaluate(Token(), Now));

    [Fact]
    public void Evaluate_ExpiredToken_IsExpired() =>
        Assert.Equal(RefreshTokenState.Expired, RefreshTokenPolicy.Evaluate(Token(expiresInMinutes: 0), Now));

    [Fact]
    public void Evaluate_RevokedByLogout_IsRevoked() =>
        Assert.Equal(RefreshTokenState.Revoked, RefreshTokenPolicy.Evaluate(Token(revokedAt: Now), Now));

    [Fact]
    public void Evaluate_AlreadyRotatedToken_IsReuse_EvenIfNotExpired() =>
        Assert.Equal(RefreshTokenState.Reused, RefreshTokenPolicy.Evaluate(Token(revokedAt: Now, replacedBy: Guid.NewGuid()), Now));
}
