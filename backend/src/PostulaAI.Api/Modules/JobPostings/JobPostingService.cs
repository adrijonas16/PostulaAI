using Microsoft.EntityFrameworkCore;
using PostulaAI.Api.Modules.AuditLogs;
using PostulaAI.Api.Modules.AuditLogs.Domain;
using PostulaAI.Api.Modules.JobPostings.Contracts;
using PostulaAI.Api.Modules.JobPostings.Domain;
using PostulaAI.Api.Shared.Errors;
using PostulaAI.Api.Shared.Persistence;

namespace PostulaAI.Api.Modules.JobPostings;

/// <summary>Every query is scoped to <c>userId</c>: another user's posting is indistinguishable from a missing one.</summary>
public sealed class JobPostingService(AppDbContext db, IAuditTrail audit, TimeProvider time)
{
    private static readonly AppError NotFound = AppError.NotFound(ErrorCodes.JobPostingNotFound, "No encontramos esa vacante.");

    public async Task<PagedResponse<JobPostingSummary>> ListAsync(Guid userId, JobPostingListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var postings = db.JobPostings.AsNoTracking().Where(posting => posting.UserId == userId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLikePattern(query.Search.Trim())}%";
            postings = postings.Where(posting =>
                EF.Functions.ILike(posting.Title, pattern, "\\")
                || (posting.Company != null && EF.Functions.ILike(posting.Company, pattern, "\\")));
        }

        var totalCount = await postings.CountAsync(cancellationToken);
        var items = await postings
            .OrderByDescending(posting => posting.CreatedAt)
            .ThenByDescending(posting => posting.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(posting => new JobPostingSummary(
                posting.Id, posting.Title, posting.Company, posting.Location, posting.WorkMode, posting.CreatedAt, posting.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<JobPostingSummary>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<Result<JobPostingResponse>> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var posting = await db.JobPostings.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id && candidate.UserId == userId, cancellationToken);
        if (posting is null)
        {
            return NotFound;
        }

        return ToResponse(posting);
    }

    public async Task<JobPostingResponse> CreateAsync(Guid userId, JobPostingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = time.GetUtcNow();
        var posting = new JobPosting
        {
            UserId = userId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        Apply(request, posting);

        db.JobPostings.Add(posting);
        audit.Record(AuditActions.JobPostingCreated, userId, nameof(JobPosting), posting.Id);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(posting);
    }

    public async Task<Result<JobPostingResponse>> UpdateAsync(Guid userId, Guid id, JobPostingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var posting = await db.JobPostings
            .SingleOrDefaultAsync(candidate => candidate.Id == id && candidate.UserId == userId, cancellationToken);
        if (posting is null)
        {
            return NotFound;
        }

        Apply(request, posting);
        posting.UpdatedAt = time.GetUtcNow();
        audit.Record(AuditActions.JobPostingUpdated, userId, nameof(JobPosting), posting.Id);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(posting);
    }

    /// <summary>Returns false when the posting does not exist or belongs to another user.</summary>
    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var posting = await db.JobPostings
            .SingleOrDefaultAsync(candidate => candidate.Id == id && candidate.UserId == userId, cancellationToken);
        if (posting is null)
        {
            return false;
        }

        db.JobPostings.Remove(posting);
        audit.Record(AuditActions.JobPostingDeleted, userId, nameof(JobPosting), posting.Id);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Escapes LIKE wildcards so user input is matched literally.</summary>
    public static string EscapeLikePattern(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }

    private static void Apply(JobPostingRequest request, JobPosting posting)
    {
        posting.Title = request.Title.Trim();
        posting.Company = Clean(request.Company);
        posting.Location = Clean(request.Location);
        posting.WorkMode = request.WorkMode;
        posting.Description = request.Description.Trim();
        posting.SourceUrl = Clean(request.SourceUrl);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static JobPostingResponse ToResponse(JobPosting posting) => new(
        posting.Id,
        posting.Title,
        posting.Company,
        posting.Location,
        posting.WorkMode,
        posting.Description,
        posting.SourceUrl,
        posting.CreatedAt,
        posting.UpdatedAt);
}
