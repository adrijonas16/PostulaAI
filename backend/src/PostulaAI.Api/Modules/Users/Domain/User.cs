using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PostulaAI.Api.Modules.Users.Domain;

public sealed class User
{
    public const int EmailMaxLength = 254;
    public const int DisplayNameMaxLength = 100;

    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>Normalized (trimmed, lower-case) e-mail. Unique.</summary>
    public required string Email { get; set; }

    /// <summary>ASP.NET Core Identity PasswordHasher output (PBKDF2). Never the plain password.</summary>
    public required string PasswordHash { get; set; }

    public required string DisplayName { get; set; }

    /// <summary>When the user confirmed they own <see cref="Email"/>. Null until then.</summary>
    public DateTimeOffset? EmailVerifiedAt { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }

    public static string NormalizeEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return email.Trim().ToLowerInvariant();
    }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.Email).HasMaxLength(User.EmailMaxLength).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(user => user.DisplayName).HasMaxLength(User.DisplayNameMaxLength).IsRequired();
    }
}
