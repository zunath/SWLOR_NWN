namespace SWLOR.Game.Server.Tests.Support;

// Part 5, StatMovementService: walk mode and the movement rate.
public sealed partial class TestStatService
{
    protected override bool IsDroid(uint creature) => _bench.Droids.Contains(creature);
}
