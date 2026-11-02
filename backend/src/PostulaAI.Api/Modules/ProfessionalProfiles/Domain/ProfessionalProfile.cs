using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PostulaAI.Api.Modules.Users.Domain;

namespace PostulaAI.Api.Modules.ProfessionalProfiles.Domain;

/// <summary>
/// Professional profile written manually by the user. This is the only source of evidence the AI may use.
/// v1: one profile per user (unique index on UserId); children are replaced as a whole on update.
/// </summary>
public sealed class ProfessionalProfile
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid UserId { get; init; }

    public required string FullName { get; set; }

    public string? Headline { get; set; }

    public string? Summary { get; set; }

    public string? LinkedInUrl { get; set; }

    public string? GitHubUrl { get; set; }

    public string? PortfolioUrl { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<WorkExperience> Experiences { get; init; } = [];

    public List<Education> Education { get; init; } = [];

    public List<Project> Projects { get; init; } = [];

    public List<Certification> Certifications { get; init; } = [];

    public List<ProfileSkill> Skills { get; init; } = [];

    public List<ProfileLanguage> Languages { get; init; } = [];
}

public sealed class WorkExperience
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid ProfileId { get; init; }

    public required string Company { get; set; }

    public required string Role { get; set; }

    public string? Location { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public string? Description { get; set; }

    public List<string> Achievements { get; set; } = [];

    public List<string> Technologies { get; set; } = [];

    public int SortOrder { get; set; }
}

public enum EducationStatus
{
    Completed,
    InProgress,
    Incomplete,
}

public sealed class Education
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid ProfileId { get; init; }

    public required string Institution { get; set; }

    public required string Degree { get; set; }

    public string? FieldOfStudy { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public EducationStatus Status { get; set; }

    public int SortOrder { get; set; }
}

public sealed class Project
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid ProfileId { get; init; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public List<string> Technologies { get; set; } = [];

    public string? Url { get; set; }

    public int SortOrder { get; set; }
}

public sealed class Certification
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid ProfileId { get; init; }

    public required string Name { get; set; }

    public required string Issuer { get; set; }

    public DateOnly? IssuedOn { get; set; }

    public string? CredentialUrl { get; set; }

    public int SortOrder { get; set; }
}

public enum SkillCategory
{
    Technical,
    Soft,
}

public sealed class ProfileSkill
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid ProfileId { get; init; }

    public required string Name { get; set; }

    public SkillCategory Category { get; set; }

    public int SortOrder { get; set; }
}

public enum LanguageProficiency
{
    Basic,
    Intermediate,
    Advanced,
    Native,
}

public sealed class ProfileLanguage
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid ProfileId { get; init; }

    public required string Language { get; set; }

    public LanguageProficiency Proficiency { get; set; }

    public int SortOrder { get; set; }
}

public static class ProfileLimits
{
    public const int FullName = 150;
    public const int Headline = 200;
    public const int Summary = 3000;
    public const int Url = 500;
    public const int ShortText = 150;
    public const int MediumText = 200;
    public const int Description = 4000;
    public const int ListItem = 500;
    public const int Tag = 100;

    public const int MaxExperiences = 30;
    public const int MaxEducation = 20;
    public const int MaxProjects = 30;
    public const int MaxCertifications = 30;
    public const int MaxAchievements = 20;
    public const int MaxTechnologies = 40;
    public const int MaxTechnicalSkills = 100;
    public const int MaxSoftSkills = 50;
    public const int MaxLanguages = 20;
}

internal sealed class ProfessionalProfileConfiguration : IEntityTypeConfiguration<ProfessionalProfile>
{
    public void Configure(EntityTypeBuilder<ProfessionalProfile> builder)
    {
        builder.ToTable("professional_profiles");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();
        builder.HasIndex(profile => profile.UserId).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(profile => profile.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(profile => profile.FullName).HasMaxLength(ProfileLimits.FullName).IsRequired();
        builder.Property(profile => profile.Headline).HasMaxLength(ProfileLimits.Headline);
        builder.Property(profile => profile.Summary).HasMaxLength(ProfileLimits.Summary);
        builder.Property(profile => profile.LinkedInUrl).HasMaxLength(ProfileLimits.Url);
        builder.Property(profile => profile.GitHubUrl).HasMaxLength(ProfileLimits.Url);
        builder.Property(profile => profile.PortfolioUrl).HasMaxLength(ProfileLimits.Url);

        builder.HasMany(profile => profile.Experiences).WithOne().HasForeignKey(item => item.ProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(profile => profile.Education).WithOne().HasForeignKey(item => item.ProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(profile => profile.Projects).WithOne().HasForeignKey(item => item.ProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(profile => profile.Certifications).WithOne().HasForeignKey(item => item.ProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(profile => profile.Skills).WithOne().HasForeignKey(item => item.ProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(profile => profile.Languages).WithOne().HasForeignKey(item => item.ProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WorkExperienceConfiguration : IEntityTypeConfiguration<WorkExperience>
{
    public void Configure(EntityTypeBuilder<WorkExperience> builder)
    {
        builder.ToTable("work_experiences");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Company).HasMaxLength(ProfileLimits.ShortText).IsRequired();
        builder.Property(item => item.Role).HasMaxLength(ProfileLimits.ShortText).IsRequired();
        builder.Property(item => item.Location).HasMaxLength(ProfileLimits.ShortText);
        builder.Property(item => item.Description).HasMaxLength(ProfileLimits.Description);
    }
}

internal sealed class EducationConfiguration : IEntityTypeConfiguration<Education>
{
    public void Configure(EntityTypeBuilder<Education> builder)
    {
        builder.ToTable("education");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Institution).HasMaxLength(ProfileLimits.MediumText).IsRequired();
        builder.Property(item => item.Degree).HasMaxLength(ProfileLimits.MediumText).IsRequired();
        builder.Property(item => item.FieldOfStudy).HasMaxLength(ProfileLimits.MediumText);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
    }
}

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Name).HasMaxLength(ProfileLimits.ShortText).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(ProfileLimits.Summary);
        builder.Property(item => item.Url).HasMaxLength(ProfileLimits.Url);
    }
}

internal sealed class CertificationConfiguration : IEntityTypeConfiguration<Certification>
{
    public void Configure(EntityTypeBuilder<Certification> builder)
    {
        builder.ToTable("certifications");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Name).HasMaxLength(ProfileLimits.MediumText).IsRequired();
        builder.Property(item => item.Issuer).HasMaxLength(ProfileLimits.MediumText).IsRequired();
        builder.Property(item => item.CredentialUrl).HasMaxLength(ProfileLimits.Url);
    }
}

internal sealed class ProfileSkillConfiguration : IEntityTypeConfiguration<ProfileSkill>
{
    public void Configure(EntityTypeBuilder<ProfileSkill> builder)
    {
        builder.ToTable("profile_skills");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Name).HasMaxLength(ProfileLimits.Tag).IsRequired();
        builder.Property(item => item.Category).HasConversion<string>().HasMaxLength(20);
    }
}

internal sealed class ProfileLanguageConfiguration : IEntityTypeConfiguration<ProfileLanguage>
{
    public void Configure(EntityTypeBuilder<ProfileLanguage> builder)
    {
        builder.ToTable("profile_languages");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Language).HasMaxLength(ProfileLimits.Tag).IsRequired();
        builder.Property(item => item.Proficiency).HasConversion<string>().HasMaxLength(20);
    }
}
