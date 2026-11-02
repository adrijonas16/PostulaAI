using PostulaAI.Api.Modules.ProfessionalProfiles.Contracts;
using PostulaAI.Api.Modules.ProfessionalProfiles.Domain;

namespace PostulaAI.Api.Modules.ProfessionalProfiles.Import;

public sealed record ExperienceDraft(
    string Company,
    string Role,
    string? Location,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    string? Description,
    IReadOnlyList<string> Achievements,
    IReadOnlyList<string> Technologies);

public sealed record EducationDraft(string Institution, string Degree, string? FieldOfStudy, DateOnly? StartDate, DateOnly? EndDate, EducationStatus Status);

public sealed record LanguageDraft(string Language, LanguageProficiency? Proficiency);

/// <summary>Profile pre-filled from a CV. Not saved: the user reviews it, completes missing data and saves with PUT.</summary>
public sealed record ProfileDraft(
    string FullName,
    string? Headline,
    string? Summary,
    ProfileLinksDto Links,
    IReadOnlyList<ExperienceDraft> Experiences,
    IReadOnlyList<EducationDraft> Education,
    IReadOnlyList<ProjectDto> Projects,
    IReadOnlyList<CertificationDto> Certifications,
    IReadOnlyList<string> TechnicalSkills,
    IReadOnlyList<string> SoftSkills,
    IReadOnlyList<LanguageDraft> Languages);

public sealed record CvImportResponse(ProfileDraft Draft, IReadOnlyList<string> Warnings);
