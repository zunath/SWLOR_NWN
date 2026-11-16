using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Support;

// Part 1, StatService (stays): the stat adjustment read and its sources.
public sealed partial class TestStatService
{
    protected override IReadOnlyList<StatAdjustmentSource> GetPerkStatSources(uint creature, StatType payloadStat) =>
        _bench.PerkSources.GetValueOrDefault(payloadStat, Array.Empty<StatAdjustmentSource>());

    protected override int GetPerkStatBonus(uint creature, StatType stat) => _bench.PerkStats.GetValueOrDefault((creature, stat));

    protected override IReadOnlyList<StatAdjustmentSource> GetTemporaryStatSources(uint creature, StatType payloadStat) =>
        _bench.TemporarySources.GetValueOrDefault(payloadStat, Array.Empty<StatAdjustmentSource>());

    protected override int GetTemporaryStatAdjustment(uint creature, StatType stat) => _bench.TemporaryStats.GetValueOrDefault((creature, stat));
}
