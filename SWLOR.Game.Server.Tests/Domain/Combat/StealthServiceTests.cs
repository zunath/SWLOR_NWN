using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The stealth instance: the entry gate, the status bookkeeping, granted stealth, the cached opposed detection check and
/// the visibility questions other systems ask. The engine and the events plugin are recording fakes, the unconverted
/// status, stat, perk, enmity and espionage calls are answered by a subclass, and the shared dice are seeded.
/// </summary>
[TestFixture]
public class StealthServiceTests
{
    private const uint Player = 101;
    private const uint Enemy = 202;
    private const uint Other = 303;
    private const uint Area = 500;
    private const uint ObjectInvalid = 0x7F000000;

    private readonly List<IDisposable> _scopes = new();
    private readonly Dictionary<string, int> _ran = new();
    private readonly HashSet<uint> _players = new();
    private readonly HashSet<uint> _stealthed = new();
    private readonly HashSet<uint> _dead = new();
    private readonly HashSet<uint> _dms = new();
    private readonly HashSet<uint> _possessed = new();
    private readonly HashSet<uint> _inCombat = new();
    private FakeNWScript _engine = null!;
    private FakeEventsPlugin _events = null!;
    private LocalVariables _locals = null!;
    private TestStealth _stealth = null!;
    private uint _self;
    private uint _target;
    private bool _hostile;
    private bool _lineOfSight;
    private float _distance;
    private uint _enemyArea;

    [SetUp]
    public void SetUp()
    {
        _ran.Clear();
        _players.Clear();
        _stealthed.Clear();
        _dead.Clear();
        _dms.Clear();
        _possessed.Clear();
        _inCombat.Clear();
        _players.Add(Player);
        _self = Player;
        _target = ObjectInvalid;
        _hostile = true;
        _lineOfSight = true;
        _distance = 20f;
        _enemyArea = Area;

        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _engine.SetReturnHandler("OBJECT_SELF", _ => _self);
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetIsPC", args => _players.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDM", args => _dms.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDMPossessed", args => _possessed.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsInCombat", args => _inCombat.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDead", args => _dead.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetCurrentHitPoints", args => _dead.Contains((uint)args[0]!) ? 0 : 50);
        _engine.SetReturnHandler("GetActionMode", args => _stealthed.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetArea", args => (uint)args[0]! == Enemy ? _enemyArea : Area);
        _engine.SetReturnHandler("GetDistanceBetween", _ => _distance);
        _engine.SetReturnHandler("LineOfSightObject", _ => _lineOfSight);
        _engine.SetReturnHandler("GetIsEnemy", _ => _hostile);
        _engine.SetReturnHandler("StringToObject", _ => _target);
        _engine.SetReturnHandler("GetEnteringObject", _ => Player);
        _engine.SetReturnHandler("GetExitingObject", _ => Player);
        _engine.SetReturnHandler("GetLastPlayerDied", _ => Player);
        _engine.SetReturnHandler("GetLastAttacker", _ => Player);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _events = new FakeEventsPlugin();
        _events.SetReturn("GetEventData", "target");
        _scopes.Add(EventsPluginRuntime.Override(_events));

        _stealth = new TestStealth { Now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc) };
        _stealth.PerkLevel = 1;
        _stealth.Detection = 10;
        _stealth.StealthRating = 10;
        Random.SetSeed(1234);
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    private void RunDeferred(string member)
    {
        _ran.TryGetValue(member, out var done);
        var calls = _engine.CallsTo(member);
        _ran[member] = calls.Count;
        foreach (var call in calls.Skip(done))
            ((Action)call.Arguments[1]!).Invoke();
    }

    private int Messages => _engine.CountOf("SendMessageToPC");

    // ---- gate and entry ----

    [Test]
    public void GateStealthEntry_LeavesNonPlayersAndDmsAlone()
    {
        _players.Clear();
        _stealth.PerkLevel = 0;
        _stealth.GateStealthEntry();
        _players.Add(Player);
        _dms.Add(Player);
        _stealth.GateStealthEntry();

        _events.CountOf("SkipEvent").Should().Be(0);
    }

    [Test]
    public void GateStealthEntry_RejectsAPlayerWithoutThePerk()
    {
        _stealth.PerkLevel = 0;

        _stealth.GateStealthEntry();

        _events.CountOf("SkipEvent").Should().Be(1);
        Messages.Should().Be(1);
    }

    [Test]
    public void GateStealthEntry_AllowsAGrantedPlayerWithoutThePerk()
    {
        _stealth.PerkLevel = 0;
        _engine.SetLocalInt(Player, "STEALTH_GRANTED_WINDOW", 4);

        _stealth.GateStealthEntry();

        _events.CountOf("SkipEvent").Should().Be(0);
    }

    [Test]
    public void GateStealthEntry_RejectsCombatEntryUnlessTheWindowIsOpen()
    {
        _inCombat.Add(Player);
        _stealth.GateStealthEntry();
        _events.CountOf("SkipEvent").Should().Be(1);
        Messages.Should().Be(1);

        _engine.SetLocalInt(Player, StealthService.CombatEntryWindowVariable, 1);
        _stealth.GateStealthEntry();
        _events.CountOf("SkipEvent").Should().Be(1);
    }

    [Test]
    public void GateStealthEntry_AcceptsAnOutOfCombatPlayerWithThePerk()
    {
        _stealth.GateStealthEntry();

        _events.CountOf("SkipEvent").Should().Be(0);
        Messages.Should().Be(0);
    }

    [Test]
    public void OnStealthEntered_IgnoresNonPlayersAndDms()
    {
        _players.Clear();
        _stealth.OnStealthEntered();
        _players.Add(Player);
        _dms.Add(Player);
        _stealth.OnStealthEntered();

        _stealth.Calls.Should().BeEmpty();
    }

    [Test]
    public void OnStealthEntered_AppliesTheStatusEndsRestAndReevaluatesEnemies()
    {
        _stealthed.Add(Player);
        _stealth.RestStatus.Add(Player);
        _stealth.Verdict(Enemy, Player);

        _stealth.OnStealthEntered();
        RunDeferred("DelayCommand");
        RunDeferred("AssignCommand");

        _stealth.StealthStatus.Should().Contain(Player);
        _stealth.RestStatus.Should().BeEmpty();
        _engine.CountOf("ClearAllActions").Should().Be(1);
        _stealth.Calls.Should().ContainInOrder("ApplyStealthStatus", "SilenceLightsaberWhileHidden", "ReevaluateEnemyAttackTargets");
    }

    [Test]
    public void OnStealthEntered_LeavesOtherActionsAloneWhenNotResting()
    {
        _stealthed.Add(Player);

        _stealth.OnStealthEntered();
        RunDeferred("AssignCommand");

        _engine.CountOf("ClearAllActions").Should().Be(0);
        _stealth.StealthStatus.Should().Contain(Player);
    }

    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public void OnStealthEntered_RejectedEntryKeepsRestAndStartsNothing(bool nativeStealth, bool ownsPerk, bool combat)
    {
        if (nativeStealth) _stealthed.Add(Player);
        _stealth.PerkLevel = ownsPerk ? 1 : 0;
        if (combat) _inCombat.Add(Player);
        _stealth.RestStatus.Add(Player);
        _engine.SetLocalInt(Player, "STEALTH_TIMED_EXIT_ACTIVE", 3);

        _stealth.OnStealthEntered();
        RunDeferred("AssignCommand");

        _stealth.RestStatus.Should().Contain(Player);
        _stealth.StealthStatus.Should().BeEmpty();
        _locals.Has(Player, "STEALTH_TIMED_EXIT_ACTIVE").Should().BeFalse();
        _engine.CountOf("ClearAllActions").Should().Be(0);
        if (nativeStealth)
            _engine.CallsTo("SetActionMode").Should().ContainSingle().Which.Arguments[2].Should().Be(false);
    }

    [Test]
    public void OnStealthEntered_AuthorizedCombatEntryAlsoEndsRest()
    {
        _stealthed.Add(Player);
        _inCombat.Add(Player);
        _engine.SetLocalInt(Player, StealthService.CombatEntryWindowVariable, 1);
        _stealth.RestStatus.Add(Player);

        _stealth.OnStealthEntered();
        RunDeferred("AssignCommand");

        _stealth.RestStatus.Should().BeEmpty();
        _stealth.StealthStatus.Should().Contain(Player);
    }

    [Test]
    public void OnStealthEntered_SkipsTheDeferredReevaluationOnceStealthEnded()
    {
        _stealthed.Add(Player);
        _stealth.OnStealthEntered();
        _stealthed.Clear();
        RunDeferred("DelayCommand");

        _stealth.Calls.Should().NotContain("ReevaluateEnemyAttackTargets");
    }

    // ---- refresh and heartbeat ----

    [Test]
    public void RefreshActiveStatusAfterPerkLevelChange_IgnoresNonPlayers()
    {
        _players.Clear();
        _stealth.RefreshActiveStatusAfterPerkLevelChange(Player);
        _stealth.Calls.Should().BeEmpty();
    }

    [Test]
    public void RefreshActiveStatusAfterPerkLevelChange_DropsTheOldSnapshotAndStopsWhenNotHidden()
    {
        _stealth.StealthStatus.Add(Player);

        _stealth.RefreshActiveStatusAfterPerkLevelChange(Player);

        _stealth.StealthStatus.Should().BeEmpty();
        _stealth.Logs.Should().ContainSingle().Which.Should().Contain("removed active status snapshot");
    }

    [Test]
    public void RefreshActiveStatusAfterPerkLevelChange_ReappliesTheStatusWhileHiddenWithThePerk()
    {
        _stealthed.Add(Player);

        _stealth.RefreshActiveStatusAfterPerkLevelChange(Player);

        _stealth.StealthStatus.Should().Contain(Player);
        _stealth.Logs.Should().ContainSingle().Which.Should().Contain("reapplied");
    }

    [Test]
    public void RefreshActiveStatusAfterPerkLevelChange_ForcesAFullyRefundedPlayerOutOfStealth()
    {
        _stealthed.Add(Player);
        _stealth.PerkLevel = 0;
        _stealth.Verdict(Enemy, Player);

        _stealth.RefreshActiveStatusAfterPerkLevelChange(Player);
        RunDeferred("AssignCommand");

        _stealth.Calls.Should().Contain("CancelInfiltration");
        _engine.CallsTo("SetActionMode").Should().ContainSingle();
        _stealth.StealthStatus.Should().BeEmpty();
        _stealth.Logs.Should().ContainSingle().Which.Should().Contain("forced native stealth exit");
    }

    [Test]
    public void EnsureStealthStatusWhileHidden_RebuildsAMissingStatusOnly()
    {
        _stealth.EnsureStealthStatusWhileHidden();
        _stealth.Calls.Should().BeEmpty();

        _stealthed.Add(Player);
        _stealth.StealthStatus.Add(Player);
        _stealth.EnsureStealthStatusWhileHidden();
        _stealth.Calls.Should().NotContain("ApplyStealthStatus");

        _stealth.StealthStatus.Clear();
        _stealth.EnsureStealthStatusWhileHidden();
        _stealth.StealthStatus.Should().Contain(Player);
        _stealth.Logs.Should().Contain(log => log.Contains("status missing"));

        _dms.Add(Player);
        _stealth.Calls.Clear();
        _stealth.StealthStatus.Clear();
        _stealth.EnsureStealthStatusWhileHidden();
        _stealth.Calls.Should().BeEmpty();
    }

    // ---- hostile actions ----

    [Test]
    public void RecordPlayerCombatInitiation_RecordsOnlyPlayerAttackers()
    {
        _stealth.RecordPlayerCombatInitiation();
        _stealth.Infiltration.Should().Equal("start:" + Player);

        _players.Clear();
        _stealth.RecordPlayerCombatInitiation();
        _players.Add(Player);
        _dms.Add(Player);
        _stealth.RecordPlayerCombatInitiation();
        _stealth.Infiltration.Should().HaveCount(1);
    }

    [Test]
    public void BreakStealthOnDamageDealt_RevealsAHiddenPlayerAttacker()
    {
        _stealth.BreakStealthOnDamageDealt();
        Messages.Should().Be(0);

        _stealthed.Add(Player);
        _stealth.BreakStealthOnDamageDealt();
        RunDeferred("AssignCommand");

        Messages.Should().Be(1);
        _engine.CallsTo("SetActionMode").Should().ContainSingle().Which.Arguments[2].Should().Be(false);

        _players.Clear();
        _engine.ClearCalls();
        _stealth.BreakStealthOnDamageDealt();
        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    // ---- granted stealth ----

    [Test]
    public void ScheduleTimedExit_NumbersWindowsAndEndsTheOneItBelongsTo()
    {
        _stealthed.Add(Player);
        var first = _stealth.ScheduleTimedExit(Player, 30f);
        var second = _stealth.ScheduleTimedExit(Player, 10f);

        second.Should().Be(first + 1);
        _locals.Int(Player, "STEALTH_TIMED_EXIT_ACTIVE").Should().Be(second);

        RunDeferred("DelayCommand");
        RunDeferred("AssignCommand");

        _engine.CallsTo("SetActionMode").Should().ContainSingle("only the newest window ends stealth");
        _locals.Has(Player, "STEALTH_TIMED_EXIT_ACTIVE").Should().BeFalse();
    }

    [Test]
    public void ScheduleTimedExit_ClearsAGrantWhoseEntryNeverHappenedAndSkipsInvalidCreatures()
    {
        var generation = _stealth.ScheduleTimedExit(Player, 5f);
        _engine.SetLocalInt(Player, "STEALTH_GRANTED_WINDOW", generation);
        _engine.SetLocalInt(Player, "STEALTH_GRANTED_BONUS", 9);

        RunDeferred("DelayCommand");

        _locals.Has(Player, "STEALTH_GRANTED_WINDOW").Should().BeFalse();
        _locals.Has(Player, "STEALTH_GRANTED_BONUS").Should().BeFalse();
        _engine.CountOf("AssignCommand").Should().Be(0, "the player was not hidden");

        _locals.ClearCalls();
        _ran.Clear();
        _engine.SetReturnHandler("GetIsObjectValid", _ => false);
        var stale = _stealth.ScheduleTimedExit(Player, 5f);
        _engine.SetLocalInt(Player, "STEALTH_GRANTED_WINDOW", stale);
        var validityChecks = _engine.CountOf("GetIsObjectValid");

        RunDeferred("DelayCommand");

        _engine.CountOf("GetIsObjectValid").Should().Be(validityChecks + 1, "the timer callback must actually run");
        _engine.CountOf("DeleteLocalInt").Should().Be(0, "an invalid creature is left alone");
    }

    [Test]
    public void ScheduleTimedExit_LeavesAReplacedWindowAlone()
    {
        _stealthed.Add(Player);
        _stealth.ScheduleTimedExit(Player, 5f);
        _engine.SetLocalInt(Player, "STEALTH_TIMED_EXIT_ACTIVE", 999);

        RunDeferred("DelayCommand");

        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    [TestCase(true, false, false, false, true)]
    [TestCase(false, false, false, false, false)]
    [TestCase(true, true, false, false, false)]
    [TestCase(true, false, true, false, false)]
    [TestCase(true, false, false, true, false)]
    public void CanReceiveGrantedStealth_NeedsALivingHiddenFreePlayerOutOfCombat(
        bool isPlayer, bool isDead, bool isInCombat, bool isInStealth, bool expected)
    {
        _stealth.CanReceiveGrantedStealth(isPlayer, isDead, isInCombat, isInStealth).Should().Be(expected);
    }

    [Test]
    public void TryGrantStealth_StartsAWindowWithTheCasterBonusOnTheMember()
    {
        _stealth.TryGrantStealth(Player, -4, 20f).Should().BeTrue();
        RunDeferred("AssignCommand");

        _locals.Int(Player, "STEALTH_GRANTED_BONUS").Should().Be(0, "a negative bonus is floored");
        _locals.Int(Player, "STEALTH_GRANTED_WINDOW").Should().BeGreaterThan(0);
        _engine.CallsTo("SetActionMode").Should().ContainSingle().Which.Arguments[2].Should().Be(true);
        _stealth.HasGrantedStealth(Player).Should().BeTrue();
    }

    [Test]
    public void TryGrantStealth_RefusesWhenTheMemberCannotBeHidden()
    {
        _inCombat.Add(Player);
        _stealth.TryGrantStealth(Player, 3, 20f).Should().BeFalse();
        _inCombat.Clear();
        _dead.Add(Player);
        _stealth.TryGrantStealth(Player, 3, 20f).Should().BeFalse();
        _dead.Clear();
        _stealthed.Add(Player);
        _stealth.TryGrantStealth(Player, 3, 20f).Should().BeFalse();
        _stealthed.Clear();
        _possessed.Add(Player);
        _stealth.TryGrantStealth(Player, 3, 20f).Should().BeFalse();

        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void GetGrantedStealthBonus_ReadsTheLocal()
    {
        _stealth.HasGrantedStealth(Player).Should().BeFalse();
        _engine.SetLocalInt(Player, "STEALTH_GRANTED_BONUS", 12);
        _stealth.GetGrantedStealthBonus(Player).Should().Be(12);
    }

    [Test]
    public void TheGrantedWindowEndsOnEnterExitAndDeath()
    {
        void Grant()
        {
            _engine.SetLocalInt(Player, "STEALTH_GRANTED_WINDOW", 4);
            _engine.SetLocalInt(Player, "STEALTH_GRANTED_BONUS", 4);
        }

        Grant();
        _stealth.ClearGrantedStealthOnEnter();
        _stealth.HasGrantedStealth(Player).Should().BeFalse();
        Grant();
        _stealth.ClearGrantedStealthOnExit();
        _stealth.HasGrantedStealth(Player).Should().BeFalse();
        Grant();
        _stealth.ClearGrantedStealthOnDeath();
        _stealth.HasGrantedStealth(Player).Should().BeFalse();
        _locals.Has(Player, "STEALTH_GRANTED_BONUS").Should().BeFalse();
    }

    // ---- exit ----

    [Test]
    public void OnStealthExited_CleansUpAndLetsEnemiesResumeAfterAPlayerLeavesStealth()
    {
        _engine.SetLocalInt(Player, "STEALTH_TIMED_EXIT_ACTIVE", 2);
        _stealth.StealthStatus.Add(Player);
        _stealth.Verdict(Enemy, Player);

        _stealth.OnStealthExited();
        RunDeferred("DelayCommand");

        _locals.Has(Player, "STEALTH_TIMED_EXIT_ACTIVE").Should().BeFalse();
        _stealth.StealthStatus.Should().BeEmpty();
        _stealth.Calls.Should().ContainInOrder("CancelInfiltration", "RestoreLightsaberAfterHidden", "ReevaluateEnemyAttackTargets", "AcquireAggroFromNearbyEnemies");
    }

    [Test]
    public void OnStealthExited_DoesNotReevaluateWhenStealthResumed_OrForNonPlayers()
    {
        _stealth.OnStealthExited();
        _stealthed.Add(Player);
        RunDeferred("DelayCommand");
        _stealth.Calls.Should().NotContain("ReevaluateEnemyAttackTargets");
        _stealth.Calls.Should().NotContain("AcquireAggroFromNearbyEnemies");

        _stealth.Calls.Clear();
        _players.Clear();
        _stealth.OnStealthExited();
        _stealth.Calls.Should().NotContain("RestoreLightsaberAfterHidden");
        _engine.CountOf("DelayCommand").Should().Be(1);
    }

    // ---- detection ----

    [Test]
    public void ResolveSpotDetection_AnswersZeroForAnInvalidTarget()
    {
        _self = Enemy;
        _target = ObjectInvalid;

        _stealth.ResolveSpotDetection();

        _events.CountOf("SkipEvent").Should().Be(1);
        _events.CallsTo("SetEventResult").Should().ContainSingle().Which.Arguments[0].Should().Be("0");
    }

    [Test]
    public void ResolveSpotDetection_DetectsAHigherRollAndBreaksAPlayersStealth()
    {
        _self = Enemy;
        _target = Player;
        _stealthed.Add(Player);
        _stealth.Detection = 100;

        _stealth.ResolveSpotDetection();
        RunDeferred("DelayCommand");

        _events.CallsTo("SetEventResult").Should().ContainSingle().Which.Arguments[0].Should().Be("1");
        _stealth.Aggro.Should().Equal((Enemy, Player));
        _stealth.Infiltration.Should().Contain("detect:" + Enemy + ":" + Player + ":True");
        _engine.CallsTo("SetActionMode").Should().HaveCountGreaterThanOrEqualTo(1);
        _engine.CallsTo("SendMessageToPC").Should().ContainSingle();
        _stealth.Logs.Should().Contain(log => log.Contains("forced native stealth exit"));
    }

    [Test]
    public void ResolveSpotDetection_AFailedRollKeepsTheStealth()
    {
        _self = Enemy;
        _target = Player;
        _stealthed.Add(Player);
        _stealth.Detection = -100;

        _stealth.ResolveSpotDetection();

        _events.CallsTo("SetEventResult").Should().ContainSingle().Which.Arguments[0].Should().Be("0");
        _stealth.Aggro.Should().BeEmpty();
        _engine.CountOf("SetActionMode").Should().Be(0);
    }

    [Test]
    public void ResolveDetection_KeepsAFriendlySuccessAsDetectedWithoutBreakingStealth()
    {
        _self = Enemy;
        _target = Player;
        _stealthed.Add(Player);
        _hostile = false;
        _stealth.Detection = 100;

        _stealth.ResolveSpotDetection();

        _events.CallsTo("SetEventResult").Should().ContainSingle().Which.Arguments[0].Should().Be("1");
        _engine.CountOf("SetActionMode").Should().Be(0);
        _stealth.Aggro.Should().BeEmpty();
    }

    [Test]
    public void DetectionOfANonPlayerTarget_DoesNotExitAnyStealth()
    {
        _stealthed.Add(Enemy);
        _stealth.Detection = 100;

        _stealth.IsHiddenByStealth(Other, Enemy).Should().BeFalse("only stealthed players are hidden by the check");
    }

    [Test]
    public void TheVerdictIsRolledOncePerWindowInTheSeededOrder()
    {
        _stealthed.Add(Player);
        _stealth.Detection = 0;
        _stealth.StealthRating = 0;
        var replay = new System.Random(1234);
        var detectionDie = replay.Next(1, 21);
        var stealthDie = replay.Next(1, 21);
        var expectedHidden = !(detectionDie > stealthDie);

        _stealth.IsHiddenByStealth(Enemy, Player).Should().Be(expectedHidden);
        _stealth.IsHiddenByStealth(Enemy, Player).Should().Be(expectedHidden);

        _stealth.StatReads.Should().Be(2, "one detection and one stealth read for the single roll");
        Random.Next(1, 21).Should().Be(replay.Next(1, 21), "the rolls consumed exactly two draws");
    }

    [Test]
    public void TheVerdictIsRolledAgainAfterTheWindowExpires()
    {
        _stealthed.Add(Player);
        _distance = 20f;
        _stealth.IsHiddenByStealth(Enemy, Player);
        _stealth.Now = _stealth.Now.AddSeconds(29);
        _stealth.IsHiddenByStealth(Enemy, Player);
        _stealth.StatReads.Should().Be(2);

        _stealth.Now = _stealth.Now.AddSeconds(2);
        _stealth.IsHiddenByStealth(Enemy, Player);
        _stealth.StatReads.Should().Be(4);
    }

    [Test]
    public void APlayerObserverAtArmsLengthRerollsFasterWithABonus()
    {
        _players.Add(Enemy);
        _stealthed.Add(Player);
        _distance = 3f;
        _stealth.Detection = 0;
        _stealth.StealthRating = 100;

        _stealth.IsHiddenByStealth(Enemy, Player);
        _stealth.Now = _stealth.Now.AddSeconds(7);
        _stealth.IsHiddenByStealth(Enemy, Player);

        _stealth.StatReads.Should().Be(4, "the close-range window is six seconds");
    }

    [Test]
    public void ANaturalTwentyAlwaysDetectsForPlayerObserversOnly()
    {
        _stealth.IsDetectionRollSuccessful(20, 0, 20, 500, true).Should().BeTrue();
        _stealth.IsDetectionRollSuccessful(20, 0, 20, 500, false).Should().BeFalse();
        _stealth.IsDetectionRollSuccessful(10, 50, 10, 50, true).Should().BeFalse("ties go to the hidden creature");
        _stealth.IsDetectionRollSuccessful(11, 50, 10, 50, false).Should().BeTrue();
    }

    [TestCase(1, true, false)]
    [TestCase(20, true, true)]
    [TestCase(20, false, false)]
    public void ACloudedObserverOnlySpotsOnAPlayersNaturalTwenty(int die, bool player, bool expected)
    {
        _stealth.IsCloudedDetectionRollSuccessful(die, player).Should().Be(expected);
    }

    [Test]
    public void ACloudedObserverRollsOneDieAndNeverReadsStats()
    {
        _stealthed.Add(Player);
        _stealth.Blocked = true;
        _stealth.IsStealthDetectionBlocked(Enemy).Should().BeTrue();

        _stealth.IsHiddenByStealth(Enemy, Player).Should().BeTrue();

        _stealth.StatReads.Should().Be(0);
        var replay = new System.Random(1234);
        replay.Next(1, 21);
        Random.Next(1, 21).Should().Be(replay.Next(1, 21), "a clouded check draws a single die");
    }

    [Test]
    public void TheTimingHelpersReturnTheirTables()
    {
        _stealth.GetCloseRangeDetectionBonus(true).Should().Be(10);
        _stealth.GetCloseRangeDetectionBonus(false).Should().Be(0);
        _stealth.GetDetectionCheckIntervalSeconds(true).Should().Be(6f);
        _stealth.GetDetectionCheckIntervalSeconds(false).Should().Be(30f);
        _stealth.ShouldBreakStealthOnDetection(true, true).Should().BeTrue();
        _stealth.ShouldBreakStealthOnDetection(true, false).Should().BeFalse();
        _stealth.ShouldBreakStealthOnDetection(false, true).Should().BeFalse();
    }

    // ---- visibility questions ----

    [Test]
    public void AnObserverThatCannotAttemptASpotCheckCannotFindAHiddenPlayer()
    {
        _stealthed.Add(Player);
        _stealth.SpotDistance = 10f;
        _distance = 20f;
        _stealth.CanAcquireAggro(Enemy, Player).Should().BeFalse();

        _distance = 5f;
        _lineOfSight = false;
        _stealth.IsHiddenByStealth(Enemy, Player).Should().BeTrue();

        _lineOfSight = true;
        _enemyArea = 999;
        _stealth.IsHiddenByStealth(Enemy, Player).Should().BeTrue();
        _stealth.StatReads.Should().Be(0);
    }

    [Test]
    public void APlayerThatIsNotHiddenIsNeverHiddenByStealth()
    {
        _stealth.IsHiddenByStealth(Enemy, Player).Should().BeFalse();
        _stealthed.Add(Player);
        _dms.Add(Player);
        _stealth.IsHiddenByStealth(Enemy, Player).Should().BeFalse();
    }

    [Test]
    public void IsHiddenFrom_CombinesInvisibilityAndStealth()
    {
        _stealth.IsHiddenFrom(Enemy, Player).Should().BeFalse();

        _stealth.MayHaveEffect = true;
        // The target has an Invisibility effect; the observer has one effect that is not See Invisible.
        _engine.SetReturnSequence("GetIsEffectValid", true, true, false);
        _engine.SetReturnSequence("GetEffectType", EffectTypeScript.Invisibility, EffectTypeScript.Slow);
        _stealth.IsHiddenFrom(Enemy, Player).Should().BeTrue();
    }

    [Test]
    public void SeeInvisibleAndTrueSeeingPierceInvisibility()
    {
        _stealth.MayHaveEffect = true;
        _engine.SetReturnSequence("GetIsEffectValid", true, true);
        _engine.SetReturnSequence("GetEffectType", EffectTypeScript.Invisibility, EffectTypeScript.TrueSeeing);

        _stealth.IsHiddenByInvisibility(Enemy, Player).Should().BeFalse();
    }

    [Test]
    public void IsInvisible_NeedsAValidCreatureAndAnEffectOfTheRightType()
    {
        _stealth.IsInvisible(ObjectInvalid).Should().BeFalse();

        _stealth.MayHaveEffect = false;
        _stealth.IsInvisible(Player).Should().BeFalse();

        _stealth.MayHaveEffect = true;
        _engine.SetReturnSequence("GetIsEffectValid", true, false);
        _engine.SetReturnHandler("GetEffectType", _ => EffectTypeScript.Slow);
        _stealth.IsInvisible(Player).Should().BeFalse();

        _engine.SetReturnHandler("GetEffectType", _ => EffectTypeScript.ImprovedInvisibility);
        _engine.SetReturnSequence("GetIsEffectValid", true);
        _stealth.IsInvisible(Player).Should().BeTrue();
    }

    [Test]
    public void IsConcealed_IsTrueForAStealthedValidCreature()
    {
        _stealth.IsConcealed(ObjectInvalid).Should().BeFalse();
        _stealth.IsConcealed(Player).Should().BeFalse();
        _stealthed.Add(Player);
        _stealth.IsConcealed(Player).Should().BeTrue();
    }

    [Test]
    public void SuppressListenDetection_SkipsTheEventAndAnswersZero()
    {
        _stealth.SuppressListenDetection();

        _events.CountOf("SkipEvent").Should().Be(1);
        _events.CallsTo("SetEventResult").Should().ContainSingle().Which.Arguments[0].Should().Be("0");
    }

    // ---- cache upkeep ----

    [Test]
    public void ClearVerdictsForObserver_ForcesAFreshRollForThatObserverOnly()
    {
        _stealthed.Add(Player);
        _stealth.IsHiddenByStealth(Enemy, Player);
        _stealth.IsHiddenByStealth(Other, Player);
        _stealth.StatReads.Should().Be(4);

        _stealth.ClearVerdictsForObserver(Enemy);
        _stealth.IsHiddenByStealth(Enemy, Player);
        _stealth.IsHiddenByStealth(Other, Player);

        _stealth.StatReads.Should().Be(6);
    }

    [Test]
    public void EnteringStealthClearsTheVerdictsAboutTheTarget()
    {
        _stealthed.Add(Player);
        _stealth.IsHiddenByStealth(Enemy, Player);
        _stealth.OnStealthEntered();
        _stealth.IsHiddenByStealth(Enemy, Player);

        _stealth.StatReads.Should().Be(4);
    }

    [Test]
    public void ReevaluateHiddenTargetsForObserver_AttacksTheHighestEnmityOfAnNpcWithATable()
    {
        _stealth.ReevaluateHiddenTargetsForObserver(Player);
        _engine.CountOf("DelayCommand").Should().Be(0);

        _stealth.EnmityCount = 2;
        _stealth.ReevaluateHiddenTargetsForObserver(Enemy);
        RunDeferred("DelayCommand");
        _stealth.Calls.Should().Contain("AttackHighestEnmityTarget");

        _stealth.Calls.Clear();
        _stealth.EnmityCount = 0;
        _stealth.ReevaluateHiddenTargetsForObserver(Enemy);
        RunDeferred("DelayCommand");
        _stealth.Calls.Should().NotContain("AttackHighestEnmityTarget");
    }

    [Test]
    public void TheVerdictCacheIsPrunedOnceItReachesTheThreshold()
    {
        _stealthed.Add(Player);
        for (uint observer = 10_000; observer < 12_001; observer++)
            _stealth.IsHiddenByStealth(observer, Player);
        _stealth.Now = _stealth.Now.AddSeconds(40);
        _stealth.IsHiddenByStealth(99_999, Player);

        var before = _stealth.StatReads;
        _stealth.IsHiddenByStealth(10_000, Player);
        _stealth.StatReads.Should().Be(before + 2, "the expired verdict was pruned and rolled again");
    }

    [Test]
    public void TheDetectionRulesAreEvaluatedWithoutAllocating()
    {
        var hits = 0;
        for (var i = 0; i < 100; i++)
            hits += Rules(i);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
            hits += Rules(i);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        allocated.Should().Be(0);
        hits.Should().BeGreaterThan(0);

        int Rules(int i) =>
            (_stealth.IsDetectionRollSuccessful(i % 20 + 1, 5, 10, 5, true) ? 1 : 0) +
            (_stealth.IsCloudedDetectionRollSuccessful(i % 20 + 1, true) ? 1 : 0) +
            (_stealth.CanReceiveGrantedStealth(true, false, false, i % 2 == 0) ? 1 : 0) +
            _stealth.GetCloseRangeDetectionBonus(i % 2 == 0) + (int)_stealth.GetDetectionCheckIntervalSeconds(i % 2 == 0);
    }

    private sealed class TestStealth : StealthService
    {
        public TestStealth()
            : base(FacadeForwarder.LazyFor<IPerkService>(), FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.LazyFor<IStatusEffectService>(), FacadeForwarder.LazyFor<IEnmityService>(),
                FacadeForwarder.For<IAIService>(), FacadeForwarder.LazyFor<IEspionageInfiltrationService>())
        {
        }

        public DateTime Now { get; set; }
        public int PerkLevel { get; set; }
        public int Detection { get; set; }
        public int StealthRating { get; set; }
        public int StatReads { get; private set; }
        public bool Blocked { get; set; }
        public bool MayHaveEffect { get; set; }
        public float SpotDistance { get; set; } = 30f;
        public int EnmityCount { get; set; }
        public HashSet<uint> StealthStatus { get; } = new();
        public HashSet<uint> RestStatus { get; } = new();
        public List<string> Calls { get; } = new();
        public List<string> Logs { get; } = new();
        public List<string> Infiltration { get; } = new();
        public List<(uint Observer, uint Target)> Aggro { get; } = new();

        public void Verdict(uint observer, uint target) { }

        protected override DateTime UtcNow() => Now;

        protected override void WriteStructuredLog(LogGroup group, string messageTemplate, params object[] propertyValues) =>
            Logs.Add(messageTemplate);

        protected override int GetStealthPerkLevel(uint creature) => PerkLevel;

        protected override int GetDetection(uint creature)
        {
            StatReads++;
            return Detection;
        }

        protected override int GetStealth(uint creature)
        {
            StatReads++;
            return StealthRating;
        }

        protected override int GetStealthDetectionBlockedAdjustment(uint observer) => Blocked ? 1 : 0;

        protected override float GetSpotCheckDistance(uint observer) => SpotDistance;

        protected override void RemoveStealthStatus(uint creature)
        {
            Calls.Add("RemoveStealthStatus");
            StealthStatus.Remove(creature);
        }

        protected override bool HasStealthStatus(uint creature) => StealthStatus.Contains(creature);

        protected override bool ApplyStealthStatus(uint creature)
        {
            Calls.Add("ApplyStealthStatus");
            return StealthStatus.Add(creature);
        }

        protected override bool HasRestStatus(uint creature) => RestStatus.Contains(creature);

        protected override void RemoveRestStatus(uint creature) => RestStatus.Remove(creature);

        protected override void SilenceLightsaberWhileHidden(uint creature) => Calls.Add("SilenceLightsaberWhileHidden");

        protected override void RestoreLightsaberAfterHidden(uint creature) => Calls.Add("RestoreLightsaberAfterHidden");

        protected override void ReevaluateEnemyAttackTargets(uint creature) => Calls.Add("ReevaluateEnemyAttackTargets");

        protected override void AcquireAggroFromNearbyEnemies(uint target) => Calls.Add("AcquireAggroFromNearbyEnemies");

        protected override int GetEnmityTableCount(uint enemy) => EnmityCount;

        protected override void AttackHighestEnmityTarget(uint creature) => Calls.Add("AttackHighestEnmityTarget");

        protected override void CancelInfiltration(uint creature) => Calls.Add("CancelInfiltration");

        protected override void RecordInfiltrationCombatInitiation(uint attacker) => Infiltration.Add("start:" + attacker);

        protected override void RecordInfiltrationDetection(uint observer, uint target, bool detected) =>
            Infiltration.Add($"detect:{observer}:{target}:{detected}");

        protected override void TryAcquireAggroAfterDetection(uint observer, uint target) => Aggro.Add((observer, target));

        protected override bool MayHaveScriptType(uint creature, EffectTypeScript effectType, EffectTypeScript otherEffectType) =>
            MayHaveEffect;
    }
}
