#nullable enable

using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The spawn instance with its clock, its definition search and its error log under the test's control:
/// <see cref="Now"/> is the time the instance sees, <see cref="Definitions"/> the spawn list definitions the module
/// load finds, and <see cref="Errors"/> what the instance reported to the error log. The registry's seams are on
/// <see cref="TestSpawnRegistryService"/>; <c>TestSpawnService.Shared.cs</c> holds the runtime's.
/// </summary>
public sealed partial class TestSpawnService : SpawnService
{
    public TestSpawnService(DateTime now)
        : this(now, new TestSpawnRegistryService(new List<string>()))
    {
    }

    private TestSpawnService(DateTime now, TestSpawnRegistryService registry)
        : this(now, registry, new TestSpawnObjectService(now, registry))
    {
    }

    private TestSpawnService(DateTime now, TestSpawnRegistryService registry, TestSpawnObjectService objects)
        : base(registry, objects)
    {
        _now = now;
        Registry = registry;
        Objects = objects;
    }

    private DateTime _now;

    /// <summary>The time the runtime and the object factory see.</summary>
    public DateTime Now
    {
        get => _now;
        set
        {
            _now = value;
            Objects.Now = value;
        }
    }

    /// <summary>The spawn list definitions the module load finds (the registry's).</summary>
    public List<Type> Definitions => Registry.Definitions;

    /// <summary>What the registry and the runtime reported to the error log.</summary>
    public List<string> Errors => Registry.Errors;

    // One accessor per part of the Spawn split (Readmes/MaintainabilityRefactorCreaturesSplit.md). Each is the one instance
    // until its part moves; the tests call the accessor of the part that owns the member.

    /// <summary>The queue, respawn and despawn runtime (part SP-1).</summary>
    public TestSpawnService Runtime => this;

    /// <summary>The spawn state, its tables and its reload carry (part SP-2).</summary>
    public TestSpawnRegistryService Registry { get; }

    /// <summary>Spawning an object and the DM spawn edge (part SP-3).</summary>
    public TestSpawnObjectService Objects { get; }
}
