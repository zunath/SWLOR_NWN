using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The PvP combat instance: who counts as a player, the combat clock and its window, the healing and damage ramps,
/// the status icon and messages the update keeps current, and the three handlers. The engine and the NWNX events
/// plugin are recording fakes; the clock, the schedule, the recorder and the status effects are answered by a subclass.
/// </summary>
[TestFixture]
public class PvpCombatServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint PlayerOne = 1001;
    private const uint PlayerTwo = 1002;
    private const uint Pet = 3001;
    private const uint Npc = 4001;
    private const uint Dm = 5001;

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private FakeEventsPlugin _events = null!;
    private TestPvp _pvp = null!;
    private readonly Dictionary<string, string> _eventData = new();
    private readonly HashSet<uint> _invalid = new();

    [SetUp]
    public void SetUp()
    {
        _eventData.Clear();
        _invalid.Clear();
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid && !_invalid.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! is PlayerOne or PlayerTwo or Dm);
        _engine.SetReturnHandler("GetIsDM", args => (uint)args[0]! == Dm);
        _engine.SetReturn("GetIsDMPossessed", false);
        _engine.SetReturnHandler("GetMaster", args => (uint)args[0]! == Pet ? PlayerOne : ObjectInvalid);
        _engine.SetReturnHandler("StringToObject", args => uint.Parse((string)args[0]!));
        _engine.SetReturn("OBJECT_SELF", PlayerOne);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _events = new FakeEventsPlugin();
        _events.SetReturnHandler("GetEventData", args => _eventData.TryGetValue((string)args[0]!, out var value) ? value : string.Empty);
        _scopes.Add(EventsPluginRuntime.Override(_events));

        _pvp = new TestPvp();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    [Test]
    public void IsPlayer_NeedsAValidPlayerCharacterThatIsNotADM()
    {
        _pvp.IsPlayer(PlayerOne).Should().BeTrue();
        _pvp.IsPlayer(Npc).Should().BeFalse();
        _pvp.IsPlayer(Dm).Should().BeFalse();
        _pvp.IsPlayer(ObjectInvalid).Should().BeFalse();

        _engine.SetReturn("GetIsDMPossessed", true);
        _pvp.IsPlayer(PlayerOne).Should().BeFalse();
    }

    [Test]
    public void GetControllingPlayer_IsThePlayerOrAPlayersMasterOtherwiseInvalid()
    {
        _pvp.GetControllingPlayer(PlayerOne).Should().Be(PlayerOne);
        _pvp.GetControllingPlayer(Pet).Should().Be(PlayerOne);
        _pvp.GetControllingPlayer(Npc).Should().Be(ObjectInvalid);
        _pvp.GetControllingPlayer(ObjectInvalid).Should().Be(ObjectInvalid);
        _pvp.IsPlayerControlled(Pet).Should().BeTrue();
        _pvp.IsPlayerControlled(Npc).Should().BeFalse();
    }

    [Test]
    public void IsPlayerAgainstPlayer_NeedsAPlayerTargetAndADifferentControllingPlayer()
    {
        _pvp.IsPlayerAgainstPlayer(PlayerTwo, PlayerOne).Should().BeTrue();
        _pvp.IsPlayerAgainstPlayer(Pet, PlayerTwo).Should().BeTrue();
        _pvp.IsPlayerAgainstPlayer(PlayerOne, PlayerOne).Should().BeFalse();
        _pvp.IsPlayerAgainstPlayer(Pet, PlayerOne).Should().BeFalse();
        _pvp.IsPlayerAgainstPlayer(Npc, PlayerOne).Should().BeFalse();
        _pvp.IsPlayerAgainstPlayer(PlayerOne, Npc).Should().BeFalse();
    }

    [Test]
    public void RecordHit_StartsCombatForBothPlayersAndSchedulesOneUpdate()
    {
        _pvp.RecordHit(PlayerOne, PlayerTwo);
        _pvp.RecordHit(PlayerTwo, PlayerOne);

        _pvp.IsInPvpCombat(PlayerOne).Should().BeTrue();
        _pvp.IsInPvpCombat(PlayerTwo).Should().BeTrue();
        _pvp.Scheduled.Should().ContainSingle().Which.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Test]
    public void RecordHit_CountsAPetAndIgnoresNpcsAndSelfHits()
    {
        _pvp.RecordHit(Pet, PlayerTwo);
        _pvp.IsInPvpCombat(PlayerOne).Should().BeTrue();
        _pvp.IsInPvpCombat(PlayerTwo).Should().BeTrue();

        var other = new TestPvp();
        other.RecordHit(Npc, PlayerOne);
        other.RecordHit(PlayerOne, Npc);
        other.RecordHit(PlayerOne, PlayerOne);
        other.RecordHit(Pet, PlayerOne);
        other.IsInPvpCombat(PlayerOne).Should().BeFalse();
        other.Scheduled.Should().BeEmpty();
    }

    [Test]
    public void TheCombatWindowLapsesAfterThirtySecondsWithoutAHit()
    {
        _pvp.RecordHit(PlayerOne, PlayerTwo);

        _pvp.Now += TimeSpan.FromSeconds(30);
        _pvp.IsInPvpCombat(PlayerOne).Should().BeTrue();
        _pvp.GetCombatSeconds(PlayerOne).Should().Be(30);

        _pvp.Now += TimeSpan.FromSeconds(0.5);
        _pvp.IsInPvpCombat(PlayerOne).Should().BeFalse();
        _pvp.GetCombatSeconds(PlayerOne).Should().Be(0);
    }

    [Test]
    public void ANewHitInsideTheWindowKeepsTheClockAndALapsedOneStartsOver()
    {
        _pvp.RecordHit(PlayerOne, PlayerTwo);
        _pvp.Now += TimeSpan.FromSeconds(20);
        _pvp.RecordHit(PlayerOne, PlayerTwo);
        _pvp.Now += TimeSpan.FromSeconds(20);

        _pvp.GetCombatSeconds(PlayerOne).Should().Be(40);

        _pvp.Now += TimeSpan.FromSeconds(31);
        _pvp.RecordHit(PlayerOne, PlayerTwo);
        _pvp.GetCombatSeconds(PlayerOne).Should().Be(0);
        _pvp.Scheduled.Should().HaveCount(1);
    }

    [TestCase(0, 0)]
    [TestCase(119, 0)]
    [TestCase(120, 10)]
    [TestCase(150, 20)]
    [TestCase(180, 30)]
    [TestCase(210, 45)]
    [TestCase(240, 60)]
    [TestCase(900, 60)]
    public void GetHealingReductionPercent_FollowsTheRampSteps(int seconds, int percent)
    {
        _pvp.SetCombatSecondsForTesting(PlayerOne, seconds);

        _pvp.GetHealingReductionPercent(PlayerOne).Should().Be(percent);
    }

    [TestCase(0, 0)]
    [TestCase(179, 0)]
    [TestCase(180, 20)]
    [TestCase(210, 40)]
    [TestCase(240, 60)]
    [TestCase(270, 80)]
    [TestCase(300, 100)]
    [TestCase(900, 100)]
    public void GetDamageIncreasePercent_FollowsTheRampSteps(int seconds, int percent)
    {
        _pvp.SetCombatSecondsForTesting(PlayerOne, seconds);

        _pvp.GetDamageIncreasePercent(PlayerOne).Should().Be(percent);
    }

    [Test]
    public void ThePercentsAreZeroOutsideCombat()
    {
        _pvp.GetHealingReductionPercent(PlayerOne).Should().Be(0);
        _pvp.GetDamageIncreasePercent(PlayerOne).Should().Be(0);
    }

    [Test]
    public void ApplyDamageIncrease_RaisesDamageOnlyAgainstAPlayerInALongFight()
    {
        _pvp.SetCombatSecondsForTesting(PlayerOne, 300);

        _pvp.ApplyDamageIncrease(PlayerOne, PlayerTwo, 10, isLandedAttack: false).Should().Be(20);
        _pvp.ApplyDamageIncrease(PlayerOne, Pet, 10, isLandedAttack: false).Should().Be(10);
        _pvp.ApplyDamageIncrease(PlayerOne, Npc, 10, isLandedAttack: false).Should().Be(10);
        _pvp.ApplyDamageIncrease(Npc, PlayerTwo, 10, isLandedAttack: false).Should().Be(10);
        _pvp.ApplyDamageIncrease(PlayerOne, PlayerTwo, 0, isLandedAttack: true).Should().Be(0);
        _pvp.ApplyDamageIncrease(PlayerOne, PlayerTwo, -5, isLandedAttack: true).Should().Be(-5);
    }

    [Test]
    public void ApplyDamageIncrease_ALandedAttackKeepsBothSidesInCombat()
    {
        _pvp.ApplyDamageIncrease(PlayerOne, PlayerTwo, 10, isLandedAttack: true).Should().Be(10);

        _pvp.IsInPvpCombat(PlayerOne).Should().BeTrue();
        _pvp.IsInPvpCombat(PlayerTwo).Should().BeTrue();
    }

    [Test]
    public void ApplyDamageIncrease_AnAttackThatDidNotLandStartsNoCombat()
    {
        _pvp.ApplyDamageIncrease(PlayerOne, PlayerTwo, 10, isLandedAttack: false);

        _pvp.IsInPvpCombat(PlayerOne).Should().BeFalse();
    }

    [Test]
    public void ReduceHealing_SkipsTheHealAndSetsTheReducedAmount()
    {
        _pvp.SetCombatSecondsForTesting(PlayerOne, 240);
        _eventData["TARGET_OBJECT_ID"] = PlayerOne.ToString();
        _eventData["HEAL_AMOUNT"] = "100";

        _pvp.ReduceHealing();

        _events.CountOf("SkipEvent").Should().Be(1);
        _events.CallsTo("SetEventResult").Single().Arguments[0].Should().Be("40");
        _pvp.Recorded.Should().Equal((PlayerOne, 100, 40));
    }

    [Test]
    public void ReduceHealing_DoesNotRecordWhenTheRecorderIsOff()
    {
        _pvp.Recording = false;
        _pvp.SetCombatSecondsForTesting(PlayerOne, 240);
        _eventData["TARGET_OBJECT_ID"] = PlayerOne.ToString();
        _eventData["HEAL_AMOUNT"] = "100";

        _pvp.ReduceHealing();

        _events.CountOf("SkipEvent").Should().Be(1);
        _pvp.Recorded.Should().BeEmpty();
    }

    [Test]
    public void ReduceHealing_LeavesHealsAlone_WhenThereIsNothingToCut()
    {
        _eventData["TARGET_OBJECT_ID"] = PlayerOne.ToString();
        _eventData["HEAL_AMOUNT"] = "100";
        _pvp.ReduceHealing();

        _pvp.SetCombatSecondsForTesting(PlayerOne, 240);
        _eventData["HEAL_AMOUNT"] = "0";
        _pvp.ReduceHealing();
        _eventData["HEAL_AMOUNT"] = "not a number";
        _pvp.ReduceHealing();
        _eventData["HEAL_AMOUNT"] = "-5";
        _pvp.ReduceHealing();

        _eventData["TARGET_OBJECT_ID"] = Npc.ToString();
        _eventData["HEAL_AMOUNT"] = "100";
        _pvp.ReduceHealing();

        _events.CountOf("SkipEvent").Should().Be(0);
    }

    [Test]
    public void ReduceHealing_IgnoresHealsWhenTheCasterIsADM()
    {
        _pvp.SetCombatSecondsForTesting(PlayerOne, 240);
        _eventData["TARGET_OBJECT_ID"] = PlayerOne.ToString();
        _eventData["HEAL_AMOUNT"] = "100";
        _engine.SetReturn("OBJECT_SELF", Dm);

        _pvp.ReduceHealing();

        _events.CountOf("SkipEvent").Should().Be(0);
    }

    [Test]
    public void EndOnDeath_EndsTheDeadPlayersCombatQuietly()
    {
        _pvp.SetCombatSecondsForTesting(PlayerOne, 200);
        _pvp.Calls.Clear();
        _engine.ClearCalls();
        _engine.SetReturn("GetLastPlayerDied", PlayerOne);

        _pvp.EndOnDeath();

        _pvp.IsInPvpCombat(PlayerOne).Should().BeFalse();
        _pvp.Calls.Should().Contain(call => call.StartsWith("Remove:1001:" + nameof(PvpCombatStatusEffect)));
        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void ForgetExitingPlayer_DropsTheStateWithoutTouchingStatusEffects()
    {
        _pvp.SetCombatSecondsForTesting(PlayerOne, 200);
        _pvp.Calls.Clear();
        _engine.SetReturn("GetExitingObject", PlayerOne);

        _pvp.ForgetExitingPlayer();

        _pvp.IsInPvpCombat(PlayerOne).Should().BeFalse();
        _pvp.Calls.Should().BeEmpty();
    }

    [Test]
    public void EndForTesting_RemovesEveryIconAndStopsTheCombat()
    {
        _pvp.SetCombatSecondsForTesting(PlayerOne, 200);
        _pvp.Calls.Clear();

        _pvp.EndForTesting(PlayerOne);
        _pvp.EndForTesting(PlayerOne);

        _pvp.IsInPvpCombat(PlayerOne).Should().BeFalse();
        _pvp.Calls.Where(call => call.StartsWith("Remove:1001:")).Should().HaveCount(6);
    }

    [Test]
    public void EndForTesting_DoesNothingForAnInvalidPlayer()
    {
        _pvp.SetCombatSecondsForTesting(PlayerOne, 200);
        _pvp.Calls.Clear();
        _invalid.Add(PlayerOne);

        _pvp.EndForTesting(PlayerOne);

        _pvp.Calls.Should().BeEmpty();
    }

    [Test]
    public void TheUpdateShowsTheCountdownIconAndExplainsTheRulesOnce()
    {
        _pvp.RecordHit(PlayerOne, PlayerTwo);
        _pvp.RunUpdate();

        _pvp.Applied.Should().Contain((PlayerOne, nameof(PvpCombatStatusEffect), 121f));
        var messages = Messages(PlayerOne);
        messages.Should().ContainSingle().Which.Should().Contain("You are in PvP combat.");

        _engine.ClearCalls();
        _pvp.Now += TimeSpan.FromSeconds(1);
        _pvp.RunUpdate();
        Messages(PlayerOne).Should().BeEmpty();
    }

    [Test]
    public void TheUpdateMovesThroughTheHealingStepsAnnouncingEach()
    {
        _pvp.RecordHit(PlayerOne, PlayerTwo);
        _pvp.RunUpdate();
        _engine.ClearCalls();

        _pvp.Now += TimeSpan.FromSeconds(25);
        _pvp.RecordHit(PlayerOne, PlayerTwo);
        _pvp.Now += TimeSpan.FromSeconds(25);
        _pvp.RecordHit(PlayerOne, PlayerTwo);
        _pvp.Now += TimeSpan.FromSeconds(25);
        _pvp.RecordHit(PlayerOne, PlayerTwo);
        _pvp.Now += TimeSpan.FromSeconds(25);
        _pvp.RecordHit(PlayerOne, PlayerTwo);
        _pvp.Now += TimeSpan.FromSeconds(25);
        _pvp.RecordHit(PlayerOne, PlayerTwo);
        _pvp.RunUpdate();

        Messages(PlayerOne).Should().ContainSingle().Which.Should().Contain("PvP combat 2:00").And.Contain("reduced by 10%");
        _pvp.Applied.Last(entry => entry.Player == PlayerOne).Should().Be((PlayerOne, nameof(PvpHealingReduction1StatusEffect), 26f));
    }

    [Test]
    public void TheUpdateGivesTheLastStepAnEndlessIconAndAnnouncesDamageSteps()
    {
        _pvp.SetCombatSecondsForTesting(PlayerOne, 300);

        var messages = Messages(PlayerOne);
        messages.Should().HaveCount(2);
        messages[0].Should().Contain("PvP combat 4:00").And.Contain("reduced by 60%");
        messages[1].Should().Contain("PvP combat 5:00").And.Contain("increased by 100%");
        _pvp.Applied.Last().Should().Be((PlayerOne, nameof(PvpHealingReduction5StatusEffect), -1f));
        _pvp.Calls.Should().Contain($"Remove:1001:{nameof(PvpCombatStatusEffect)}");
    }

    [Test]
    public void TheUpdateReappliesAMissingIconWithoutRepeatingTheMessage()
    {
        _pvp.SetCombatSecondsForTesting(PlayerOne, 130);
        _engine.ClearCalls();
        var applied = _pvp.Applied.Count;
        _pvp.ActiveEffects.Clear();

        _pvp.RunUpdate();

        _pvp.Applied.Count.Should().Be(applied + 1);
        Messages(PlayerOne).Should().BeEmpty();
    }

    [Test]
    public void TheUpdateEndsALapsedFightWithAMessageAndStopsTheScheduleWhenNobodyIsLeft()
    {
        _pvp.RecordHit(PlayerOne, PlayerTwo);
        _pvp.RunUpdate();
        _engine.ClearCalls();
        _pvp.Now += TimeSpan.FromSeconds(31);

        _pvp.RunUpdate();

        Messages(PlayerOne).Should().ContainSingle().Which.Should().Contain("PvP combat has ended.");
        Messages(PlayerTwo).Should().ContainSingle().Which.Should().Contain("PvP combat has ended.");
        _pvp.IsInPvpCombat(PlayerOne).Should().BeFalse();
        _pvp.ScheduleDisposed.Should().BeTrue();

        _pvp.RecordHit(PlayerOne, PlayerTwo);
        _pvp.Scheduled.Should().HaveCount(2);
    }

    [Test]
    public void TheUpdateDropsAPlayerWhoIsNoLongerValidWithoutMessages()
    {
        _pvp.RecordHit(PlayerOne, PlayerTwo);
        _engine.ClearCalls();
        _invalid.Add(PlayerOne);

        _pvp.RunUpdate();

        _pvp.IsInPvpCombat(PlayerOne).Should().BeFalse();
        Messages(PlayerOne).Should().BeEmpty();
        _pvp.IsInPvpCombat(PlayerTwo).Should().BeTrue();
    }

    [Test]
    public void TheInstancesKeepTheirOwnFights()
    {
        var other = new TestPvp();

        _pvp.RecordHit(PlayerOne, PlayerTwo);

        other.IsInPvpCombat(PlayerOne).Should().BeFalse();
    }

    private List<string> Messages(uint player) =>
        _engine.CallsTo("SendMessageToPC").Where(call => (uint)call.Arguments[0]! == player)
            .Select(call => (string)call.Arguments[1]!).ToList();

    private sealed class TestPvp : PvpCombatService
    {
        public TestPvp()
            : base(FacadeForwarder.LazyFor<IStatusEffectService>(), FacadeForwarder.For<ICombatRecorderService>())
        {
        }

        private Action? _update;

        public DateTime Now { get; set; } = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
        public bool Recording { get; set; } = true;
        public List<TimeSpan> Scheduled { get; } = new();
        public bool ScheduleDisposed { get; private set; }
        public List<string> Calls { get; } = new();
        public List<(uint Target, int Requested, int Reduced)> Recorded { get; } = new();
        public List<(uint Player, string Effect, float Duration)> Applied { get; } = new();
        public HashSet<(uint Player, Type Effect)> ActiveEffects { get; } = new();

        public void RunUpdate() => _update!();

        protected override DateTime UtcNow() => Now;

        protected override bool IsRecording() => Recording;

        protected override void RecordPvpHealingReduced(uint target, int requested, int reduced) =>
            Recorded.Add((target, requested, reduced));

        protected override IDisposable ScheduleRepeating(Action task, TimeSpan schedule)
        {
            _update = task;
            Scheduled.Add(schedule);
            ScheduleDisposed = false;
            return new Handle(() => ScheduleDisposed = true);
        }

        protected override bool HasStatusEffect(uint creature, Type statusEffectClass) =>
            ActiveEffects.Contains((creature, statusEffectClass));

        protected override void RemoveStatusEffect(uint creature, Type statusEffectClass, bool sendsWornOffMessage)
        {
            Calls.Add($"Remove:{creature}:{statusEffectClass.Name}");
            ActiveEffects.Remove((creature, statusEffectClass));
        }

        protected override void ApplyStatusEffect(uint source, uint creature, Type statusEffectClass, float durationSeconds)
        {
            Applied.Add((creature, statusEffectClass.Name, durationSeconds));
            ActiveEffects.Add((creature, statusEffectClass));
        }
    }

    private sealed class Handle : IDisposable
    {
        private readonly Action _onDispose;

        public Handle(Action onDispose) => _onDispose = onDispose;

        public void Dispose() => _onDispose();
    }
}
