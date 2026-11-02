using PostulaAI.Api.Shared.Persistence;

namespace PostulaAI.UnitTests.Shared;

public sealed class StorageClockTests
{
    [Fact]
    public void GetUtcNowForStorage_TruncatesToMicroseconds_AsPostgreSqlStoresThem()
    {
        var precise = new DateTimeOffset(2026, 11, 2, 19, 30, 0, TimeSpan.Zero).AddTicks(1_234_567);

        var stored = new FixedTimeProvider(precise).GetUtcNowForStorage();

        Assert.Equal(0, stored.Ticks % TimeSpan.TicksPerMicrosecond);
        Assert.Equal(precise.AddTicks(-7), stored);
    }
}
