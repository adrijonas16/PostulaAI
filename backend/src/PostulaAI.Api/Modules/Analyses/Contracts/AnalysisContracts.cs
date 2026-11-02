using PostulaAI.AI.Contracts;
using PostulaAI.Api.Modules.Analyses.Domain;

namespace PostulaAI.Api.Modules.Analyses.Contracts;

public sealed record CreateAnalysisRequest(Guid JobPostingId, string? Language);

/// <summary>Score band and requirement counts so the UI never shows a bare percentage.</summary>
public sealed record ScoreContext(
    string Band,
    int RequiredMet,
    int RequiredMissing,
    int PreferredMet,
    int PreferredMissing);

public sealed record AnalysisResponse(
    Guid Id,
    AnalysisStatus Status,
    Guid? JobPostingId,
    string JobTitle,
    string? JobCompany,
    string Language,
    JobAnalysisResult? Result,
    ScoreContext? Score,
    string? ErrorCode,
    string PromptId,
    string PromptVersion,
    string Model,
    bool RepairAttempted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record AnalysisSummary(
    Guid Id,
    AnalysisStatus Status,
    Guid? JobPostingId,
    string JobTitle,
    string? JobCompany,
    int? CompatibilityScore,
    string? ScoreBand,
    string? ErrorCode,
    DateTimeOffset CreatedAt);
