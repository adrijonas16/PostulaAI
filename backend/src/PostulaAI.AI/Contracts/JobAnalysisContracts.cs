namespace PostulaAI.AI.Contracts;

// ----- Input: provider-neutral snapshot of the data the analysis may use. -----

public sealed record CandidateExperience(
    string Company,
    string Role,
    string? Location,
    string StartDate,
    string? EndDate,
    bool IsCurrent,
    string? Description,
    IReadOnlyList<string> Achievements,
    IReadOnlyList<string> Technologies);

public sealed record CandidateEducation(string Institution, string Degree, string? FieldOfStudy, string? StartDate, string? EndDate, string Status);

public sealed record CandidateProject(string Name, string? Description, IReadOnlyList<string> Technologies, string? Url);

public sealed record CandidateCertification(string Name, string Issuer, string? IssuedOn);

public sealed record CandidateLanguage(string Language, string Proficiency);

/// <summary>The candidate profile exactly as the user wrote it. It is the only evidence the model may use.</summary>
public sealed record CandidateProfile(
    string FullName,
    string? Headline,
    string? Summary,
    IReadOnlyList<CandidateExperience> Experiences,
    IReadOnlyList<CandidateEducation> Education,
    IReadOnlyList<CandidateProject> Projects,
    IReadOnlyList<CandidateCertification> Certifications,
    IReadOnlyList<string> TechnicalSkills,
    IReadOnlyList<string> SoftSkills,
    IReadOnlyList<CandidateLanguage> Languages);

public sealed record JobPostingInput(string Title, string? Company, string? Location, string WorkMode, string Description);

public sealed record JobAnalysisRequest(CandidateProfile Profile, JobPostingInput JobPosting, string Language);

// ----- Output: mirrors Schemas/job-analysis-v1.schema.json (kept in sync by a unit test). -----

public enum RequirementImportance
{
    Required,
    Preferred,
}

public enum GapType
{
    MissingSkill,
    MissingExperience,
    UnclearEvidence,
    Other,
}

public sealed record AnalysisStrength(string Requirement, string Evidence, RequirementImportance Importance);

public sealed record AnalysisGap(string Requirement, GapType GapType, RequirementImportance Importance, string Recommendation);

public sealed record JobAnalysisResult(
    int CompatibilityScore,
    string Summary,
    IReadOnlyList<AnalysisStrength> Strengths,
    IReadOnlyList<AnalysisGap> Gaps,
    IReadOnlyList<string> KeywordsToInclude,
    IReadOnlyList<string> CvRecommendations,
    string AdaptedProfessionalSummary,
    string CoverLetterDraft,
    string Disclaimer);

// ----- Outcome of one analysis run (production and evals). -----

public static class AnalysisErrorCodes
{
    public const string InvalidOutput = "ai_invalid_output";
    public const string NotConfigured = "ai_not_configured";
    public const string RateLimited = "ai_rate_limited";
    public const string Unavailable = "ai_unavailable";
    public const string Refused = "ai_refused";
    public const string RequestRejected = "ai_request_rejected";
}

public sealed record JobAnalysisOutcome
{
    public required bool Succeeded { get; init; }

    public JobAnalysisResult? Result { get; init; }

    /// <summary>Raw JSON returned by the model and accepted by every validator. Null when the analysis failed.</summary>
    public string? ResultJson { get; init; }

    public string? ErrorCode { get; init; }

    /// <summary>Validation problems of the last attempt (no personal content beyond what the model returned).</summary>
    public IReadOnlyList<string> ValidationIssues { get; init; } = [];

    public required string PromptId { get; init; }

    public required string PromptVersion { get; init; }

    public required string Model { get; init; }

    public required string Language { get; init; }

    public int InputTokens { get; init; }

    public int OutputTokens { get; init; }

    public TimeSpan Latency { get; init; }

    public bool RepairAttempted { get; init; }

    /// <summary>False when the request never reached the provider (for example, missing configuration).</summary>
    public bool ProviderCalled { get; init; }
}
