using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

// The seams two parts call: the stat trigger and the temporary modifier (part 6, the deflection reactions, and part 7, the
// healing adjustments that join CombatResourcesService). Each part's double copies the ones it calls when it moves.
public sealed partial class TestStatService
{
    protected override bool TryUseStatTrigger(uint creature, StatType statType, int cooldownSeconds)
    {
        _bench.Log.Add($"TryTrigger:{statType}:{cooldownSeconds}");
        return _bench.StatTriggerAvailable;
    }

    protected override void ReplaceTemporaryStatModifier(uint creature, StatType statType, int amount, float durationSeconds, StatType groupStatType) =>
        _bench.Log.Add($"Temporary:{statType}:{amount}:{durationSeconds}:{groupStatType}");
}
