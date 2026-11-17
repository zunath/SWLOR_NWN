namespace SWLOR.Game.Server.Tests.Support;

// Part 2, CreatureStatService: movement, maximum HP and food HP, attributes, NPC loading and regeneration.
public sealed partial class TestStatService
{
    protected override void ScheduleTemporaryHitPointLimitCheck(uint creature) => _bench.Log.Add($"LimitCheck:{creature}");

    protected override bool IsDroid(uint creature) => _bench.Droids.Contains(creature);
}
