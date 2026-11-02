using PostulaAI.AI.Contracts;
using PostulaAI.AI.Services;
using PostulaAI.Api.Modules.AuditLogs;
using PostulaAI.Api.Modules.AuditLogs.Domain;
using PostulaAI.Api.Shared.Errors;
using PostulaAI.Api.Shared.Persistence;

namespace PostulaAI.Api.Modules.ProfessionalProfiles.Import;

/// <summary>Reads a CV file and returns an unsaved profile draft. The file and its text are never stored or logged.</summary>
public sealed partial class CvImportService(
    CvExtractionService extractor,
    AppDbContext db,
    IAuditTrail audit,
    TimeProvider time,
    ILogger<CvImportService> logger)
{
    public async Task<Result<CvImportResponse>> ImportAsync(Guid userId, byte[] content, CancellationToken cancellationToken)
    {
        var text = CvTextExtractor.Extract(content);
        if (!text.IsSuccess)
        {
            return text.Error;
        }

        var outcome = await extractor.ExtractAsync(text.Value, CancellationToken.None);
        LogOutcome(logger, outcome.Succeeded, outcome.ErrorCode, outcome.Model, outcome.PromptVersion,
            outcome.InputTokens, outcome.OutputTokens, (int)outcome.Latency.TotalMilliseconds, outcome.RepairAttempted);

        if (outcome.ProviderCalled)
        {
            audit.Record(AuditActions.CvImported, userId, "ProfessionalProfile", null);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!outcome.Succeeded || outcome.Extraction is null)
        {
            return ErrorFor(outcome.ErrorCode);
        }

        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var (draft, warnings) = CvDraftNormalizer.Normalize(outcome.Extraction, today);
        return new CvImportResponse(draft, [.. outcome.Warnings, .. warnings]);
    }

    private static AppError ErrorFor(string? errorCode) => errorCode switch
    {
        AnalysisErrorCodes.NotConfigured => new AppError(errorCode, StatusCodes.Status503ServiceUnavailable,
            "La importación con IA no está configurada en este servidor. Falta la API key de Anthropic."),
        AnalysisErrorCodes.RateLimited => new AppError(errorCode, StatusCodes.Status503ServiceUnavailable,
            "El servicio de IA está saturado en este momento. Intenta de nuevo en unos minutos."),
        AnalysisErrorCodes.InvalidOutput => new AppError(errorCode, StatusCodes.Status502BadGateway,
            "No pudimos interpretar tu CV. Intenta de nuevo o completa el perfil manualmente."),
        AnalysisErrorCodes.RequestRejected => new AppError(errorCode, StatusCodes.Status422UnprocessableEntity,
            "El servicio de IA rechazó el contenido del CV."),
        _ => new AppError(AnalysisErrorCodes.Unavailable, StatusCodes.Status503ServiceUnavailable,
            "El servicio de IA no está disponible en este momento. Intenta de nuevo más tarde."),
    };

    [LoggerMessage(Level = LogLevel.Information,
        Message = "CV import finished: succeeded={Succeeded} {ErrorCode} model={Model} prompt={PromptVersion} tokens={InputTokens}/{OutputTokens} latencyMs={LatencyMs} repaired={RepairAttempted}")]
    private static partial void LogOutcome(ILogger logger, bool succeeded, string? errorCode, string model, string promptVersion,
        int inputTokens, int outputTokens, int latencyMs, bool repairAttempted);
}
