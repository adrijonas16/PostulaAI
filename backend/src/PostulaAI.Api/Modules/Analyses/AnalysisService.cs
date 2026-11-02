using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PostulaAI.AI.Contracts;
using PostulaAI.AI.Services;
using PostulaAI.Api.Modules.Analyses.Contracts;
using PostulaAI.Api.Modules.Analyses.Domain;
using PostulaAI.Api.Modules.AuditLogs;
using PostulaAI.Api.Modules.AuditLogs.Domain;
using PostulaAI.Api.Modules.JobPostings;
using PostulaAI.Api.Modules.JobPostings.Contracts;
using PostulaAI.Api.Modules.ProfessionalProfiles;
using PostulaAI.Api.Modules.ProfessionalProfiles.Contracts;
using PostulaAI.Api.Modules.UsageLimits;
using PostulaAI.Api.Shared.Errors;
using PostulaAI.Api.Shared.Persistence;

namespace PostulaAI.Api.Modules.Analyses;

/// <summary>
/// Runs an analysis for the authenticated user: ownership checks, usage limit, immutable snapshots, AI pipeline
/// (PostulaAI.AI) and persistence. Only validated results are stored. Logs carry identifiers and metrics only.
/// </summary>
public sealed partial class AnalysisService(
    AppDbContext db,
    ProfileService profiles,
    JobPostingService jobPostings,
    UsageLimitService usage,
    JobAnalysisService analyzer,
    IAuditTrail audit,
    TimeProvider time,
    ILogger<AnalysisService> logger)
{
    public const string DefaultLanguage = "es";

    private static readonly AppError NotFound = AppError.NotFound(ErrorCodes.AnalysisNotFound, "No encontramos ese análisis.");

    private static readonly JsonSerializerOptions SnapshotJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    public async Task<Result<AnalysisResponse>> CreateAsync(Guid userId, CreateAnalysisRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await profiles.GetAsync(userId, cancellationToken);
        if (!profile.IsSuccess)
        {
            return new AppError(ErrorCodes.ProfileRequired, StatusCodes.Status409Conflict,
                "Completa tu perfil profesional antes de pedir un análisis.");
        }

        var posting = await jobPostings.GetAsync(userId, request.JobPostingId, cancellationToken);
        if (!posting.IsSuccess)
        {
            return posting.Error;
        }

        var currentUsage = await usage.GetUsageAsync(userId, cancellationToken);
        if (currentUsage.Remaining == 0)
        {
            return new AppError(ErrorCodes.UsageLimitReached, StatusCodes.Status429TooManyRequests,
                $"Alcanzaste el límite de {currentUsage.Limit} análisis cada {currentUsage.WindowHours} horas. Intenta más tarde.");
        }

        var language = (request.Language ?? DefaultLanguage).ToLowerInvariant();
        var analysis = new Analysis
        {
            UserId = userId,
            ProfileId = profile.Value.Id,
            JobPostingId = posting.Value.Id,
            Status = AnalysisStatus.Pending,
            ProfileSnapshot = JsonSerializer.Serialize(profile.Value, SnapshotJson),
            JobPostingSnapshot = JsonSerializer.Serialize(posting.Value, SnapshotJson),
            JobTitle = posting.Value.Title,
            JobCompany = posting.Value.Company,
            PromptId = "pending",
            PromptVersion = "pending",
            Model = "pending",
            Language = language,
            CreatedAt = time.GetUtcNowForStorage(),
        };
        db.Analyses.Add(analysis);
        await db.SaveChangesAsync(cancellationToken);

        JobAnalysisOutcome outcome;
        try
        {
            // Not tied to the HTTP request: if the browser disconnects, the analysis still finishes and is saved.
            outcome = await analyzer.AnalyzeAsync(
                new JobAnalysisRequest(ToCandidate(profile.Value), ToPostingInput(posting.Value), language),
                CancellationToken.None);
        }
        catch (Exception)
        {
            analysis.Status = AnalysisStatus.Failed;
            analysis.ErrorCode = ErrorCodes.InternalError;
            analysis.CompletedAt = time.GetUtcNowForStorage();
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        Apply(outcome, analysis);
        audit.Record(outcome.Succeeded ? AuditActions.AnalysisCompleted : AuditActions.AnalysisFailed, userId, nameof(Analysis), analysis.Id);
        await db.SaveChangesAsync(CancellationToken.None);
        LogOutcome(logger, analysis.Id, analysis.Status, analysis.ErrorCode, analysis.Model, analysis.PromptVersion,
            analysis.InputTokens, analysis.OutputTokens, analysis.LatencyMs, analysis.RepairAttempted);

        if (!outcome.Succeeded)
        {
            return ErrorFor(outcome.ErrorCode);
        }

        return ToResponse(analysis);
    }

    public async Task<PagedResponse<AnalysisSummary>> ListAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.Analyses.AsNoTracking().Where(analysis => analysis.UserId == userId);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(analysis => analysis.CreatedAt)
            .ThenByDescending(analysis => analysis.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(analysis => new { analysis.Id, analysis.Status, analysis.JobPostingId, analysis.JobTitle, analysis.JobCompany, analysis.CompatibilityScore, analysis.ErrorCode, analysis.CreatedAt })
            .ToListAsync(cancellationToken);

        var items = rows.Select(row => new AnalysisSummary(
            row.Id, row.Status, row.JobPostingId, row.JobTitle, row.JobCompany, row.CompatibilityScore,
            row.CompatibilityScore is { } score ? Band(score) : null, row.ErrorCode, row.CreatedAt)).ToList();

        return new PagedResponse<AnalysisSummary>(items, page, pageSize, total);
    }

    public async Task<Result<AnalysisResponse>> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var analysis = await db.Analyses.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id && candidate.UserId == userId, cancellationToken);
        if (analysis is null)
        {
            return NotFound;
        }

        return ToResponse(analysis);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var analysis = await db.Analyses.SingleOrDefaultAsync(candidate => candidate.Id == id && candidate.UserId == userId, cancellationToken);
        if (analysis is null)
        {
            return false;
        }

        db.Analyses.Remove(analysis);
        audit.Record(AuditActions.AnalysisDeleted, userId, nameof(Analysis), analysis.Id);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public static string Band(int score) => score switch
    {
        >= 70 => "high",
        >= 40 => "medium",
        _ => "low",
    };

    public static ScoreContext ScoreFor(JobAnalysisResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new ScoreContext(
            Band(result.CompatibilityScore),
            result.Strengths.Count(item => item.Importance == RequirementImportance.Required),
            result.Gaps.Count(item => item.Importance == RequirementImportance.Required),
            result.Strengths.Count(item => item.Importance == RequirementImportance.Preferred),
            result.Gaps.Count(item => item.Importance == RequirementImportance.Preferred));
    }

    internal static CandidateProfile ToCandidate(ProfileResponse profile) => new(
        profile.FullName,
        profile.Headline,
        profile.Summary,
        [.. profile.Experiences.Select(item => new CandidateExperience(
            item.Company, item.Role, item.Location, Iso(item.StartDate), Iso(item.EndDate), item.IsCurrent, item.Description,
            item.Achievements, item.Technologies))],
        [.. profile.Education.Select(item => new CandidateEducation(
            item.Institution, item.Degree, item.FieldOfStudy, Iso(item.StartDate), Iso(item.EndDate), EnumText(item.Status)))],
        [.. profile.Projects.Select(item => new CandidateProject(item.Name, item.Description, item.Technologies, item.Url))],
        [.. profile.Certifications.Select(item => new CandidateCertification(item.Name, item.Issuer, Iso(item.IssuedOn)))],
        profile.TechnicalSkills,
        profile.SoftSkills,
        [.. profile.Languages.Select(item => new CandidateLanguage(item.Language, EnumText(item.Proficiency)))]);

    private static JobPostingInput ToPostingInput(JobPostingResponse posting) =>
        new(posting.Title, posting.Company, posting.Location, EnumText(posting.WorkMode), posting.Description);

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private static string? Iso(DateOnly? date) => date is { } value ? Iso(value) : null;

    private static string EnumText<T>(T value)
        where T : struct, Enum => JsonSerializer.Serialize(value, SnapshotJson).Trim('"');

    private void Apply(JobAnalysisOutcome outcome, Analysis analysis)
    {
        analysis.Status = outcome.Succeeded ? AnalysisStatus.Completed : AnalysisStatus.Failed;
        analysis.ResultJson = outcome.Succeeded ? outcome.ResultJson : null;
        analysis.CompatibilityScore = outcome.Succeeded ? outcome.Result?.CompatibilityScore : null;
        analysis.ErrorCode = outcome.ErrorCode;
        analysis.PromptId = outcome.PromptId;
        analysis.PromptVersion = outcome.PromptVersion;
        analysis.Model = outcome.Model;
        analysis.InputTokens = outcome.InputTokens;
        analysis.OutputTokens = outcome.OutputTokens;
        analysis.LatencyMs = (int)Math.Min(int.MaxValue, outcome.Latency.TotalMilliseconds);
        analysis.RepairAttempted = outcome.RepairAttempted;
        analysis.ProviderCalled = outcome.ProviderCalled;
        analysis.CompletedAt = time.GetUtcNowForStorage();
    }

    private static AnalysisResponse ToResponse(Analysis analysis)
    {
        JobAnalysisResult? result = null;
        if (analysis.ResultJson is not null && JobAnalysisJson.TryParse(analysis.ResultJson, out var parsed, out _))
        {
            result = parsed;
        }

        return new AnalysisResponse(
            analysis.Id, analysis.Status, analysis.JobPostingId, analysis.JobTitle, analysis.JobCompany, analysis.Language,
            result, result is null ? null : ScoreFor(result), analysis.ErrorCode, analysis.PromptId, analysis.PromptVersion,
            analysis.Model, analysis.RepairAttempted, analysis.CreatedAt, analysis.CompletedAt);
    }

    private static AppError ErrorFor(string? errorCode) => errorCode switch
    {
        AnalysisErrorCodes.NotConfigured => new AppError(errorCode, StatusCodes.Status503ServiceUnavailable,
            "El análisis con IA no está configurado en este servidor. Falta la API key de Anthropic."),
        AnalysisErrorCodes.RateLimited => new AppError(errorCode, StatusCodes.Status503ServiceUnavailable,
            "El servicio de IA está saturado en este momento. Intenta de nuevo en unos minutos."),
        AnalysisErrorCodes.InvalidOutput => new AppError(errorCode, StatusCodes.Status502BadGateway,
            "No pudimos generar un análisis válido y no guardamos un resultado incompleto. Intenta de nuevo."),
        AnalysisErrorCodes.Refused => new AppError(errorCode, StatusCodes.Status422UnprocessableEntity,
            "La IA no pudo analizar este contenido. Revisa el texto de la vacante."),
        AnalysisErrorCodes.RequestRejected => new AppError(errorCode, StatusCodes.Status422UnprocessableEntity,
            "El servicio de IA rechazó la solicitud. Revisa que la vacante y el perfil no sean demasiado largos."),
        _ => new AppError(AnalysisErrorCodes.Unavailable, StatusCodes.Status503ServiceUnavailable,
            "El servicio de IA no está disponible en este momento. Intenta de nuevo más tarde."),
    };

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Analysis {AnalysisId} finished: {Status} {ErrorCode} model={Model} prompt={PromptVersion} tokens={InputTokens}/{OutputTokens} latencyMs={LatencyMs} repaired={RepairAttempted}")]
    private static partial void LogOutcome(ILogger logger, Guid analysisId, AnalysisStatus status, string? errorCode, string model,
        string promptVersion, int inputTokens, int outputTokens, int latencyMs, bool repairAttempted);
}
