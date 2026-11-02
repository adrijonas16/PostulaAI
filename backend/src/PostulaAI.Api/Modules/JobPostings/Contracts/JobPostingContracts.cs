using PostulaAI.Api.Modules.JobPostings.Domain;

namespace PostulaAI.Api.Modules.JobPostings.Contracts;

public sealed record JobPostingRequest(
    string Title,
    string? Company,
    string? Location,
    WorkMode WorkMode,
    string Description,
    string? SourceUrl);

public sealed record JobPostingResponse(
    Guid Id,
    string Title,
    string? Company,
    string? Location,
    WorkMode WorkMode,
    string Description,
    string? SourceUrl,
    ApplicationResponse Application,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>What the user recorded about applying to this posting.</summary>
public sealed record ApplicationResponse(
    ApplicationStatus Status,
    DateOnly? AppliedOn,
    string? Notes,
    string? SubmittedSummary,
    string? SubmittedCoverLetter,
    DateTimeOffset? UpdatedAt);

/// <summary>Replaces the whole application record. Sending <c>saved</c> with no date means "not applied yet".</summary>
public sealed record ApplicationRequest(
    ApplicationStatus Status,
    DateOnly? AppliedOn,
    string? Notes,
    string? SubmittedSummary,
    string? SubmittedCoverLetter);

/// <summary>List item without the (long) description.</summary>
public sealed record JobPostingSummary(
    Guid Id,
    string Title,
    string? Company,
    string? Location,
    WorkMode WorkMode,
    string? SourceUrl,
    ApplicationStatus ApplicationStatus,
    DateOnly? AppliedOn,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record JobPostingListQuery(int Page = 1, int PageSize = 20, string? Search = null, ApplicationStatus? Status = null);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
