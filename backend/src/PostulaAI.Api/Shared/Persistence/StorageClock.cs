namespace PostulaAI.Api.Shared.Persistence;

public static class StorageClock
{
    /// <summary>
    /// Current UTC time truncated to PostgreSQL's timestamp precision (1 µs). Using it for persisted values keeps
    /// the timestamps returned right after a write identical to those read back later.
    /// </summary>
    public static DateTimeOffset GetUtcNowForStorage(this TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        var now = time.GetUtcNow();
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }
}
