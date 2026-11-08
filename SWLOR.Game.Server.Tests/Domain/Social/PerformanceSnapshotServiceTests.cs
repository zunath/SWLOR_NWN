using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>
/// Reading a live session into a snapshot. The unconverted Perk and Stat statics and the main Performance facade are
/// answered by a subclass; the engine is a recording fake that knows where the performer stands.
/// </summary>
[TestFixture]
public class PerformanceSnapshotServiceTests
{
    private const uint Performer = 1001;
    private static readonly DateTime Start = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private TestSnapshots _snapshots = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _engine.SetReturn("GetPosition", new Vector3(1f, 2f, 3f));
        _scope = NWScriptRuntime.Override(_engine);
        _snapshots = new TestSnapshots();
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    private static PerformanceSession Session(PerformanceKind kind) =>
        new(Performer, kind, PerformanceInstrumentFamily.Strings, PerformanceTempo.Relaxed, Start, new System.Random(3))
        {
            PlayerId = "uuid-1001",
            Routine = PerformanceRoutineType.LightFeet,
            Set = PerformanceSetType.KeenEar
        };

    [Test]
    public void ADancerSnapshotCarriesTheirStatsAndUnlocks()
    {
        _snapshots.Perks[PerkType.Showstopper] = 1;
        _snapshots.Perks[PerkType.PartnerDance] = 1;
        _snapshots.Perks[PerkType.StandingOvation] = 1;
        _snapshots.Perks[PerkType.Requiem] = 1;
        _snapshots.Stats[StatType.PerformanceDanceXPBonus] = 12;
        _snapshots.Stats[StatType.PerformanceMusicXPBonus] = 99;
        _snapshots.Stats[StatType.PerformanceDanceDebtClearPercent] = 30;
        _snapshots.Stats[StatType.PerformanceBankingBonusPercent] = 20;
        _snapshots.Stats[StatType.PerformanceInspiredCapBonusMinutes] = 5;
        _snapshots.Stats[StatType.PerformanceLightFeetSpeedPercent] = 8;
        _snapshots.Stats[StatType.PerformanceRoutineAttributeBonus] = 2;
        _snapshots.Stats[StatType.PerformanceSteadyHandsBonus] = 7;
        _snapshots.Stats[StatType.PerformanceRangeBonusMeters] = 3;
        _snapshots.UnlockedRoutines.Add(PerformanceRoutineType.LightFeet);
        _snapshots.UnlockedRoutines.Add(PerformanceRoutineType.PowerStep);
        _snapshots.Preference.SecondRoutine = PerformanceRoutineType.PowerStep;
        var session = Session(PerformanceKind.Dance);

        var snapshot = _snapshots.Build(session, 4, 77);

        snapshot.Performer.Should().Be(Performer);
        snapshot.Kind.Should().Be(PerformanceKind.Dance);
        snapshot.StartedUtc.Should().Be(Start);
        snapshot.IsRoleplay.Should().Be(session.IsRoleplay);
        snapshot.Quality.Should().Be(session.Quality);
        snapshot.Position.Should().Be(new Vector3(1f, 2f, 3f));
        snapshot.RangeMeters.Should().Be(PerformanceAudienceRules.GetRange(3));
        snapshot.Venue.Should().Be(4);
        snapshot.XpBase.Should().Be(12);
        snapshot.XpScaled.Should().Be(12 + 1000, "the test social scaling adds a marker");
        snapshot.DebtClearPercent.Should().Be(30);
        snapshot.BankingBonusPercent.Should().Be(20);
        snapshot.CapBonusMinutes.Should().Be(5);
        snapshot.LightFeetSpeedPercent.Should().Be(8);
        snapshot.RoutineAttributeBonus.Should().Be(2);
        snapshot.SteadyHandsBonus.Should().Be(0, "musician stats do not apply to a dancer");
        snapshot.HasCapstone.Should().BeTrue();
        snapshot.HasPartnerTrait.Should().BeTrue();
        snapshot.HasStandingOvation.Should().BeTrue();
        snapshot.HasRequiem.Should().BeFalse();
        snapshot.Routine.Should().Be(PerformanceRoutineType.LightFeet);
        snapshot.SecondRoutine.Should().Be(PerformanceRoutineType.PowerStep);
        snapshot.Set.Should().Be(PerformanceSetType.CantinaStandards);
        snapshot.SecondSet.Should().Be(PerformanceSetType.CantinaStandards);
        snapshot.SongTrackId.Should().Be(77);
        snapshot.Session.Should().BeSameAs(session);
    }

    [Test]
    public void ARoutineRefundedMidPerformanceFallsBackToTheCantinaShuffle()
    {
        _snapshots.Perks[PerkType.Showstopper] = 1;
        _snapshots.Preference.SecondRoutine = PerformanceRoutineType.PowerStep;

        var snapshot = _snapshots.Build(Session(PerformanceKind.Dance), 0, -1);

        snapshot.Routine.Should().Be(PerformanceRoutineType.CantinaShuffle);
        snapshot.SecondRoutine.Should().Be(PerformanceRoutineType.CantinaShuffle);
    }

    [Test]
    public void AMusicianSnapshotCarriesTheirStatsAndUnlocks()
    {
        _snapshots.Perks[PerkType.Virtuoso] = 1;
        _snapshots.Perks[PerkType.Duet] = 1;
        _snapshots.Perks[PerkType.Requiem] = 1;
        _snapshots.Perks[PerkType.StandingOvation] = 1;
        _snapshots.Stats[StatType.PerformanceMusicXPBonus] = 18;
        _snapshots.Stats[StatType.PerformanceDanceXPBonus] = 99;
        _snapshots.Stats[StatType.PerformanceMusicDebtClearPercent] = 25;
        _snapshots.Stats[StatType.PerformanceHarmonyBankingBonusPercent] = 15;
        _snapshots.Stats[StatType.PerformanceInspiredCapBonusMinutes] = 5;
        _snapshots.Stats[StatType.PerformanceLightFeetSpeedPercent] = 8;
        _snapshots.Stats[StatType.PerformanceSteadyHandsBonus] = 7;
        _snapshots.Stats[StatType.PerformanceAnglersLullabyBonus] = 6;
        _snapshots.Stats[StatType.PerformanceSetAttributeBonus] = 4;
        _snapshots.UnlockedSets.Add(PerformanceSetType.KeenEar);
        _snapshots.UnlockedSets.Add(PerformanceSetType.StillWaters);
        _snapshots.Preference.SecondSet = PerformanceSetType.StillWaters;

        var snapshot = _snapshots.Build(Session(PerformanceKind.Music), 2, 5);

        snapshot.XpBase.Should().Be(18);
        snapshot.DebtClearPercent.Should().Be(25);
        snapshot.BankingBonusPercent.Should().Be(15);
        snapshot.CapBonusMinutes.Should().Be(0);
        snapshot.LightFeetSpeedPercent.Should().Be(0);
        snapshot.SteadyHandsBonus.Should().Be(7);
        snapshot.AnglersLullabyBonus.Should().Be(6);
        snapshot.SetAttributeBonus.Should().Be(4);
        snapshot.HasCapstone.Should().BeTrue();
        snapshot.HasPartnerTrait.Should().BeTrue();
        snapshot.HasStandingOvation.Should().BeFalse("only a dancer has Standing Ovation");
        snapshot.HasRequiem.Should().BeTrue();
        snapshot.Routine.Should().Be(PerformanceRoutineType.CantinaShuffle);
        snapshot.Set.Should().Be(PerformanceSetType.KeenEar);
        snapshot.SecondSet.Should().Be(PerformanceSetType.StillWaters);
    }

    [Test]
    public void ANegativeStatNeverSubtractsAndAMusicianWithoutTheCapstoneHasNoSecondSet()
    {
        _snapshots.Stats[StatType.PerformanceMusicXPBonus] = -4;
        _snapshots.Stats[StatType.PerformanceMusicDebtClearPercent] = -1;
        _snapshots.Preference.SecondSet = PerformanceSetType.StillWaters;
        _snapshots.UnlockedSets.Add(PerformanceSetType.StillWaters);

        var snapshot = _snapshots.Build(Session(PerformanceKind.Music), 0, 0);

        snapshot.XpBase.Should().Be(0);
        snapshot.DebtClearPercent.Should().Be(0);
        snapshot.HasCapstone.Should().BeFalse();
        snapshot.SecondSet.Should().Be(PerformanceSetType.CantinaStandards);
    }

    private sealed class TestSnapshots : PerformanceSnapshotService
    {
        public TestSnapshots()
            : base(FacadeForwarder.LazyFor<IPerformanceService>(), FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IStatService>())
        {
        }

        public Dictionary<PerkType, int> Perks { get; } = new();
        public Dictionary<StatType, int> Stats { get; } = new();
        public HashSet<PerformanceRoutineType> UnlockedRoutines { get; } = new();
        public HashSet<PerformanceSetType> UnlockedSets { get; } = new();
        public PerformancePreference Preference { get; } = new();

        protected override PerformancePreference GetPreference(uint player) => Preference;

        protected override bool IsRoutineUnlocked(uint player, PerformanceRoutineType routine) => UnlockedRoutines.Contains(routine);

        protected override bool IsSetUnlocked(uint player, PerformanceSetType set) => UnlockedSets.Contains(set);

        protected override int GetPerkLevel(uint creature, PerkType perk) => Perks.GetValueOrDefault(perk);

        protected override int GetStatAdjustment(uint creature, StatType stat) => Stats.GetValueOrDefault(stat);

        protected override int ScaleBySocial(uint performer, int baseValue, int maximum) => baseValue + 1000;
    }
}
