#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The spawn instance with its clock, its definition search and its error log under the test's control:
/// <see cref="Now"/> is the time the instance sees, <see cref="Definitions"/> the spawn list definitions the module
/// load finds, and <see cref="Errors"/> what the instance reported to the error log. The seams are in partial files by the
/// part whose code calls them (<c>TestSpawnService.Registry.cs</c>; <c>.Shared</c> holds the seams several parts call).
/// </summary>
public sealed partial class TestSpawnService : SpawnService
{
    public TestSpawnService(DateTime now)
        : base(FacadeForwarder.For<IAIService>(), FacadeForwarder.For<ICreatureStatService>(), FacadeForwarder.For<IWalkmeshService>())
    {
        Now = now;
    }

    public DateTime Now { get; set; }

    public List<Type> Definitions { get; } = new();

    public List<string> Errors { get; } = new();

    // One accessor per part of the Spawn split (Readmes/MaintainabilityRefactorCreaturesSplit.md). Each is the one instance
    // until its part moves; the tests call the accessor of the part that owns the member.

    /// <summary>The queue, respawn and despawn runtime (part SP-1).</summary>
    public TestSpawnService Runtime => this;

    /// <summary>The spawn state, its tables and its reload carry (part SP-2).</summary>
    public TestSpawnService Registry => this;

    /// <summary>Spawning an object and the DM spawn edge (part SP-3).</summary>
    public TestSpawnService Objects => this;
}
