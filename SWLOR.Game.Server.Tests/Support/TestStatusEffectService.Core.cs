namespace SWLOR.Game.Server.Tests.Support;

// Part 1, StatusEffectService (stays): the listener notices the tracker sends and the hit point limit check.
public sealed partial class TestStatusEffectService
{
    public override void ReapplyFoodHP(uint player) => _bench.Log.Add($"ReapplyFoodHP:{player}");

    public override void ApplyCreatureMovementRate(uint creature) => _bench.Log.Add($"ApplyCreatureMovementRate:{creature}");

    protected override void ScheduleTemporaryHitPointLimitCheck(uint target) => _bench.Log.Add($"LimitCheck:{target}");
}
