using PostulaAI.Api.Modules.ProfessionalProfiles.Domain;

namespace PostulaAI.Api.Modules.ProfessionalProfiles.Contracts;

public sealed record ProfileLinksDto(string? LinkedInUrl, string? GitHubUrl, string? PortfolioUrl);

public sealed record WorkExperienceDto(
    string Company,
    string Role,
    string? Location,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    string? Description,
    IReadOnlyList<string> Achievements,
    IReadOnlyList<string> Technologies);

public sealed record EducationDto(
    string Institution,
    string Degree,
    string? FieldOfStudy,
    DateOnly? StartDate,
    DateOnly? EndDate,
    EducationStatus Status);

public sealed record ProjectDto(string Name, string? Description, IReadOnlyList<string> Technologies, string? Url);

public sealed record CertificationDto(string Name, string Issuer, DateOnly? IssuedOn, string? CredentialUrl);

public sealed record LanguageDto(string Language, LanguageProficiency Proficiency);

/// <summary>Full profile replacement (PUT). Collections are always sent, possibly empty.</summary>
public sealed record ProfileRequest(
    string FullName,
    string? Headline,
    string? Summary,
    ProfileLinksDto Links,
    IReadOnlyList<WorkExperienceDto> Experiences,
    IReadOnlyList<EducationDto> Education,
    IReadOnlyList<ProjectDto> Projects,
    IReadOnlyList<CertificationDto> Certifications,
    IReadOnlyList<string> TechnicalSkills,
    IReadOnlyList<string> SoftSkills,
    IReadOnlyList<LanguageDto> Languages);

public sealed record ProfileResponse(
    Guid Id,
    string FullName,
    string? Headline,
    string? Summary,
    ProfileLinksDto Links,
    IReadOnlyList<WorkExperienceDto> Experiences,
    IReadOnlyList<EducationDto> Education,
    IReadOnlyList<ProjectDto> Projects,
    IReadOnlyList<CertificationDto> Certifications,
    IReadOnlyList<string> TechnicalSkills,
    IReadOnlyList<string> SoftSkills,
    IReadOnlyList<LanguageDto> Languages,
    DateTimeOffset UpdatedAt);
