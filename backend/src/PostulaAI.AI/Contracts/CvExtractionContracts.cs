namespace PostulaAI.AI.Contracts;

// Mirrors Schemas/cv-extraction-v1.schema.json. Dates are "YYYY-MM-DD" strings or null exactly as the model returns
// them; the API normalizes and validates them before showing a draft to the user.

public enum CvEducationStatus
{
    Completed,
    InProgress,
    Incomplete,
}

public enum CvLanguageLevel
{
    Basic,
    Intermediate,
    Advanced,
    Native,
}

public sealed record CvLinks(string? LinkedInUrl, string? GitHubUrl, string? PortfolioUrl);

public sealed record CvExperience(
    string Company,
    string Role,
    string? Location,
    string? StartDate,
    string? EndDate,
    bool IsCurrent,
    string? Description,
    IReadOnlyList<string> Achievements,
    IReadOnlyList<string> Technologies);

public sealed record CvEducation(string Institution, string Degree, string? FieldOfStudy, string? StartDate, string? EndDate, CvEducationStatus Status);

public sealed record CvProject(string Name, string? Description, IReadOnlyList<string> Technologies, string? Url);

public sealed record CvCertification(string Name, string Issuer, string? IssuedOn, string? CredentialUrl);

public sealed record CvLanguage(string Language, CvLanguageLevel? Proficiency);

public sealed record CvExtraction(
    string FullName,
    string? Headline,
    string? Summary,
    CvLinks Links,
    IReadOnlyList<CvExperience> Experiences,
    IReadOnlyList<CvEducation> Education,
    IReadOnlyList<CvProject> Projects,
    IReadOnlyList<CvCertification> Certifications,
    IReadOnlyList<string> TechnicalSkills,
    IReadOnlyList<string> SoftSkills,
    IReadOnlyList<CvLanguage> Languages);

public sealed record CvExtractionOutcome
{
    public required bool Succeeded { get; init; }

    public CvExtraction? Extraction { get; init; }

    /// <summary>User-facing notes (Spanish): items removed by the grounding check, truncated input, etc.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public string? ErrorCode { get; init; }

    public required string PromptId { get; init; }

    public required string PromptVersion { get; init; }

    public required string Model { get; init; }

    public int InputTokens { get; init; }

    public int OutputTokens { get; init; }

    public TimeSpan Latency { get; init; }

    public bool RepairAttempted { get; init; }

    public bool ProviderCalled { get; init; }
}
