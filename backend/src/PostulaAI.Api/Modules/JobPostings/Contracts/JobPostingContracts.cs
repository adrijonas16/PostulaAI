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
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>List item without the (long) description.</summary>
public sealed record JobPostingSummary(
    Guid Id,
    string Title,
    string? Company,
    string? Location,
    WorkMode WorkMode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record JobPostingListQuery(int Page = 1, int PageSize = 20, string? Search = null);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
