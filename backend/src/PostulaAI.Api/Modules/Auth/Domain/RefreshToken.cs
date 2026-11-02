using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PostulaAI.Api.Modules.Users.Domain;

namespace PostulaAI.Api.Modules.Auth.Domain;

/// <summary>
/// Opaque refresh token. Only the SHA-256 hash is stored. Tokens issued from the same login share a
/// <see cref="FamilyId"/>; presenting an already-rotated token revokes the whole family.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid UserId { get; init; }

    public required string TokenHash { get; init; }

    public Guid FamilyId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? ReplacedByTokenId { get; set; }
}

public enum RefreshTokenState
{
    Active,
    Expired,

    /// <summary>Revoked by logout or by reuse detection.</summary>
    Revoked,

    /// <summary>Already rotated and presented again: possible theft.</summary>
    Reused,
}

public static class RefreshTokenPolicy
{
    public static RefreshTokenState Evaluate(RefreshToken token, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (token.ReplacedByTokenId is not null)
        {
            return RefreshTokenState.Reused;
        }

        if (token.RevokedAt is not null)
        {
            return RefreshTokenState.Revoked;
        }

        return token.ExpiresAt <= now ? RefreshTokenState.Expired : RefreshTokenState.Active;
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();
        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.FamilyId);
        builder.HasOne<User>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
