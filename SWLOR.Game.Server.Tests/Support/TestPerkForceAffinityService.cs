using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The force affinity part with its one call into the stat service answered by the bench: a creature's stat adjustment.</summary>
public sealed class TestPerkForceAffinityService : PerkForceAffinityService
{
    private readonly PerkBench _bench;

    public TestPerkForceAffinityService(PerkBench bench, IPerkService perk, IStatService stat)
        : base(perk, stat)
    {
        _bench = bench;
    }

    protected override int GetCreatureStatAdjustment(uint creature, StatType stat) =>
        _bench.StatAdjustments.GetValueOrDefault((creature, stat));
}
