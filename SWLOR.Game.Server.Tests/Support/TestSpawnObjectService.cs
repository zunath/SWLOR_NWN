#nullable enable
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The spawn object factory (part SP-3) with its clock under the test's control: <see cref="Now"/> is the time the
/// instance sees, kept in step with the <see cref="TestSpawnService"/> it belongs to.
/// </summary>
public sealed class TestSpawnObjectService : SpawnObjectService
{
    public TestSpawnObjectService(DateTime now, ISpawnRegistryService registry)
        : base(registry, FacadeForwarder.For<IAIService>(), FacadeForwarder.For<ICreatureStatService>(), FacadeForwarder.For<IWalkmeshService>())
    {
        Now = now;
    }

    public DateTime Now { get; set; }

    protected override DateTime UtcNow() => Now;

    /// <summary>The instance's own clock, for the test that proves it reads the system clock.</summary>
    public DateTime ReadSystemClock() => base.UtcNow();
}
