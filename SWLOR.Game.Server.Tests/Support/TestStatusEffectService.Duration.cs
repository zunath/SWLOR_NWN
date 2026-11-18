using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Support;

// Part 4, StatusEffectDurationService: the stat read behind the outgoing duration adjustments.
public sealed partial class TestStatusEffectService
{
    protected override int GetCreatureStatAdjustment(uint creature, StatType stat) =>
        _bench.Stats.GetValueOrDefault((creature, stat));
}
