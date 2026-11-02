using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PostulaAI.Api.Modules.AuditLogs.Domain;

/// <summary>
/// Security-relevant event. Holds identifiers only: never profile, job posting, e-mail or prompt content.
/// Not linked by foreign key to users so the trail survives account deletion (UserId is anonymized then).
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid? UserId { get; set; }

    public required string Action { get; init; }

    public string? EntityType { get; init; }

    public Guid? EntityId { get; init; }

    public string? CorrelationId { get; init; }

    public DateTimeOffset OccurredAt { get; init; }
}

public static class AuditActions
{
    public const string UserRegistered = "user.registered";
    public const string LoginSucceeded = "auth.login_succeeded";
    public const string LoginFailed = "auth.login_failed";
    public const string RefreshTokenReuseDetected = "auth.refresh_token_reuse_detected";
    public const string LoggedOut = "auth.logged_out";
    public const string ProfileUpdated = "profile.updated";
    public const string JobPostingCreated = "job_posting.created";
    public const string JobPostingUpdated = "job_posting.updated";
    public const string JobPostingDeleted = "job_posting.deleted";
    public const string JobPostingApplicationUpdated = "job_posting.application_updated";
    public const string AnalysisCompleted = "analysis.completed";
    public const string AnalysisFailed = "analysis.failed";
    public const string AnalysisDeleted = "analysis.deleted";
    public const string CvImported = "profile.cv_imported";
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).ValueGeneratedNever();
        builder.Property(log => log.Action).HasMaxLength(100).IsRequired();
        builder.Property(log => log.EntityType).HasMaxLength(100);
        builder.Property(log => log.CorrelationId).HasMaxLength(100);
        builder.HasIndex(log => new { log.UserId, log.OccurredAt });
    }
}
