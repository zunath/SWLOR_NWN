namespace SWLOR.Game.Server.Tests.Support;

// Part 5, StatusEffectNativeService: the hit point limit check and the native effect tag read.
public sealed partial class TestStatusEffectService
{
    protected override void ScheduleTemporaryHitPointLimitCheck(uint target) => _bench.Log.Add($"LimitCheck:{target}");

    protected override bool MayHaveEffectTag(uint creature, string tag) => _bench.NativeTags.Contains(tag);
}
