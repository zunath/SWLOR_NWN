namespace SWLOR.Game.Server.Tests.Support;

// Part 3, StatPoolService: the FP and Stamina pools.
public sealed partial class TestStatService
{
    protected override void ApplyFPRestoredEffects(uint creature) => _bench.Log.Add($"FPRestoredEffects:{creature}");

    protected override void ApplyStaminaRestoredEffects(uint creature) => _bench.Log.Add($"StaminaRestoredEffects:{creature}");
}
