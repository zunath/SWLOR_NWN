using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

// Part 4, StatRecordService: maximum HP, the gear-fed player records, attributes and NPC loading.
public sealed partial class TestStatService
{
    protected override void ScheduleTemporaryHitPointLimitCheck(uint creature) => _bench.Log.Add($"LimitCheck:{creature}");

    protected override IReadOnlyList<CombatDamageType> GetDefenseDamageTypes() => new[] { CombatDamageType.Physical, CombatDamageType.Force };
}
