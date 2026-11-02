using PostulaAI.Api.Modules.AuditLogs.Domain;
using PostulaAI.Api.Shared.Persistence;

namespace PostulaAI.Api.Modules.AuditLogs;

/// <summary>Adds audit entries to the current unit of work; they are saved with the caller's SaveChanges.</summary>
public interface IAuditTrail
{
    void Record(string action, Guid? userId, string? entityType = null, Guid? entityId = null);
}

public sealed class AuditTrail(AppDbContext db, TimeProvider time, IHttpContextAccessor httpContextAccessor) : IAuditTrail
{
    public void Record(string action, Guid? userId, string? entityType = null, Guid? entityId = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            CorrelationId = httpContextAccessor.HttpContext?.TraceIdentifier,
            OccurredAt = time.GetUtcNowForStorage(),
        });
    }
}
