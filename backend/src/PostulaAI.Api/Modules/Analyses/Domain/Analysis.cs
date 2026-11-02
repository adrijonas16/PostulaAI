using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PostulaAI.Api.Modules.JobPostings.Domain;
using PostulaAI.Api.Modules.ProfessionalProfiles.Domain;
using PostulaAI.Api.Modules.Users.Domain;

namespace PostulaAI.Api.Modules.Analyses.Domain;

public enum AnalysisStatus
{
    Pending,
    Completed,
    Failed,
}

/// <summary>
/// One analysis run. Snapshots freeze the profile and posting that were analysed. <see cref="ResultJson"/> is only
/// set when the model output passed schema, business-rule and grounding validation.
/// </summary>
public sealed class Analysis
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid UserId { get; init; }

    public Guid? ProfileId { get; set; }

    public Guid? JobPostingId { get; set; }

    public AnalysisStatus Status { get; set; }

    public int? CompatibilityScore { get; set; }

    public string? ResultJson { get; set; }

    public required string ProfileSnapshot { get; init; }

    public required string JobPostingSnapshot { get; init; }

    /// <summary>Denormalized from the snapshot for listing without parsing JSON.</summary>
    public required string JobTitle { get; init; }

    public string? JobCompany { get; init; }

    public required string PromptId { get; set; }

    public required string PromptVersion { get; set; }

    public required string Model { get; set; }

    public required string Language { get; init; }

    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }

    public int LatencyMs { get; set; }

    public bool RepairAttempted { get; set; }

    /// <summary>Whether the request reached the AI provider; only those attempts count toward usage limits.</summary>
    public bool ProviderCalled { get; set; }

    public string? ErrorCode { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; set; }
}

internal sealed class AnalysisConfiguration : IEntityTypeConfiguration<Analysis>
{
    public void Configure(EntityTypeBuilder<Analysis> builder)
    {
        builder.ToTable("analyses");
        builder.HasKey(analysis => analysis.Id);
        builder.Property(analysis => analysis.Id).ValueGeneratedNever();
        builder.HasOne<User>().WithMany().HasForeignKey(analysis => analysis.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProfessionalProfile>().WithMany().HasForeignKey(analysis => analysis.ProfileId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<JobPosting>().WithMany().HasForeignKey(analysis => analysis.JobPostingId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(analysis => new { analysis.UserId, analysis.CreatedAt });

        builder.Property(analysis => analysis.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(analysis => analysis.ResultJson).HasColumnType("jsonb");
        builder.Property(analysis => analysis.ProfileSnapshot).HasColumnType("jsonb").IsRequired();
        builder.Property(analysis => analysis.JobPostingSnapshot).HasColumnType("jsonb").IsRequired();
        builder.Property(analysis => analysis.JobTitle).HasMaxLength(JobPostingLimits.Title).IsRequired();
        builder.Property(analysis => analysis.JobCompany).HasMaxLength(JobPostingLimits.Company);
        builder.Property(analysis => analysis.PromptId).HasMaxLength(100).IsRequired();
        builder.Property(analysis => analysis.PromptVersion).HasMaxLength(20).IsRequired();
        builder.Property(analysis => analysis.Model).HasMaxLength(100).IsRequired();
        builder.Property(analysis => analysis.Language).HasMaxLength(10).IsRequired();
        builder.Property(analysis => analysis.ErrorCode).HasMaxLength(50);
    }
}
