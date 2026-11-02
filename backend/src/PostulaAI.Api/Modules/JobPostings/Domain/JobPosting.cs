using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PostulaAI.Api.Modules.Users.Domain;

namespace PostulaAI.Api.Modules.JobPostings.Domain;

public enum WorkMode
{
    Unspecified,
    Onsite,
    Hybrid,
    Remote,
}

/// <summary>A job posting pasted by the user. Owned by exactly one user; never visible to others.</summary>
public sealed class JobPosting
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid UserId { get; init; }

    public required string Title { get; set; }

    public string? Company { get; set; }

    public string? Location { get; set; }

    public WorkMode WorkMode { get; set; }

    public required string Description { get; set; }

    public string? SourceUrl { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public static class JobPostingLimits
{
    public const int Title = 200;
    public const int Company = 200;
    public const int Location = 200;
    public const int DescriptionMin = 30;
    public const int DescriptionMax = 20_000;
    public const int Url = 500;
    public const int Search = 100;
    public const int MaxPageSize = 50;
}

internal sealed class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
{
    public void Configure(EntityTypeBuilder<JobPosting> builder)
    {
        builder.ToTable("job_postings");
        builder.HasKey(posting => posting.Id);
        builder.Property(posting => posting.Id).ValueGeneratedNever();
        builder.HasOne<User>().WithMany().HasForeignKey(posting => posting.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(posting => new { posting.UserId, posting.CreatedAt });

        builder.Property(posting => posting.Title).HasMaxLength(JobPostingLimits.Title).IsRequired();
        builder.Property(posting => posting.Company).HasMaxLength(JobPostingLimits.Company);
        builder.Property(posting => posting.Location).HasMaxLength(JobPostingLimits.Location);
        builder.Property(posting => posting.WorkMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(posting => posting.Description).HasMaxLength(JobPostingLimits.DescriptionMax).IsRequired();
        builder.Property(posting => posting.SourceUrl).HasMaxLength(JobPostingLimits.Url);
    }
}
