#nullable enable
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Tests.Support;

// Seams that more than one part calls: the clock (the queue and despawn code in SP-1, QueueResourceDespawn in SP-3) and the
// error log (LoadSpawnTables, StoreSpawns and AdoptSpawns in SP-2, ProcessQueuedSpawns in SP-1).
public sealed partial class TestSpawnService
{
    protected override DateTime UtcNow() => Now;

    protected override void WriteError(string message) => Errors.Add(message);

    /// <summary>The instance's own clock, for the test that proves it reads the system clock.</summary>
    public DateTime ReadSystemClock() => base.UtcNow();

    /// <summary>The instance's own error sink, for the test that proves it writes to the error log.</summary>
    public void WriteToTheErrorLog(string message) => base.WriteError(message);
}
