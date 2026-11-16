using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

// Part 1, StatService (stays): the stat adjustment read and its sources, and the pools' restored effects.
public sealed partial class TestStatService
{
    protected override IReadOnlyList<StatAdjustmentSource> GetPerkStatSources(uint creature, StatType payloadStat) =>
        _bench.PerkSources.GetValueOrDefault(payloadStat, Array.Empty<StatAdjustmentSource>());

    protected override int GetPerkStatBonus(uint creature, StatType stat) => _bench.PerkStats.GetValueOrDefault((creature, stat));

    protected override IReadOnlyList<StatAdjustmentSource> GetTemporaryStatSources(uint creature, StatType payloadStat) =>
        _bench.TemporarySources.GetValueOrDefault(payloadStat, Array.Empty<StatAdjustmentSource>());

    protected override int GetTemporaryStatAdjustment(uint creature, StatType stat) => _bench.TemporaryStats.GetValueOrDefault((creature, stat));

    protected override void ApplyFPRestoredEffects(uint creature) => _bench.Log.Add($"FPRestoredEffects:{creature}");

    protected override void ApplyStaminaRestoredEffects(uint creature) => _bench.Log.Add($"StaminaRestoredEffects:{creature}");
}
