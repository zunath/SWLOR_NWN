namespace SWLOR.Game.Server.Tests.Support;

// Part 1, StatusEffectService (stays): the listener notices the tracker sends.
public sealed partial class TestStatusEffectService
{
    public override void ReapplyFoodHP(uint player) => _bench.Log.Add($"ReapplyFoodHP:{player}");

    public override void ApplyCreatureMovementRate(uint creature) => _bench.Log.Add($"ApplyCreatureMovementRate:{creature}");
}
