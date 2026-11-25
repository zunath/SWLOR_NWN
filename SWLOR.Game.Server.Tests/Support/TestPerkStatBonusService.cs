using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The stat bonus part with its calls into unconverted code answered by the bench: the stat aggregation and the droid test. It reads
/// the catalog through the stay-part double, so the real boot handler fills its tables from the bench's definitions.
/// </summary>
public sealed class TestPerkStatBonusService : PerkStatBonusService
{
    private readonly PerkBench _bench;

    public TestPerkStatBonusService(PerkBench bench, IPerkService perk, ICompanionLookup companions)
        : base(perk, companions)
    {
        _bench = bench;
    }

    protected override int AggregateStatAdjustment(StatType stat, int current, int adjustment) => current + adjustment;

    protected override bool IsDroid(uint creature) => _bench.Droids.Contains(creature);

    /// <summary>The seams' default bodies, for the test that proves each one is the original call.</summary>
    public int DefaultAggregateStatAdjustment(StatType stat, int current, int adjustment) =>
        base.AggregateStatAdjustment(stat, current, adjustment);

    public bool DefaultIsDroid(uint creature) => base.IsDroid(creature);
}
