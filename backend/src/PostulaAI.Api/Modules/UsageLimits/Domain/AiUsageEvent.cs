using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PostulaAI.Api.Modules.Users.Domain;

namespace PostulaAI.Api.Modules.UsageLimits.Domain;

public enum AiFeature
{
    Analysis,
    CvImport,
}

/// <summary>
/// One request that was allowed to reach the AI provider. Used for the site-wide daily budget.
/// Holds no content: only who, which feature and when.
/// </summary>
public sealed class AiUsageEvent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid UserId { get; init; }

    public AiFeature Feature { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

internal sealed class AiUsageEventConfiguration : IEntityTypeConfiguration<AiUsageEvent>
{
    public void Configure(EntityTypeBuilder<AiUsageEvent> builder)
    {
        builder.ToTable("ai_usage_events");
        builder.HasKey(usage => usage.Id);
        builder.Property(usage => usage.Id).ValueGeneratedNever();
        builder.HasOne<User>().WithMany().HasForeignKey(usage => usage.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(usage => usage.Feature).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(usage => usage.CreatedAt);
    }
}
