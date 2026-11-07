using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>
/// The performance instance: starting and stopping a dance or a musical performance, the hot bar, the beat loop, the end
/// triggers, the preferences and the handlers. The engine and the NWNX creature, player and chat plugins are recording
/// fakes, the database an in-memory one, the audience, windows and animation services scripted stand-ins, and the
/// unconverted statics (Scheduler, Perk, Activity, Space) are answered by a subclass that also owns the clock.
/// </summary>
[TestFixture]
public class PerformanceServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 1001;
    private const uint Area = 500;
    private const uint Module = 9;
    private const uint Guitar = 7001;

    private static readonly DateTime T0 = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);

    private FakeNWScript _engine = null!;
    private FakeCreaturePlugin _creature = null!;
    private FakePlayerPlugin _playerPlugin = null!;
    private FakeChatPlugin _chat = null!;
    private readonly List<IDisposable> _scopes = new();
    private InMemoryDBService _db = null!;
    private ScriptedProxy _animationScript = null!;
    private ScriptedProxy _audienceScript = null!;
    private ScriptedProxy _windowsScript = null!;
    private TestPerformance _performance = null!;
    private readonly List<(string Member, object?[] Arguments)> _audienceCalls = new();
    private readonly List<string> _windowCalls = new();
    private readonly List<string> _animationCalls = new();
    private readonly Dictionary<InventorySlot, uint> _slots = new();
    private readonly Dictionary<uint, string> _resrefs = new();
    private readonly Dictionary<(uint, string), string> _strings = new();
    private readonly Dictionary<(uint, string), int> _ints = new();
    private bool _isPc = true;
    private bool _isDead;
    private bool _inCombat;
    private int _hitPoints = 100;
    private uint _area = Area;
    private Vector3 _position = Vector3.Zero;
    private bool _windowOpens = true;
    private bool _audienceThrows;
    private bool _playerValid = true;

    [SetUp]
    public void SetUp()
    {
        _slots.Clear();
        _resrefs.Clear();
        _strings.Clear();
        _ints.Clear();
        _audienceCalls.Clear();
        _windowCalls.Clear();
        _animationCalls.Clear();
        _isPc = true;
        _isDead = false;
        _inCombat = false;
        _hitPoints = 100;
        _area = Area;
        _position = Vector3.Zero;
        _windowOpens = true;
        _audienceThrows = false;
        _playerValid = true;
        _closeThrows = false;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsPC", _ => _isPc);
        _engine.SetReturnHandler("GetIsDM", _ => false);
        _engine.SetReturnHandler("GetIsDMPossessed", _ => false);
        _engine.SetReturnHandler("GetIsDead", _ => _isDead);
        _engine.SetReturnHandler("GetCurrentHitPoints", _ => _hitPoints);
        _engine.SetReturnHandler("GetIsInCombat", _ => _inCombat);
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid && ((uint)arguments[0]! != Player || _playerValid));
        _engine.SetReturnHandler("GetObjectUUID", arguments => $"uuid-{(uint)arguments[0]!}");
        _engine.SetReturnHandler("GetArea", _ => _area);
        _engine.SetReturnHandler("GetPosition", _ => _position);
        _engine.SetReturnHandler("GetModule", _ => Module);
        _engine.SetReturnHandler("GetItemInSlot", arguments => _slots.GetValueOrDefault((InventorySlot)arguments[0]!, ObjectInvalid));
        _engine.SetReturnHandler("GetResRef", arguments => _resrefs.GetValueOrDefault((uint)arguments[0]!, string.Empty));
        _engine.SetReturnHandler("GetLocalString", arguments => _strings.GetValueOrDefault(((uint)arguments[0]!, (string)arguments[1]!), string.Empty));
        _engine.SetReturnHandler("GetLocalInt", arguments => _ints.GetValueOrDefault(((uint)arguments[0]!, (string)arguments[1]!)));
        _engine.SetReturn("GetCurrentAction", ActionType.Invalid);
        _engine.SetReturn("GetEnteringObject", Player);
        _engine.SetReturn("GetExitingObject", Player);
        _engine.SetReturn("GetLastPlayerDied", Player);
        _engine.SetReturn("OBJECT_SELF", Player);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _creature = new FakeCreaturePlugin();
        _creature.SetReturn("SerializeQuickbar", "saved-bar");
        _creature.SetReturn("DeserializeQuickbar", true);
        _scopes.Add(CreaturePluginRuntime.Override(_creature));
        _playerPlugin = new FakePlayerPlugin();
        _scopes.Add(PlayerPluginRuntime.Override(_playerPlugin));
        _chat = new FakeChatPlugin();
        _scopes.Add(ChatPluginRuntime.Override(_chat));

        _db = new InMemoryDBService().With(new Player { Id = "uuid-1001" });

        _animation = ScriptedProxy.Create<INamedAnimationService>(out _animationScript);
        _animationScript.On("Play", arguments =>
        {
            _animationCalls.Add($"Play:{arguments[1]}");
            return $"token-{_animationCalls.Count}";
        });
        _animationScript.On("StopIfCurrent", arguments =>
        {
            _animationCalls.Add($"Stop:{arguments[1]}");
            return true;
        });

        _audience = ScriptedProxy.Create<IPerformanceAudienceService>(out _audienceScript);
        foreach (var member in new[] { "OnSessionEnded", "OnPerformanceStarted", "OnFlourish", "SetPerformerSong" })
        {
            var name = member;
            _audienceScript.On(name, arguments =>
            {
                _audienceCalls.Add((name, arguments));
                if (_audienceThrows && name == "OnSessionEnded")
                    throw new InvalidOperationException("audience failure");

                return null;
            });
        }

        _windows = ScriptedProxy.Create<IPerformanceWindowsService>(out _windowsScript);
        foreach (var member in new[] { "ClearFallbackStrip", "ToggleOptions", "RefreshSetup", "ShowFallbackStrip" })
        {
            var name = member;
            _windowsScript.On(name, _ =>
            {
                _windowCalls.Add(name);
                return null;
            });
        }

        _windowsScript.On("CloseCueWindow", _ =>
        {
            _windowCalls.Add("CloseCueWindow");
            if (_closeThrows)
                throw new InvalidOperationException("window failure");

            return null;
        });

        _windowsScript.On("OpenCueWindow", arguments =>
        {
            _windowCalls.Add("OpenCueWindow");
            if (_windowOpens)
                ((PerformanceSession)arguments[0]!).View = _view;
            return _windowOpens;
        });

        _view = new FakeView { IsOpen = true };
        _performance = new TestPerformance(_db, _animation, _audience, _windows, new PerformanceSongService(null!), new PerformanceRiderService()) { Now = T0, Perks = _perks };
        _perks.Clear();
    }

    private FakeView _view = null!;
    private INamedAnimationService _animation = null!;
    private IPerformanceAudienceService _audience = null!;
    private IPerformanceWindowsService _windows = null!;
    private bool _closeThrows;
    private readonly Dictionary<PerkType, int> _perks = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in Enumerable.Reverse(_scopes))
            scope.Dispose();

        _scopes.Clear();
    }

    private PerformanceSession Live => _performance.TryGetSession(Player, out var session) ? session : throw new InvalidOperationException("no live session");

    private void StartDance(int level = 1)
    {
        _performance.ToggleDance(Player, level);
        _performance.TryGetSession(Player, out _).Should().BeTrue("the dance should have started");
    }

    private void StartMusic(int level = 1)
    {
        _slots[InventorySlot.RightHand] = Guitar;
        _resrefs[Guitar] = "guitar";
        _performance.TogglePlayInstrument(Player, level);
        _performance.TryGetSession(Player, out _).Should().BeTrue("the performance should have started");
    }

    private List<Action> HandBacks()
    {
        foreach (var assigned in _engine.CallsTo("AssignCommand").Select(call => (Action)call.Arguments[1]!).ToList())
            assigned();

        return _engine.CallsTo("DelayCommand").Select(call => (Action)call.Arguments[1]!).ToList();
    }

    private IReadOnlyList<string> Messages() =>
        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).ToList();

    private void Beats(int count)
    {
        for (var index = 0; index < count; index++)
        {
            _performance.Now = _performance.Now.AddSeconds(1);
            _performance.Schedules[^1].Action();
        }
    }

    [TestCase(false, false, false, "Only players may perform.")]
    [TestCase(true, true, false, "You cannot perform right now.")]
    [TestCase(true, false, true, "You cannot perform while in combat.")]
    public void APlayerWhoCannotPerformIsTold(bool isPc, bool dead, bool combat, string expected)
    {
        _isPc = isPc;
        _isDead = dead;
        _inCombat = combat;

        _performance.ToggleDance(Player, 1);

        Messages().Should().Equal(expected);
        _performance.TryGetSession(Player, out _).Should().BeFalse();
    }

    [Test]
    public void ABusyPlayerOrOneInSpaceCannotPerform()
    {
        _performance.Busy = true;
        _performance.ToggleDance(Player, 1);
        _performance.Busy = false;
        _performance.InSpace = true;
        _performance.ToggleDance(Player, 1);

        Messages().Should().Equal("You are busy.", "You cannot perform in space.");
    }

    [Test]
    public void AMusicianWithoutAnInstrumentCannotPlay()
    {
        _performance.TogglePlayInstrument(Player, 1);

        Messages().Should().Equal("You must equip an instrument to play music.");
    }

    [Test]
    public void AHurtPlayerAtZeroHitPointsCannotPerform()
    {
        _hitPoints = 0;

        _performance.ToggleDance(Player, 1);

        Messages().Should().Equal("You cannot perform right now.");
    }

    [Test]
    public void StartingADanceSavesTheHotBarSwapsInTheCuesAndOpensTheWindow()
    {
        StartDance(level: 2);

        var player = _db.Get<Player>("uuid-1001");
        player.PerformanceSavedHotBar.Should().Be("saved-bar");
        _creature.CallsTo("AddFeat").Select(call => (FeatType)call.Arguments[1]!).Should().BeEquivalentTo(new[]
        {
            FeatType.PerformanceStepLeft, FeatType.PerformanceStepUp, FeatType.PerformanceStepDown,
            FeatType.PerformanceStepRight, FeatType.PerformanceFlourish, FeatType.PerformanceOptions
        });
        _playerPlugin.CallsTo("SetQuickBarSlot").Should().HaveCount(12 + 4 + 3);
        _performance.BusySet.Should().Be(ActivityStatusType.Performing);
        Live.ToggleFeat.Should().Be(FeatType.Dance2);
        Live.PlayerId.Should().Be("uuid-1001");
        _audienceCalls.Should().Contain(call => call.Member == "OnPerformanceStarted");
        _windowCalls.Should().Contain("OpenCueWindow");
        Messages().Should().ContainSingle().Which.Should().StartWith("You begin to dance.");
        _animationCalls.Should().HaveCount(1, "the opening clip; the loop takes over when it ends");
        _performance.Schedules.Should().ContainSingle();
    }

    [Test]
    public void AStartThatCannotReadThePlayerRecordIsRefused()
    {
        _db = new InMemoryDBService();
        _performance = new TestPerformance(_db, _animation, _audience, _windows, new PerformanceSongService(null!), new PerformanceRiderService()) { Now = T0 };

        _performance.ToggleDance(Player, 1);

        Messages().Should().Equal("You cannot perform right now.");
        _performance.TryGetSession(Player, out _).Should().BeFalse();
    }

    [Test]
    public void WhenTheCueWindowCannotOpenTheStripFallsBackToScreenText()
    {
        _windowOpens = false;

        StartDance();

        _windowCalls.Should().Contain("ShowFallbackStrip");
    }

    [Test]
    public void ToggleStopsARunningPerformanceAndRestoresTheHotBar()
    {
        StartDance();

        _performance.ToggleDance(Player, 1);

        _performance.TryGetSession(Player, out _).Should().BeFalse();
        Messages().Should().Contain("You finish your performance.");
        _creature.CallsTo("DeserializeQuickbar").Should().ContainSingle();
        var stopped = _db.Get<Player>("uuid-1001");
        stopped.PerformanceSavedHotBar.Should().Be("saved-bar", "the recovery copy stays until a login acknowledges the restored file");
        stopped.PerformanceHotBarRestoreSequence.Should().Be(1);
        _performance.PersistCalls.Should().Be(1);
        _performance.BusyCleared.Should().BeTrue();
        _windowCalls.Should().Contain("CloseCueWindow");
        _audienceCalls.Should().Contain(call => call.Member == "OnSessionEnded");
        _view.Ended.Should().BeFalse("the window service ends the view, not the performance service");
    }

    [Test]
    public void AHotBarThatCannotBeRestoredIsCleared()
    {
        StartDance();
        _creature.SetReturn("DeserializeQuickbar", false);
        _playerPlugin.ClearCalls();

        _performance.EndPerformance(Player, PerformanceEndReason.Stopped);

        _playerPlugin.CallsTo("SetQuickBarSlot").Should().HaveCount(12);
    }

    [Test]
    public void APlayerWithNoRecordHasTheirHotBarClearedWhenThePerformanceEnds()
    {
        StartDance();
        _db.Delete<Player>("uuid-1001");
        _playerPlugin.ClearCalls();

        _performance.EndPerformance(Player, PerformanceEndReason.Stopped);

        _playerPlugin.CallsTo("SetQuickBarSlot").Should().HaveCount(12);
    }

    [Test]
    public void EndingWithoutAPerformanceDoesNothing()
    {
        _performance.EndPerformance(Player, PerformanceEndReason.Stopped);

        _audienceCalls.Should().BeEmpty();
    }

    [Test]
    public void ALoggedOutPerformerIsNotMessagedOrGivenAFinalClip()
    {
        StartDance();
        _animationCalls.Clear();
        _windowCalls.Clear();
        Messages();

        _performance.EndPerformance(Player, PerformanceEndReason.LoggedOut);

        _windowCalls.Should().NotContain("ClearFallbackStrip");
        _animationCalls.Should().BeEmpty();
    }

    [Test]
    public void ADeadPerformerGetsNoClipAndNoMessage()
    {
        StartDance();
        _animationCalls.Clear();

        _performance.EndPerformance(Player, PerformanceEndReason.Died);

        _animationCalls.Should().BeEmpty();
        Messages().Should().ContainSingle("only the start message was sent");
    }

    [Test]
    public void AnInvalidPlayerIsNotTouchedWhenThePerformanceEnds()
    {
        StartDance();
        _playerValid = false;
        _creature.ClearCalls();

        _performance.EndPerformance(Player, PerformanceEndReason.Invalid);

        _creature.CallsTo("RemoveFeat").Should().BeEmpty();
        _audienceCalls.Should().Contain(call => call.Member == "OnSessionEnded");
    }

    [Test]
    public void AFailingAudienceCleanupIsLoggedAndTheSessionIsStillGone()
    {
        StartDance();
        _audienceThrows = true;

        var end = () => _performance.EndPerformance(Player, PerformanceEndReason.Stopped);

        end.Should().NotThrow();
        _performance.TryGetSession(Player, out _).Should().BeFalse();
    }

    [Test]
    public void AStoppedPerformerStandingIdleBowsAndAWalkingOneJustReleasesTheClip()
    {
        StartDance();
        _animationCalls.Clear();
        _performance.EndPerformance(Player, PerformanceEndReason.Stopped);
        _animationCalls.Should().ContainSingle().Which.Should().StartWith("Play:");

        StartDance();
        _animationCalls.Clear();
        _engine.SetReturn("GetCurrentAction", ActionType.MoveToPoint);
        _performance.EndPerformance(Player, PerformanceEndReason.Stopped);
        _animationCalls.Should().ContainSingle().Which.Should().StartWith("Stop:");

        StartDance();
        _animationCalls.Clear();
        _performance.EndPerformance(Player, PerformanceEndReason.Combat);
        _animationCalls.Should().ContainSingle().Which.Should().StartWith("Stop:");
    }

    [Test]
    public void AnInstrumentInAHandSetsTheFamily()
    {
        _slots[InventorySlot.RightHand] = Guitar;
        _resrefs[Guitar] = "kloo_horn";

        _performance.HasInstrumentEquipped(Player, out var family).Should().BeTrue();

        family.Should().Be(PerformanceInstrumentFamily.Horns);
    }

    [Test]
    public void AnInstrumentTaggedWithAFamilyVariableCounts()
    {
        _slots[InventorySlot.LeftHand] = 7002;
        _resrefs[7002] = "custom_thing";
        _strings[(7002, "PERFORMANCE_INSTRUMENT_FAMILY")] = "horns";

        _performance.HasInstrumentEquipped(Player, out var family).Should().BeTrue();

        family.Should().Be(PerformanceInstrumentFamily.Horns);
    }

    [Test]
    public void TheNoInstrumentAllowanceLetsAnyoneStartAndPicksTheFamilyFromThePlayerOrTheModule()
    {
        _performance.HasInstrumentEquipped(Player, out var none).Should().BeFalse();
        none.Should().Be(PerformanceInstrumentFamily.None);

        _ints[(Module, "PERFORMANCE_ALLOW_NO_INSTRUMENT")] = 1;
        _performance.HasInstrumentEquipped(Player, out var byDefault).Should().BeTrue();
        byDefault.Should().Be(PerformanceInstrumentFamily.Strings);

        _strings[(Module, "PERFORMANCE_INSTRUMENT_FAMILY")] = "Horns";
        _performance.HasInstrumentEquipped(Player, out var fromModule).Should().BeTrue();
        fromModule.Should().Be(PerformanceInstrumentFamily.Horns);

        _strings[(Player, "PERFORMANCE_INSTRUMENT_FAMILY")] = "strings";
        _performance.HasInstrumentEquipped(Player, out var fromPlayer).Should().BeTrue();
        fromPlayer.Should().Be(PerformanceInstrumentFamily.Strings);

        _ints.Clear();
        _ints[(Player, "PERFORMANCE_ALLOW_NO_INSTRUMENT")] = 1;
        _strings[(Player, "PERFORMANCE_INSTRUMENT_FAMILY")] = "nonsense";
        _strings[(Module, "PERFORMANCE_INSTRUMENT_FAMILY")] = "None";
        _performance.HasInstrumentEquipped(Player, out var fallback).Should().BeTrue();
        fallback.Should().Be(PerformanceInstrumentFamily.Strings);
    }

    [Test]
    public void AMusicianStartsWithTheirInstrumentAndTheMusicToggleFeat()
    {
        StartMusic(level: 3);

        Live.Kind.Should().Be(PerformanceKind.Music);
        Live.ToggleFeat.Should().Be(FeatType.PlayInstrument3);
        Live.Instrument.Should().Be(Guitar);
        Live.InstrumentSlot.Should().Be(InventorySlot.RightHand);
        Live.InstrumentFamily.Should().Be(PerformanceInstrumentFamily.Strings);
        Messages().Should().ContainSingle().Which.Should().StartWith("You begin to play.");
    }

    [Test]
    public void ModeFeatsAreTheCuesFlourishAndOptions_AndAPerformersToggleFeatStopsThem()
    {
        _performance.IsModeFeat(FeatType.PerformanceStepLeft).Should().BeTrue();
        _performance.IsModeFeat(FeatType.PerformanceNoteHigh).Should().BeTrue();
        _performance.IsModeFeat(FeatType.PerformanceOptions).Should().BeTrue();
        _performance.IsModeFeat(FeatType.Dance1).Should().BeFalse();
        _performance.IsModeFeat(Player, FeatType.Dance1).Should().BeFalse("nobody is performing");

        StartDance();

        _performance.IsModeFeat(Player, FeatType.Dance1).Should().BeTrue();
        _performance.IsModeFeat(Player, FeatType.PlayInstrument4).Should().BeTrue();
        _performance.IsModeFeat(Player, FeatType.PerformanceFlourish).Should().BeTrue();
        _performance.IsModeFeat(Player, FeatType.Invalid).Should().BeFalse();
    }

    [Test]
    public void ACuePressScoresOnTheCurrentBeat()
    {
        StartDance();
        var lane = Live.Current.Primary;
        var feat = lane switch
        {
            PerformanceCue.Left => FeatType.PerformanceStepLeft,
            PerformanceCue.Up => FeatType.PerformanceStepUp,
            PerformanceCue.Down => FeatType.PerformanceStepDown,
            _ => FeatType.PerformanceStepRight
        };
        _animationCalls.Clear();

        _performance.HandleInput(Player, feat);

        _animationCalls.Should().NotBeEmpty("a hit plays its accent clip");
    }

    [Test]
    public void ACueOutsideTheKindsLanesIsIgnored()
    {
        StartDance();
        _animationCalls.Clear();

        _performance.HandleInput(Player, FeatType.PerformanceNoteLow);
        _performance.HandleInput(Player, FeatType.Invalid);

        _animationCalls.Should().BeEmpty();
    }

    [Test]
    public void ACuePressOnAMissOrAPartialPlaysNoClip()
    {
        StartMusic();
        var other = Live.Current.Primary == PerformanceCue.Low ? FeatType.PerformanceNoteHigh : FeatType.PerformanceNoteLow;
        _animationCalls.Clear();

        _performance.HandleInput(Player, other);

        _animationCalls.Should().BeEmpty();
    }

    [Test]
    public void TheOptionsButtonOpensThePerformWindowAndAFlourishThatIsNotReadyTellsThePlayer()
    {
        StartDance();

        _performance.HandleInput(Player, FeatType.PerformanceOptions);
        _performance.HandleInput(Player, FeatType.PerformanceFlourish);

        _windowCalls.Should().Contain("ToggleOptions");
        Messages().Should().Contain(message => message.StartsWith("Flourish is not ready"));
    }

    [Test]
    public void AReadyFlourishPlaysTheClipAndTellsTheAudience()
    {
        StartDance();
        while (Live.FlourishMeter < PerformanceMath.FlourishMeterMax)
        {
            Live.RegisterCue(Live.Current.Primary, T0);
            Live.AdvanceBeat();
        }

        _performance.HandleInput(Player, FeatType.PerformanceFlourish);

        Messages().Should().Contain("Flourish!");
        _audienceCalls.Should().Contain(call => call.Member == "OnFlourish");
        Live.FlourishesPlayed.Should().Be(1);
        _engine.CallsTo("ApplyEffectToObject").Should().NotBeEmpty();
    }

    [Test]
    public void AToggleFeatPressedDuringAPerformanceStopsIt()
    {
        StartDance();

        _performance.HandleInput(Player, FeatType.Dance1);

        _performance.TryGetSession(Player, out _).Should().BeFalse();
    }

    [Test]
    public void AModeFeatPressedWithNoSessionRecoversTheHotBarAndRemovesTheFeats()
    {
        _db.Get<Player>("uuid-1001").PerformanceSavedHotBar = "left-over";
        _performance.Busy = true;
        _performance.BusyType = ActivityStatusType.Performing;

        _performance.HandleInput(Player, FeatType.PerformanceStepLeft);

        _creature.CallsTo("DeserializeQuickbar").Should().ContainSingle();
        _creature.CallsTo("RemoveFeat").Should().HaveCount(18, "the recovery and the stale-button clean-up each remove the nine mode feats");
        _performance.BusyCleared.Should().BeTrue();
    }

    [Test]
    public void LoggingInAfterACrashRestoresTheHotBarButNotForDmsOrNonPlayers()
    {
        _db.Get<Player>("uuid-1001").PerformanceSavedHotBar = "left-over";

        _performance.RecoverOnLogin();
        _creature.CallsTo("DeserializeQuickbar").Should().ContainSingle();

        _isPc = false;
        _creature.ClearCalls();
        _performance.RecoverOnLogin();
        _creature.CallsTo("DeserializeQuickbar").Should().BeEmpty();
    }

    [Test]
    public void LoggingInAfterTheRestoredBarWasSavedForgetsTheRecoveryCopy()
    {
        var player = _db.Get<Player>("uuid-1001");
        player.PerformanceSavedHotBar = "left-over";
        player.PerformanceHotBarRestoreSequence = 8;
        _performance.LoadedFileSequence = 8;

        _performance.RecoverOnLogin();

        _creature.CallsTo("DeserializeQuickbar").Should().BeEmpty();
        player.PerformanceSavedHotBar.Should().BeNull();
        player.PerformanceHotBarRestoreSequence.Should().Be(0);
    }

    [Test]
    public void LoggingInOnAFileOlderThanTheRestoredBarRestoresItAgain()
    {
        var player = _db.Get<Player>("uuid-1001");
        player.PerformanceSavedHotBar = "left-over";
        player.PerformanceHotBarRestoreSequence = 8;
        _performance.LoadedFileSequence = 7;
        _performance.FileSequence = 8;

        _performance.RecoverOnLogin();

        _creature.CallsTo("DeserializeQuickbar").Should().ContainSingle();
        player.PerformanceHotBarRestoreSequence.Should().BeGreaterThan(8);
    }

    [Test]
    public void ARecoveryWithNothingSavedDoesNothing()
    {
        _performance.RecoverOnLogin();

        _creature.CallsTo("DeserializeQuickbar").Should().BeEmpty();
    }

    [Test]
    public void ARecoveryDuringALivePerformanceDoesNothing()
    {
        StartDance();
        _creature.ClearCalls();

        _performance.RecoverOnLogin();

        _creature.CallsTo("DeserializeQuickbar").Should().BeEmpty();
    }

    [Test]
    public void ARoutineIsPickedOnlyWhenItsPerkIsOwned_AndASwitchDuringADanceTakesEffectAtOnce()
    {
        var routine = Enum.GetValues<PerformanceRoutineType>().First(value => value != PerformanceRoutineType.CantinaShuffle);

        _performance.SetRoutine(Player, routine).Should().BeFalse();

        _perks[PerformanceRiderCatalog.GetUnlockPerk(routine)] = 1;
        StartDance();
        _animationCalls.Clear();
        _windowCalls.Clear();

        _performance.SetRoutine(Player, routine).Should().BeTrue();

        _performance.GetPreference(Player).Routine.Should().Be(routine);
        Live.Routine.Should().Be(routine);
        _animationCalls.Should().NotBeEmpty();
        _windowCalls.Should().Contain("RefreshSetup");
    }

    [Test]
    public void ASetIsPickedOnlyWhenItsPerkIsOwned_AndASwitchDuringAPerformanceTakesEffectAtOnce()
    {
        var set = Enum.GetValues<PerformanceSetType>().First(value => value != PerformanceSetType.CantinaStandards);

        _performance.SetSet(Player, set).Should().BeFalse();

        _perks[PerformanceRiderCatalog.GetUnlockPerk(set)] = 1;
        StartMusic();
        _windowCalls.Clear();

        _performance.SetSet(Player, set).Should().BeTrue();

        Live.Set.Should().Be(set);
        _windowCalls.Should().Contain("RefreshSetup");
    }

    [Test]
    public void TheSecondRoutineAndSecondSetNeedTheirCapstonePerks()
    {
        var routine = Enum.GetValues<PerformanceRoutineType>().First(value => value != PerformanceRoutineType.CantinaShuffle);
        var set = Enum.GetValues<PerformanceSetType>().First(value => value != PerformanceSetType.CantinaStandards);
        _perks[PerformanceRiderCatalog.GetUnlockPerk(routine)] = 1;
        _perks[PerformanceRiderCatalog.GetUnlockPerk(set)] = 1;

        _performance.SetSecondRoutine(Player, routine).Should().BeFalse("no Showstopper");
        _performance.SetSecondSet(Player, set).Should().BeFalse("no Virtuoso");

        _perks[PerkType.Showstopper] = 1;
        _perks[PerkType.Virtuoso] = 1;
        StartDance();
        _windowCalls.Clear();
        _performance.SetSecondRoutine(Player, routine).Should().BeTrue();
        _windowCalls.Should().Contain("RefreshSetup");
        _performance.GetPreference(Player).SecondRoutine.Should().Be(routine);
        _performance.EndPerformance(Player, PerformanceEndReason.Stopped);

        StartMusic();
        _windowCalls.Clear();
        _performance.SetSecondSet(Player, set).Should().BeTrue();
        _windowCalls.Should().Contain("RefreshSetup");
        _performance.GetPreference(Player).SecondSet.Should().Be(set);
    }

    [Test]
    public void TheSongMustBeOneThePlayerOwns()
    {
        _performance.ChosenSong = 5;

        _performance.SetSong(Player, 9).Should().BeFalse();

        _performance.ChosenSong = 9;
        _performance.SetSong(Player, 9).Should().BeTrue();
        _performance.GetPreference(Player).SongTrackId.Should().Be(9);

        StartMusic();
        _performance.SetSong(Player, 9).Should().BeTrue();
        _audienceCalls.Should().Contain(call => call.Member == "SetPerformerSong");
    }

    [Test]
    public void RoleplayModeDropsTheCuesAndKeepsThePerformanceGoing()
    {
        _performance.SetRoleplay(Player, true);
        _performance.GetPreference(Player).Roleplay.Should().BeTrue();

        StartDance();
        Live.IsRoleplay.Should().BeTrue();
        _windowCalls.Should().NotContain("ShowFallbackStrip");
        _playerPlugin.CallsTo("SetQuickBarSlot").Count(call => (int)call.Arguments[1]! < 4 && call.Arguments[2]!.ToString() != string.Empty)
            .Should().BeGreaterThan(0);
        _performance.EndPerformance(Player, PerformanceEndReason.Stopped);
        Messages().Should().Contain(message => message.Contains("Roleplay mode"));
    }

    [Test]
    public void SwitchingModeDuringAPerformanceRedrawsTheBarAndTheWindow()
    {
        StartDance();
        _windowCalls.Clear();

        _performance.SetRoleplay(Player, true);
        _view.Modes.Should().Equal(true);
        _windowCalls.Should().Contain("ClearFallbackStrip").And.Contain("RefreshSetup");
        Messages().Should().Contain(message => message.StartsWith("Roleplay mode: no cues"));

        _performance.SetRoleplay(Player, true);
        _view.Modes.Should().HaveCount(1, "nothing changed");

        _performance.SetRoleplay(Player, false);
        _view.Modes.Should().Equal(true, false);
        Messages().Should().Contain(message => message.StartsWith("Active play: use the step buttons"));
    }

    [Test]
    public void LeavingRoleplayWithNoOpenWindowReopensItOrFallsBackToText()
    {
        StartMusic();
        _performance.SetRoleplay(Player, true);
        Live.View = null;
        _windowOpens = false;
        _windowCalls.Clear();

        _performance.SetRoleplay(Player, false);

        _windowCalls.Should().Contain("OpenCueWindow").And.Contain("ShowFallbackStrip");
        Messages().Should().Contain(message => message.StartsWith("Active play: use the note buttons"));
    }

    [Test]
    public void ATempoChangeDuringAPerformanceReschedulesTheBeatLoop()
    {
        StartDance();
        var first = _performance.Schedules[0];

        _performance.SetTempo(Player, PerformanceTempo.Upbeat);

        _performance.GetPreference(Player).Tempo.Should().Be(PerformanceTempo.Upbeat);
        first.Disposed.Should().BeTrue();
        _performance.Schedules.Should().HaveCount(2);
        Messages().Should().Contain(message => message.StartsWith("Tempo: Upbeat"));

        _performance.EndPerformance(Player, PerformanceEndReason.Stopped);
        _performance.SetTempo(Player, PerformanceTempo.Relaxed);
        _performance.GetPreference(Player).Tempo.Should().Be(PerformanceTempo.Relaxed);
    }

    [Test]
    public void ADanceBeatSplitsIntoStepsAndPushesOnlyTheLastToTheScoreboard()
    {
        StartDance();
        _view.Progress.Clear();

        Beats(1);
        Beats(1);
        _view.Progress.Should().HaveCount(0, "the window does not want progress");
        _view.WantsProgress = true;
        Beats(3);

        _view.Progress.Should().NotBeEmpty();
        _view.Beats.Should().Be(1, "one full beat has been scored");
    }

    [Test]
    public void AClosedCueWindowGetsOneLineOfScreenTextPerBeat()
    {
        StartMusic();
        Live.View = null;
        _windowCalls.Clear();

        Beats(1);

        _windowCalls.Should().Contain("ShowFallbackStrip");
    }

    [Test]
    public void ARoleplayBeatOnlyKeepsTheClipGoing()
    {
        _performance.SetRoleplay(Player, true);
        StartMusic();
        _windowCalls.Clear();
        _view.Beats = 0;

        Beats(1);

        _view.Beats.Should().Be(0);
        _windowCalls.Should().NotContain("ShowFallbackStrip");
    }

    [Test]
    public void TheLoopClipIsRenewedBeforeItLapses()
    {
        StartMusic();
        _animationCalls.Clear();

        _performance.Now = _performance.Now.AddSeconds(700);
        Beats(1);

        _animationCalls.Should().NotBeEmpty();
    }

    [Test]
    public void AnAccentHandsBackToTheLoopWhenItEnds()
    {
        StartMusic();
        _animationCalls.Clear();
        var delayed = HandBacks();

        delayed.Should().NotBeEmpty();
        _animationCalls.Clear();
        delayed[^1]();

        _animationCalls.Should().ContainSingle().Which.Should().StartWith("Play:", "the loop resumes");
    }

    [Test]
    public void AnAccentThatWasReplacedDoesNotHandBack()
    {
        StartMusic();
        var delayed = HandBacks();
        Live.AnimationToken = "newer";
        _animationCalls.Clear();

        delayed[^1]();

        _animationCalls.Should().BeEmpty();
    }

    [TestCase("invalid", PerformanceEndReason.Invalid)]
    [TestCase("dead", PerformanceEndReason.Died)]
    [TestCase("area", PerformanceEndReason.AreaExit)]
    [TestCase("combat", PerformanceEndReason.Combat)]
    [TestCase("moved", PerformanceEndReason.Moved)]
    [TestCase("instrument", PerformanceEndReason.InstrumentRemoved)]
    [TestCase("idle", PerformanceEndReason.Idle)]
    public void TheBeatLoopEndsThePerformanceOnTheFirstTrigger(string trigger, PerformanceEndReason reason)
    {
        StartMusic();
        switch (trigger)
        {
            case "invalid": _playerValid = false; break;
            case "dead": _isDead = true; break;
            case "area": _area = OtherArea; break;
            case "combat": _inCombat = true; break;
            case "moved": _position = new Vector3(100f, 0f, 0f); break;
            case "instrument": _slots[InventorySlot.RightHand] = ObjectInvalid; break;
            case "idle": _performance.Now = _performance.Now.AddMinutes(30); break;
        }

        Beats(1);

        _performance.TryGetSession(Player, out _).Should().BeFalse();
        _audienceCalls.Single(call => call.Member == "OnSessionEnded").Arguments[1].Should().Be(reason);
    }

    private const uint OtherArea = 501;

    [Test]
    public void ASessionStartedUnderTheNoInstrumentAllowanceNeverChecksTheSlot()
    {
        _ints[(Module, "PERFORMANCE_ALLOW_NO_INSTRUMENT")] = 1;
        _performance.TogglePlayInstrument(Player, 1);
        Live.Instrument.Should().Be(ObjectInvalid);

        Beats(1);

        _performance.TryGetSession(Player, out _).Should().BeTrue();
    }

    [Test]
    public void ABeatThatFailsEndsThePerformanceAsInvalid()
    {
        StartDance();
        _view.Throws = true;
        _view.WantsProgress = true;

        Beats(3);

        _performance.TryGetSession(Player, out _).Should().BeFalse();
        _audienceCalls.Single(call => call.Member == "OnSessionEnded").Arguments[1].Should().Be(PerformanceEndReason.Invalid);
    }

    [Test]
    public void ABeatWhoseCleanupAlsoFailsStillRemovesTheSession()
    {
        StartDance();
        _view.Throws = true;
        _audienceThrows = true;
        _closeThrows = true;

        Beats(3);

        _performance.TryGetSession(Player, out _).Should().BeFalse();
    }

    [Test]
    public void AStaleBeatScheduleDoesNothing()
    {
        StartDance();
        var schedule = _performance.Schedules[0];
        _performance.EndPerformance(Player, PerformanceEndReason.Stopped);
        _animationCalls.Clear();

        var stale = () => schedule.Action();

        stale.Should().NotThrow();
        _animationCalls.Should().BeEmpty("a beat of an ended performance does nothing");
    }

    [Test]
    public void TalkingKeepsARoleplayPerformanceGoingButCommandsDoNot()
    {
        _performance.SetRoleplay(Player, true);
        StartDance();
        var start = Live.LastInputUtc;
        _chat.SetReturn("GetChannel", ChatChannel.PlayerTalk);
        _chat.SetReturn("GetSender", Player);
        _chat.SetReturn("GetMessage", "/dance");
        _performance.Now = T0.AddMinutes(1);
        _performance.KeepRoleplayGoing();
        Live.LastInputUtc.Should().Be(start, "chat commands do not count");

        _chat.SetReturn("GetMessage", "  ");
        _performance.KeepRoleplayGoing();
        Live.LastInputUtc.Should().Be(start, "an empty line does not count");

        _chat.SetReturn("GetMessage", "hello");
        _performance.KeepRoleplayGoing();
        Live.LastInputUtc.Should().Be(T0.AddMinutes(1));

        _chat.SetReturn("GetChannel", ChatChannel.PlayerShout);
        _performance.Now = T0.AddMinutes(2);
        _performance.KeepRoleplayGoing();
        Live.LastInputUtc.Should().Be(T0.AddMinutes(1), "shouts are not talking");

        _chat.SetReturn("GetChannel", ChatChannel.PlayerTalk);
        _chat.SetReturn("GetSender", 9999u);
        _performance.KeepRoleplayGoing();
        Live.LastInputUtc.Should().Be(T0.AddMinutes(1), "somebody else is talking");
    }

    [Test]
    public void ChatWithNoLiveSessionIsIgnored()
    {
        var chat = () => _performance.KeepRoleplayGoing();

        chat.Should().NotThrow();
    }

    [Test]
    public void ActivePlayIsNotKeptGoingByTalking()
    {
        StartDance();
        var start = Live.LastInputUtc;
        _chat.SetReturn("GetChannel", ChatChannel.PlayerParty);
        _chat.SetReturn("GetSender", Player);
        _chat.SetReturn("GetMessage", "hello");
        _performance.Now = T0.AddMinutes(1);

        _performance.KeepRoleplayGoing();

        Live.LastInputUtc.Should().Be(start);
    }

    [Test]
    public void EachEndHandlerEndsThePerformanceForItsReason()
    {
        var cases = new (Action Handler, PerformanceEndReason Reason)[]
        {
            (_performance.EndOnLogout, PerformanceEndReason.LoggedOut),
            (_performance.EndOnAreaExit, PerformanceEndReason.AreaExit),
            (_performance.EndOnDeath, PerformanceEndReason.Died),
            (_performance.EndOnDamage, PerformanceEndReason.Combat),
            (_performance.EndOnAttack, PerformanceEndReason.Combat)
        };

        foreach (var (handler, reason) in cases)
        {
            _audienceCalls.Clear();
            StartDance();

            handler();

            _audienceCalls.Single(call => call.Member == "OnSessionEnded").Arguments[1].Should().Be(reason);
        }
    }

    [Test]
    public void RemovingTheInstrumentEndsAMusicalPerformanceOnlyForATaggedInstrument()
    {
        StartMusic();
        _engine.SetReturn("GetPCItemLastUnequippedBy", Player);
        _engine.SetReturn("GetPCItemLastUnequipped", Guitar);

        _performance.EndOnInstrumentUnequipped();
        _performance.TryGetSession(Player, out _).Should().BeTrue("a guitar by resref carries no family variable");

        _strings[(Guitar, "PERFORMANCE_INSTRUMENT_FAMILY")] = "Strings";
        _performance.EndOnInstrumentUnequipped();
        _performance.TryGetSession(Player, out _).Should().BeFalse();
    }

    [Test]
    public void RemovingAnythingDuringADanceIsIgnored()
    {
        StartDance();
        _engine.SetReturn("GetPCItemLastUnequippedBy", Player);
        _strings[(Guitar, "PERFORMANCE_INSTRUMENT_FAMILY")] = "Strings";
        _engine.SetReturn("GetPCItemLastUnequipped", Guitar);

        _performance.EndOnInstrumentUnequipped();

        _performance.TryGetSession(Player, out _).Should().BeTrue();
    }

    [Test]
    public void SessionsAreListedAsACopy()
    {
        StartDance();

        var sessions = _performance.GetSessions();
        _performance.EndPerformance(Player, PerformanceEndReason.Stopped);

        sessions.Should().ContainSingle();
        _performance.GetSessions().Should().BeEmpty();
    }

    private sealed class FakeView : IPerformanceView
    {
        public bool IsOpen { get; set; }
        public bool WantsProgress { get; set; }
        public bool Throws { get; set; }
        public bool Ended { get; private set; }
        public int Beats { get; set; }
        public List<float> Progress { get; } = new();
        public List<bool> Modes { get; } = new();
        public bool WantsBeatProgress => WantsProgress;

        public void ShowBeat(PerformanceSession session, PerformanceBeatOutcome lastOutcome)
        {
            if (Throws)
                throw new InvalidOperationException("view failure");

            Beats++;
        }

        public void ShowBeatProgress(float timeLeft) => Progress.Add(timeLeft);

        public void ShowSetup(PerformerSetup setup)
        {
        }

        public void ShowAudience(PerformerAudienceInfo info)
        {
        }

        public void ShowAudienceGets(string text)
        {
        }

        public void ShowMode(bool roleplay) => Modes.Add(roleplay);

        public void MarkEnded() => Ended = true;
    }

    private sealed class Schedule : IDisposable
    {
        public Schedule(Action action) => Action = action;

        public Action Action { get; }
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    private sealed class TestPerformance : PerformanceService
    {
        public TestPerformance(
            IDBService db,
            INamedAnimationService animation,
            IPerformanceAudienceService audience,
            IPerformanceWindowsService windows,
            IPerformanceSongService songs,
            IPerformanceRiderService riders)
            : base(db, animation, audience, windows, songs, riders, null!)
        {
        }

        public DateTime Now { get; set; }
        public int FileSequence { get; set; }
        public int LoadedFileSequence { get; set; }
        public int PersistCalls { get; private set; }
        public Dictionary<PerkType, int> Perks { get; set; } = new();
        public bool Busy { get; set; }
        public bool InSpace { get; set; }
        public ActivityStatusType BusyType { get; set; }
        public ActivityStatusType? BusySet { get; private set; }
        public bool BusyCleared { get; private set; }
        public int ChosenSong { get; set; } = -1;
        public List<Schedule> Schedules { get; } = new();
        protected override DateTime UtcNow() => Now;

        protected override IDisposable ScheduleRepeating(Action task, TimeSpan interval)
        {
            var schedule = new Schedule(task);
            Schedules.Add(schedule);
            return schedule;
        }

        protected override int GetPerkLevel(uint creature, PerkType perk) => Perks.GetValueOrDefault(perk);

        protected override int AdvanceFileSequence(uint player) => ++FileSequence;

        protected override int LoadedSequence(uint player) => LoadedFileSequence;

        protected override void PersistCharacter(uint player) => PersistCalls++;

        protected override bool IsBusy(uint target) => Busy;

        protected override ActivityStatusType GetBusyType(uint target) => BusySet ?? BusyType;

        protected override void SetBusy(uint target, ActivityStatusType type) => BusySet = type;

        protected override void ClearBusy(uint target)
        {
            BusyCleared = true;
            BusySet = null;
            BusyType = ActivityStatusType.Invalid;
        }

        protected override bool IsPlayerInSpaceMode(uint player) => InSpace;

        public override int ChooseSong(string playerId, int preferredTrackId = PerformanceSongs.AutomaticSong) => ChosenSong;
    }
}
