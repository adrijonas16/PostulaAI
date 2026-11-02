using Microsoft.EntityFrameworkCore;
using PostulaAI.Api.Modules.AuditLogs;
using PostulaAI.Api.Modules.AuditLogs.Domain;
using PostulaAI.Api.Modules.ProfessionalProfiles.Contracts;
using PostulaAI.Api.Modules.ProfessionalProfiles.Domain;
using PostulaAI.Api.Shared.Errors;
using PostulaAI.Api.Shared.Persistence;

namespace PostulaAI.Api.Modules.ProfessionalProfiles;

public sealed class ProfileService(AppDbContext db, IAuditTrail audit, TimeProvider time)
{
    public async Task<Result<ProfileResponse>> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await LoadAsync(userId, tracking: false, cancellationToken);
        if (profile is null)
        {
            return AppError.NotFound(ErrorCodes.ProfileNotFound, "Todavía no has creado tu perfil profesional.");
        }

        return ToResponse(profile);
    }

    /// <summary>Creates or fully replaces the profile of <paramref name="userId"/>. Always scoped to that user.</summary>
    public async Task<ProfileResponse> UpsertAsync(Guid userId, ProfileRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = time.GetUtcNow();
        var profile = await LoadAsync(userId, tracking: true, cancellationToken);
        if (profile is null)
        {
            profile = new ProfessionalProfile { UserId = userId, FullName = request.FullName, CreatedAt = now };
            db.ProfessionalProfiles.Add(profile);
        }

        Apply(request, profile);
        profile.UpdatedAt = now;

        audit.Record(AuditActions.ProfileUpdated, userId, nameof(ProfessionalProfile), profile.Id);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(profile);
    }

    private async Task<ProfessionalProfile?> LoadAsync(Guid userId, bool tracking, CancellationToken cancellationToken)
    {
        var query = db.ProfessionalProfiles
            .Include(profile => profile.Experiences)
            .Include(profile => profile.Education)
            .Include(profile => profile.Projects)
            .Include(profile => profile.Certifications)
            .Include(profile => profile.Skills)
            .Include(profile => profile.Languages)
            .AsSplitQuery()
            .Where(profile => profile.UserId == userId);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    private static void Apply(ProfileRequest request, ProfessionalProfile profile)
    {
        profile.FullName = request.FullName.Trim();
        profile.Headline = Clean(request.Headline);
        profile.Summary = Clean(request.Summary);
        profile.LinkedInUrl = Clean(request.Links.LinkedInUrl);
        profile.GitHubUrl = Clean(request.Links.GitHubUrl);
        profile.PortfolioUrl = Clean(request.Links.PortfolioUrl);

        profile.Experiences.Clear();
        profile.Experiences.AddRange(request.Experiences.Select((item, index) => new WorkExperience
        {
            Company = item.Company.Trim(),
            Role = item.Role.Trim(),
            Location = Clean(item.Location),
            StartDate = item.StartDate,
            EndDate = item.IsCurrent ? null : item.EndDate,
            IsCurrent = item.IsCurrent,
            Description = Clean(item.Description),
            Achievements = CleanList(item.Achievements),
            Technologies = CleanList(item.Technologies),
            SortOrder = index,
        }));

        profile.Education.Clear();
        profile.Education.AddRange(request.Education.Select((item, index) => new Education
        {
            Institution = item.Institution.Trim(),
            Degree = item.Degree.Trim(),
            FieldOfStudy = Clean(item.FieldOfStudy),
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            Status = item.Status,
            SortOrder = index,
        }));

        profile.Projects.Clear();
        profile.Projects.AddRange(request.Projects.Select((item, index) => new Project
        {
            Name = item.Name.Trim(),
            Description = Clean(item.Description),
            Technologies = CleanList(item.Technologies),
            Url = Clean(item.Url),
            SortOrder = index,
        }));

        profile.Certifications.Clear();
        profile.Certifications.AddRange(request.Certifications.Select((item, index) => new Certification
        {
            Name = item.Name.Trim(),
            Issuer = item.Issuer.Trim(),
            IssuedOn = item.IssuedOn,
            CredentialUrl = Clean(item.CredentialUrl),
            SortOrder = index,
        }));

        profile.Skills.Clear();
        profile.Skills.AddRange(CleanList(request.TechnicalSkills)
            .Select((name, index) => new ProfileSkill { Name = name, Category = SkillCategory.Technical, SortOrder = index }));
        profile.Skills.AddRange(CleanList(request.SoftSkills)
            .Select((name, index) => new ProfileSkill { Name = name, Category = SkillCategory.Soft, SortOrder = index }));

        profile.Languages.Clear();
        profile.Languages.AddRange(request.Languages.Select((item, index) => new ProfileLanguage
        {
            Language = item.Language.Trim(),
            Proficiency = item.Proficiency,
            SortOrder = index,
        }));
    }

    private static ProfileResponse ToResponse(ProfessionalProfile profile) => new(
        profile.Id,
        profile.FullName,
        profile.Headline,
        profile.Summary,
        new ProfileLinksDto(profile.LinkedInUrl, profile.GitHubUrl, profile.PortfolioUrl),
        [.. profile.Experiences.OrderBy(item => item.SortOrder).Select(item => new WorkExperienceDto(
            item.Company, item.Role, item.Location, item.StartDate, item.EndDate, item.IsCurrent, item.Description,
            item.Achievements, item.Technologies))],
        [.. profile.Education.OrderBy(item => item.SortOrder).Select(item => new EducationDto(
            item.Institution, item.Degree, item.FieldOfStudy, item.StartDate, item.EndDate, item.Status))],
        [.. profile.Projects.OrderBy(item => item.SortOrder).Select(item => new ProjectDto(
            item.Name, item.Description, item.Technologies, item.Url))],
        [.. profile.Certifications.OrderBy(item => item.SortOrder).Select(item => new CertificationDto(
            item.Name, item.Issuer, item.IssuedOn, item.CredentialUrl))],
        [.. SkillNames(profile, SkillCategory.Technical)],
        [.. SkillNames(profile, SkillCategory.Soft)],
        [.. profile.Languages.OrderBy(item => item.SortOrder).Select(item => new LanguageDto(item.Language, item.Proficiency))],
        profile.UpdatedAt);

    private static IEnumerable<string> SkillNames(ProfessionalProfile profile, SkillCategory category) =>
        profile.Skills.Where(skill => skill.Category == category).OrderBy(skill => skill.SortOrder).Select(skill => skill.Name);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<string> CleanList(IEnumerable<string> values) =>
        [.. values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim())];
}
