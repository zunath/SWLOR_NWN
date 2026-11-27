using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The stat service the bench hands the ability impact state store for the stat sources of an impact
/// (<c>IAbilityImpactState.GetAbilityImpactStatSources</c>): it answers <see cref="AbilityBench.StatSources"/> and counts the
/// reads without allocating, as the ability instance's seam did before the store moved to Combat. It re-implements only that
/// member of <see cref="IStatService"/>; nothing else of the service is called, so its constructor takes no neighbours.
/// </summary>
public sealed class AbilityBenchStat : StatService, IStatService
{
    private readonly AbilityBench _bench;

    public AbilityBenchStat(AbilityBench bench)
        : base(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!)
    {
        _bench = bench;
    }

    IReadOnlyList<StatAdjustmentSource> IStatService.GetStatSources(uint creature, StatType payloadStat)
    {
        _bench.StatSourceReads++;
        return _bench.StatSources;
    }
}
