using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The seam the force affinity code calls: a creature's stat adjustment.</summary>
public sealed partial class TestPerkService
{
    protected override int GetCreatureStatAdjustment(uint creature, StatType stat) =>
        _bench.StatAdjustments.GetValueOrDefault((creature, stat));
}
