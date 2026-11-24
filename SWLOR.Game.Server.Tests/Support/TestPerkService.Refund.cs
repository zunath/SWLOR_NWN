namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The seam the refund code calls: the tracker refresh.</summary>
public sealed partial class TestPerkService
{
    protected override void RefreshStatDrivenTrackerEffects(uint creature) =>
        _bench.Engine.Note($"GameServices.Combat.RefreshStatDrivenTrackerEffects:{creature}:saved={_bench.Db.Saved.Count}");
}
