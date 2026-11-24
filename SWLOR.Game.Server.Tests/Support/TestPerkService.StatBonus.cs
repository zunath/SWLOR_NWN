using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The seam the stat bonus code calls: the stat aggregation.</summary>
public sealed partial class TestPerkService
{
    protected override int AggregateStatAdjustment(StatType stat, int current, int adjustment) => current + adjustment;

    public int DefaultAggregateStatAdjustment(StatType stat, int current, int adjustment) =>
        base.AggregateStatAdjustment(stat, current, adjustment);
}
