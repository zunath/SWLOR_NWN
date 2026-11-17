using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The stat instance over a <see cref="StatBench"/>. A container is installed because the support types the instance
/// builds (<c>EquipmentStatTotals</c> asks <c>Item</c> and <c>Resistance</c>) reach facades.
/// </summary>
[NonParallelizable]
public abstract class StatServiceTestBase
{
    protected const uint PlayerCreature = StatBench.PlayerCreature;
    protected const uint Npc = StatBench.Npc;
    protected const uint ObjectInvalid = StatBench.ObjectInvalid;

    private FacadeContainer _facades = null!;
    private IDisposable _installed = null!;

    protected StatBench Bench { get; private set; } = null!;

    // One accessor per part of the stat split, as on the bench (Readmes/MaintainabilityRefactorStatSplit.md): a test calls
    // each member through its part's accessor, so a part that moves changes the bench, not the tests.

    protected TestStatService Service => Bench.Service;

    protected TestStatService CreatureStat => Bench.CreatureStat;

    protected TestStatService Record => Bench.Record;

    protected TestCombatRatingService CombatRating => Bench.CombatRating;

    protected TestStatService DeflectionReaction => Bench.DeflectionReaction;

    protected TestStatService Healing => Bench.Healing;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void CreateBench()
    {
        _installed = _facades.Install();
        Bench = new StatBench();
    }

    [TearDown]
    public void DisposeBench()
    {
        Bench.Dispose();
        _installed.Dispose();
    }

    /// <summary>Sets what the status effect stand-in answers for a stat of a creature.</summary>
    protected void SetStat(uint creature, StatType stat, int value) => Bench.StatusStats[(creature, stat)] = value;
}
