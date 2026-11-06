using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>
/// The audience instance: the minute tick that turns watching into banked Inspired, debt clearing, band music and
/// performer XP, the Follow picks and the Flourish bonus. The engine is a recording fake, the database an in-memory one,
/// the neighbouring services scripted stand-ins, and the unconverted statics (Scheduler, Skill, Perk, Stat, StatusEffect,
/// the main Performance facade) are answered by a subclass. The tests move a clock the subclass owns.
/// </summary>
[TestFixture]
public class PerformanceAudienceServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Area = 500;
    private const uint OtherArea = 501;
    private const uint DancerId = 1001;
    private const uint MusicianId = 1002;
    private const uint WatcherId = 2001;
    private const uint SecondWatcherId = 2002;

    private static readonly DateTime T0 = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);

    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private InMemoryDBService _db = null!;
    private TestAudience _audience = null!;
    private ScriptedProxy _windowsScript = null!;
    private ScriptedProxy _musicScript = null!;
    private readonly List<uint> _online = new();
    private readonly Dictionary<uint, uint> _areas = new();
    private readonly Dictionary<uint, Vector3> _positions = new();
    private readonly HashSet<uint> _inCombat = new();
    private readonly HashSet<uint> _dead = new();
    private readonly HashSet<uint> _pcs = new();
    private readonly HashSet<uint> _watchingOpen = new();
    private readonly List<(uint Watcher, PerformanceWatchView View)> _shown = new();
    private readonly List<uint> _closed = new();
    private readonly List<(uint Player, int Track)> _played = new();
    private readonly List<uint> _restored = new();
    private int _cursor;

    [SetUp]
    public void SetUp()
    {
        _online.Clear();
        _areas.Clear();
        _positions.Clear();
        _inCombat.Clear();
        _dead.Clear();
        _pcs.Clear();
        _watchingOpen.Clear();
        _shown.Clear();
        _closed.Clear();
        _played.Clear();
        _restored.Clear();

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetFirstPC", _ =>
        {
            _cursor = 0;
            return _online.Count > 0 ? _online[0] : ObjectInvalid;
        });
        _engine.SetReturnHandler("GetNextPC", _ =>
        {
            _cursor++;
            return _cursor < _online.Count ? _online[_cursor] : ObjectInvalid;
        });
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetArea", arguments => _areas.GetValueOrDefault((uint)arguments[0]!, ObjectInvalid));
        _engine.SetReturnHandler("GetIsPC", arguments => _pcs.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetIsDM", _ => false);
        _engine.SetReturnHandler("GetIsDMPossessed", _ => false);
        _engine.SetReturnHandler("GetIsDead", arguments => _dead.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetIsInCombat", arguments => _inCombat.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetPosition", arguments => _positions.GetValueOrDefault((uint)arguments[0]!, Vector3.Zero));
        _engine.SetReturnHandler("GetPCPublicCDKey", arguments => $"key-{(uint)arguments[0]!}");
        _engine.SetReturnHandler("GetObjectUUID", arguments => $"uuid-{(uint)arguments[0]!}");
        _scope = NWScriptRuntime.Override(_engine);

        _db = new InMemoryDBService();

        var windows = ScriptedProxy.Create<IPerformanceWindowsService>(out _windowsScript);
        _windowsScript.On("IsWatchingOpen", arguments => _watchingOpen.Contains((uint)arguments[0]!));
        _windowsScript.On("OpenWatching", arguments =>
        {
            _watchingOpen.Add((uint)arguments[0]!);
            return true;
        });
        _windowsScript.On("ShowWatching", arguments =>
        {
            _shown.Add(((uint)arguments[0]!, (PerformanceWatchView)arguments[1]!));
            return null;
        });
        _windowsScript.On("CloseWatching", arguments =>
        {
            _closed.Add((uint)arguments[0]!);
            _watchingOpen.Remove((uint)arguments[0]!);
            return null;
        });
        _windowsScript.On("BuildAudienceInfo", _ => new PerformerAudienceInfo { VenueText = "venue" });

        var venue = ScriptedProxy.Create<IPerformanceVenueService>(out var venueScript);
        venueScript.On("GetAreaRating", _ => 1);
        venueScript.On("HasDanceFloorNearby", _ => false);
        venueScript.On("Combine", arguments => (int)arguments[0]! + ((bool)arguments[1]! ? 1 : 0));

        var music = ScriptedProxy.Create<IMusicService>(out _musicScript);
        _musicScript.On("PlayTemporaryTrack", arguments =>
        {
            _played.Add(((uint)arguments[0]!, (int)arguments[1]!));
            return null;
        });
        _musicScript.On("RestoreBackgroundMusic", arguments =>
        {
            _restored.Add((uint)arguments[0]!);
            return null;
        });

        _audience = new TestAudience(
            _db, venue, windows, music, new PerformanceSongService(music), new PerformanceSnapshotService(), new PerformanceInspiredService())
        {
            Now = T0,
            PositionOf = creature => _positions.GetValueOrDefault(creature, Vector3.Zero)
        };
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    private PerformanceSession Perform(uint performer, PerformanceKind kind, DateTime? startedUtc = null, float x = 0f)
    {
        var session = new PerformanceSession(
            performer, kind, PerformanceInstrumentFamily.Strings, PerformanceTempo.Relaxed, startedUtc ?? T0.AddMinutes(-10), new System.Random(1))
        {
            PlayerId = $"uuid-{performer}",
            Area = Area
        };
        _audience.Sessions[performer] = session;
        _areas[performer] = Area;
        _pcs.Add(performer);
        _positions[performer] = new Vector3(x, 0f, 0f);
        return session;
    }

    private void Watch(uint watcher, float x = 1f, uint area = Area)
    {
        _online.Add(watcher);
        _areas[watcher] = area;
        _pcs.Add(watcher);
        _positions[watcher] = new Vector3(x, 0f, 0f);
    }

    private void Tick(int secondsLater = 60)
    {
        _audience.Now = _audience.Now.AddSeconds(secondsLater);
        _audience.RunScheduledTick();
    }

    private void StartTickLoop() => _audience.StartAudienceTick();

    [Test]
    public void TheAudienceTickIsOneRepeatingScheduleThatIsReplacedWhenTheModuleLoadsAgain()
    {
        StartTickLoop();
        var first = _audience.Schedules[0];
        StartTickLoop();

        _audience.Schedules.Should().HaveCount(2);
        _audience.Schedules[0].Interval.Should().Be(TimeSpan.FromSeconds(60));
        first.Disposed.Should().BeTrue("the previous schedule is cancelled");
        _audience.Schedules[1].Disposed.Should().BeFalse();
    }

    [Test]
    public void ATickWithNoSessionsForgetsWatchersWhoStoppedWatching()
    {
        Perform(DancerId, PerformanceKind.Dance);
        Watch(WatcherId);
        StartTickLoop();
        Tick();
        _audience.Sessions.Clear();

        Tick();

        _audience.Follow(WatcherId, DancerId).Should().BeFalse("the performer has no session");
        _audience.BuildWatchingView(WatcherId).Should().BeNull("the watcher's state was released");
    }

    [Test]
    public void AWatcherInRangeIsServedAndTheWarmUpBanksNothing()
    {
        Perform(DancerId, PerformanceKind.Dance);
        Watch(WatcherId);
        StartTickLoop();

        Tick(0);

        _audience.Applied.Should().Contain(call => call.Creature == DancerId, "the performer is inspired by their own show");
        _audience.Applied.Should().NotContain(call => call.Creature == WatcherId, "the warm-up pays nothing");
        _windowsScript.Calls.Should().NotContain(call => call.Member == "OpenWatching", "the window opens after the first counted minute");
    }

    [Test]
    public void ADancersPerformerXpIsPaidOncePerTick()
    {
        Perform(DancerId, PerformanceKind.Dance, startedUtc: T0.AddMinutes(-10));
        Watch(WatcherId);
        StartTickLoop();

        Tick(0);
        Tick();

        _audience.SkillXp.Should().Contain(call => call.Player == DancerId && call.Skill == SkillType.Performance && call.Xp > 0);
    }

    [Test]
    public void ApplyingThePlanBanksInspiredClearsDebtAndOpensTheWatchingWindow()
    {
        Perform(DancerId, PerformanceKind.Dance);
        _audience.Debt = 50;
        _db.With(new Player { Id = "uuid-2001", XPDebt = 100 });
        Watch(WatcherId);
        StartTickLoop();

        Tick(0);
        Tick();
        Tick();
        Tick();

        _audience.Applied.Should().Contain(call => call.Creature == WatcherId);
        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).Should()
            .Contain(message => message.StartsWith("The performance inspires you."))
            .And.Contain(message => message.StartsWith("The performance clears 50 XP of your debt."));
        _db.Saved.OfType<Player>().Last().XPDebt.Should().Be(50);
        _windowsScript.Calls.Should().Contain(call => call.Member == "OpenWatching");
        _shown.Should().NotBeEmpty();
        _audience.BuildWatchingView(WatcherId).Should().NotBeNull();
    }

    [Test]
    public void AWatcherWhoAlreadyHoldsInspiredIsNotToldAgain_AndNoDebtMeansNoPlayerWrite()
    {
        Perform(DancerId, PerformanceKind.Dance);
        _audience.Effects[WatcherId] = new InspiredStatusEffect();
        Watch(WatcherId);
        StartTickLoop();

        Tick(0);
        Tick();
        Tick();
        Tick();

        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).Should()
            .NotContain(message => message.StartsWith("The performance inspires you."));
        _db.Saved.Should().BeEmpty("the performer clears no debt");
    }

    [Test]
    public void InspiredHeldDuringTheWarmUpDoesNotRunDown()
    {
        Perform(DancerId, PerformanceKind.Dance);
        _audience.Effects[WatcherId] = new InspiredStatusEffect();
        Watch(WatcherId);
        StartTickLoop();

        Tick(0);

        _audience.Remaining.Should().Contain(call => call.Creature == WatcherId);
    }

    [Test]
    public void ADeadWatcherInCombatOrPerformingIsNotCounted()
    {
        Perform(DancerId, PerformanceKind.Dance);
        Perform(MusicianId, PerformanceKind.Music);
        Watch(WatcherId);
        Watch(SecondWatcherId);
        _dead.Add(WatcherId);
        _inCombat.Add(SecondWatcherId);
        _online.Add(MusicianId);
        StartTickLoop();

        Tick(0);

        _audience.Applied.Should().NotContain(call => call.Creature == WatcherId || call.Creature == SecondWatcherId);
    }

    [Test]
    public void AWatcherOutOfRangeOrInAnotherShowIsNotServed_AndASecondCharacterOnOneKeyIsNotCounted()
    {
        var performer = Perform(DancerId, PerformanceKind.Dance);
        Watch(WatcherId, x: 100f);
        Watch(SecondWatcherId, area: OtherArea);
        StartTickLoop();
        Tick(0);
        _audience.Applied.Should().NotContain(call => call.Creature == WatcherId || call.Creature == SecondWatcherId);

        _positions[WatcherId] = new Vector3(2f, 0f, 0f);
        _engine.SetReturnHandler("GetPCPublicCDKey", _ => "shared-key");
        Watch(SecondWatcherId, x: 3f);
        Tick();

        performer.Should().NotBeNull();
        _audience.BuildWatchingView(WatcherId).Should().BeNull("no window was drawn yet");
    }

    [Test]
    public void AMusiciansBandSongIsSentToTheWatcherOnce_AndRestoredWhenTheyLeave()
    {
        Perform(MusicianId, PerformanceKind.Music);
        Watch(WatcherId);
        _audience.Preferences[WatcherId] = new PerformancePreference();
        StartTickLoop();

        Tick(0);
        Tick();

        _played.Should().ContainSingle(play => play.Player == WatcherId);

        _audience.Sessions.Clear();
        Tick();
        _restored.Should().Contain(WatcherId);
    }

    [Test]
    public void AWatcherWhoTurnedOffThePerformersSongKeepsTheirOwnMusic()
    {
        Perform(MusicianId, PerformanceKind.Music);
        Watch(WatcherId);
        _audience.Preferences[WatcherId] = new PerformancePreference { PlayPerformerSong = false };
        StartTickLoop();

        Tick(0);

        _played.Should().BeEmpty();
    }

    [Test]
    public void TheWatchingBoxSwitchesTheBandSongOffAndOn()
    {
        Perform(MusicianId, PerformanceKind.Music);
        Watch(WatcherId);
        _audience.Preferences[WatcherId] = new PerformancePreference();
        StartTickLoop();
        Tick(0);
        Tick();
        Tick();
        Tick();
        _played.Clear();
        _restored.Clear();

        _audience.SetPlayPerformerSong(WatcherId, false);
        _restored.Should().Contain(WatcherId);
        _audience.Preferences[WatcherId].PlayPerformerSong.Should().BeFalse();

        _audience.SetPlayPerformerSong(WatcherId, true);
        _played.Should().ContainSingle(play => play.Player == WatcherId);

        _played.Clear();
        _audience.SetPlayPerformerSong(WatcherId, true);
        _played.Should().BeEmpty("the band song is already playing");

        _audience.SetPlayPerformerSong(SecondWatcherId, true);
        _audience.Preferences[SecondWatcherId].PlayPerformerSong.Should().BeTrue("a player with no watching state only keeps the choice");
    }

    [Test]
    public void FollowingAPerformerNeedsALiveSessionAndAValidWatcher()
    {
        Perform(DancerId, PerformanceKind.Dance);
        Perform(MusicianId, PerformanceKind.Music);

        _audience.Follow(WatcherId, WatcherId).Should().BeFalse("nobody follows themselves");
        _audience.Follow(WatcherId, 9999).Should().BeFalse("the performer has no session");
        _audience.Follow(ObjectInvalid, DancerId).Should().BeFalse("the watcher is not valid");
        _audience.Follow(WatcherId, DancerId).Should().BeTrue();
        _audience.Follow(WatcherId, MusicianId).Should().BeTrue();

        _audience.Unfollow(WatcherId, DancerId).Should().BeTrue();
        _audience.Unfollow(WatcherId, MusicianId).Should().BeTrue();
        _audience.Unfollow(WatcherId, MusicianId).Should().BeFalse("nothing left to drop");
        _audience.Unfollow(WatcherId, 0).Should().BeFalse();
        _audience.Unfollow(SecondWatcherId, DancerId).Should().BeFalse("no state for the watcher");
    }

    [Test]
    public void AFollowClickRedrawsTheWatchingWindowFromTheLastTick()
    {
        Perform(DancerId, PerformanceKind.Dance);
        Watch(WatcherId);
        StartTickLoop();
        Tick(0);
        Tick();
        Tick();
        Tick();
        _shown.Clear();

        _audience.RefreshWatching(WatcherId);
        _shown.Should().ContainSingle();

        _watchingOpen.Clear();
        _shown.Clear();
        _audience.RefreshWatching(WatcherId);
        _shown.Should().BeEmpty("a closed window costs nothing");

        _audience.RefreshWatching(SecondWatcherId);
        _shown.Should().BeEmpty("a player with no last tick has nothing to redraw");
    }

    [Test]
    public void ClosingTheWatchingWindowKeepsItClosedForThatShow()
    {
        Perform(DancerId, PerformanceKind.Dance);
        Watch(WatcherId);
        StartTickLoop();
        Tick(0);
        Tick();

        _audience.OnWatchingWindowClosed(WatcherId);
        _watchingOpen.Clear();
        _windowsScript.Calls.Count(call => call.Member == "OpenWatching").Should().Be(1);
        Tick();

        _windowsScript.Calls.Count(call => call.Member == "OpenWatching").Should().Be(1, "a dismissed window stays closed");
        _audience.OnWatchingWindowClosed(SecondWatcherId);
    }

    [Test]
    public void ALeavingPerformerIsRemovedFromEveryWatchersChoices_AndTheirBandSongStops()
    {
        Perform(DancerId, PerformanceKind.Dance);
        var musician = Perform(MusicianId, PerformanceKind.Music);
        Watch(WatcherId);
        _audience.Preferences[WatcherId] = new PerformancePreference();
        StartTickLoop();
        _audience.Follow(WatcherId, DancerId);
        _audience.Follow(WatcherId, MusicianId);
        Tick(0);
        Tick();
        _played.Should().NotBeEmpty();
        _restored.Clear();

        _audience.OnSessionEnded(musician, PerformanceEndReason.Stopped);
        _audience.OnSessionEnded(_audience.Sessions[DancerId], PerformanceEndReason.Stopped);

        _restored.Should().Contain(WatcherId);
        _audience.BuildWatchingView(WatcherId).Should().NotBeNull();
    }

    [Test]
    public void ASongPickedWhilePerformingIsUsedAtTheNextTick()
    {
        Perform(MusicianId, PerformanceKind.Music);
        Watch(WatcherId);
        _audience.Preferences[WatcherId] = new PerformancePreference();
        StartTickLoop();
        Tick(0);

        _audience.SetPerformerSong(MusicianId, 44);
        _audience.SetPerformerSong(9999, 44);
        Tick();

        _played.Should().Contain(play => play.Track == 44);
    }

    [Test]
    public void AFlourishBanksMinutesForWarmedUpFollowers()
    {
        var dancer = Perform(DancerId, PerformanceKind.Dance);
        Watch(WatcherId);
        _audience.Effects[WatcherId] = new InspiredStatusEffect();
        StartTickLoop();
        Tick(0);
        Tick();
        Tick();
        Tick();
        _audience.Remaining.Clear();
        _audience.OnFlourishNow(dancer);

        _audience.Remaining.Should().Contain(call => call.Creature == WatcherId && call.BankedAgain);
        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).Should()
            .Contain(message => message.StartsWith("The flourish thrills you."));
    }

    [Test]
    public void TwoFlourishesWithinTwoSecondsPayBothAudiencesDouble()
    {
        var dancer = Perform(DancerId, PerformanceKind.Dance);
        var musician = Perform(MusicianId, PerformanceKind.Music, x: 2f);
        Watch(WatcherId);
        _audience.Effects[WatcherId] = new InspiredStatusEffect();
        StartTickLoop();
        Tick(0);
        Tick();
        Tick();
        Tick();
        _audience.Remaining.Clear();
        _audience.Sessions[MusicianId] = FlourishedAt(musician, _audience.Now.AddSeconds(-1));

        _audience.OnFlourishNow(dancer);

        _audience.Remaining.Should().Contain(call => call.Creature == WatcherId);
    }

    [Test]
    public void AFlourishPaysNothingToAWatcherWhoCannotWatchRightNow()
    {
        var dancer = Perform(DancerId, PerformanceKind.Dance);
        Watch(WatcherId);
        _audience.Effects[WatcherId] = new InspiredStatusEffect();
        StartTickLoop();
        Tick(0);
        Tick();
        Tick();
        Tick();
        _audience.Remaining.Clear();
        _inCombat.Add(WatcherId);

        _audience.OnFlourishNow(dancer);

        _audience.Remaining.Should().BeEmpty();
    }

    [Test]
    public void WatchingStopsWhenTheWatcherChangesAreaDiesDisconnectsOrStepsOutOfRange()
    {
        Perform(DancerId, PerformanceKind.Dance);
        Watch(WatcherId);
        _audience.Effects[WatcherId] = new InspiredStatusEffect();
        StartTickLoop();
        Tick(0);
        Tick();
        Tick();
        Tick();

        _engine.SetReturn("GetEnteringObject", WatcherId);
        _audience.StopWatchingOnAreaEnter();
        _audience.BuildWatchingView(WatcherId).Should().BeNull("area entry resets the cached view");
        _audience.StopWatchingOnAreaEnter();

        Tick();
        _engine.SetReturn("GetLastPlayerDied", WatcherId);
        _audience.StopWatchingOnDeath();
        _audience.BuildWatchingView(WatcherId).Should().BeNull();

        Tick();
        _engine.SetReturn("OBJECT_SELF", WatcherId);
        _audience.RestoreFrozenInspiredOnDisconnect();
        _audience.BuildWatchingView(WatcherId).Should().BeNull();
        _audience.Remaining.Should().NotBeEmpty("the frozen remainder is handed back");
    }

    [Test]
    public void TheHeartbeatInterruptsWatchingOnlyWhenNoPerformerCoversTheWatcher()
    {
        Perform(DancerId, PerformanceKind.Dance);
        Watch(WatcherId);
        StartTickLoop();
        _engine.SetReturn("OBJECT_SELF", WatcherId);

        _audience.StopWatchingWhenOutOfRange();
        Tick(0);
        Tick();
        _audience.StopWatchingWhenOutOfRange();
        _audience.BuildWatchingView(WatcherId).Should().NotBeNull("a performer still covers the watcher");

        _positions[WatcherId] = new Vector3(500f, 0f, 0f);
        _audience.StopWatchingWhenOutOfRange();

        _audience.BuildWatchingView(WatcherId).Should().BeNull();
        _closed.Should().Contain(WatcherId);

        Tick();
        _inCombat.Add(WatcherId);
        _positions[WatcherId] = new Vector3(1f, 0f, 0f);
        _audience.StopWatchingWhenOutOfRange();
    }

    [Test]
    public void StartingToPerformInterruptsWatchingAtOnce()
    {
        Perform(DancerId, PerformanceKind.Dance);
        Watch(WatcherId);
        StartTickLoop();
        Tick(0);
        Tick();

        _audience.OnPerformanceStarted(WatcherId);
        _audience.OnPerformanceStarted(9999);

        _audience.BuildWatchingView(WatcherId).Should().BeNull();
    }

    [Test]
    public void LeavingTheModuleAndForgettingTheBandSong()
    {
        Perform(MusicianId, PerformanceKind.Music);
        Watch(WatcherId);
        _audience.Preferences[WatcherId] = new PerformancePreference();
        StartTickLoop();
        Tick(0);
        _played.Should().HaveCount(1);

        _audience.ForgetBandSong(WatcherId);
        _audience.ForgetBandSong(9999);
        Tick();
        _played.Should().HaveCount(2, "the band song is sent again after it was forgotten");

        _engine.SetReturn("GetExitingObject", WatcherId);
        _audience.ForgetExitingPlayer();
        _audience.BuildWatchingView(WatcherId).Should().BeNull();
    }

    [Test]
    public void AShowWithAFailingParticipantStillServesTheOthers()
    {
        var broken = Perform(DancerId, PerformanceKind.Dance);
        Perform(MusicianId, PerformanceKind.Music);
        Watch(WatcherId);
        _audience.FailFor = broken.Performer;
        StartTickLoop();

        var tick = () => Tick(0);

        tick.Should().NotThrow();
        _audience.Applied.Should().Contain(call => call.Creature == MusicianId);
    }

    [Test]
    public void ATickThatFailsIsLoggedAndTheNextOneRuns()
    {
        Perform(DancerId, PerformanceKind.Dance);
        _audience.FailSessions = true;
        StartTickLoop();

        var tick = () => Tick(0);

        tick.Should().NotThrow();
        _audience.FailSessions = false;
        tick.Should().NotThrow();
    }

    [Test]
    public void APerformerInAnInvalidAreaIsSkipped()
    {
        Perform(DancerId, PerformanceKind.Dance);
        _areas[DancerId] = ObjectInvalid;
        Watch(WatcherId);
        StartTickLoop();

        Tick(0);

        _audience.Applied.Should().BeEmpty();
        _areas[DancerId] = Area;
        _audience.Sessions[MusicianId] = new PerformanceSession(
            ObjectInvalid, PerformanceKind.Music, PerformanceInstrumentFamily.Strings, PerformanceTempo.Relaxed, T0, new System.Random(1));
        Tick(0);
        _audience.Applied.Should().NotBeEmpty("the valid performer is still served");
    }

    [Test]
    public void APerformersCueWindowLearnsWhoIsWatching()
    {
        var dancer = Perform(DancerId, PerformanceKind.Dance);
        var view = new ViewRecorder { IsOpen = true };
        dancer.View = view;
        Watch(WatcherId);
        StartTickLoop();

        Tick(0);

        view.Audience.Should().ContainSingle().Which.VenueText.Should().Be("venue");
    }

    [Test]
    public void AWatcherWhoStartsPerformingWithoutTheStartHookIsCaughtByTheTick()
    {
        Perform(DancerId, PerformanceKind.Dance);
        Watch(WatcherId);
        StartTickLoop();
        Tick(0);
        Tick();
        _audience.BuildWatchingView(WatcherId).Should().NotBeNull();

        Perform(WatcherId, PerformanceKind.Music, x: 1f);
        _online.Remove(WatcherId);
        Watch(SecondWatcherId, x: 2f);
        Tick();

        _audience.BuildWatchingView(WatcherId).Should().BeNull("performing interrupts watching");
        _closed.Should().Contain(WatcherId);
    }

    private static PerformanceSession FlourishedAt(PerformanceSession session, DateTime flourishUtc)
    {
        while (session.FlourishMeter < PerformanceMath.FlourishMeterMax)
        {
            session.RegisterCue(session.Current.Primary, flourishUtc);
            session.AdvanceBeat();
        }

        session.TryConsumeFlourish(flourishUtc).Should().BeTrue();
        return session;
    }

    private sealed class ViewRecorder : IPerformanceView
    {
        public bool IsOpen { get; set; }
        public bool WantsBeatProgress => false;
        public List<PerformerAudienceInfo> Audience { get; } = new();

        public void ShowBeat(PerformanceSession session, PerformanceBeatOutcome lastOutcome)
        {
        }

        public void ShowBeatProgress(float timeLeft)
        {
        }

        public void ShowSetup(PerformerSetup setup)
        {
        }

        public void ShowAudience(PerformerAudienceInfo info) => Audience.Add(info);

        public void ShowAudienceGets(string text)
        {
        }

        public void ShowMode(bool roleplay)
        {
        }

        public void MarkEnded()
        {
        }
    }

    private sealed class Schedule : IDisposable
    {
        public Schedule(Action action, TimeSpan interval)
        {
            Action = action;
            Interval = interval;
        }

        public Action Action { get; }
        public TimeSpan Interval { get; }
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    private sealed class TestAudience : PerformanceAudienceService
    {
        public TestAudience(
            IDBService db,
            IPerformanceVenueService venue,
            IPerformanceWindowsService windows,
            IMusicService music,
            IPerformanceSongService songs,
            IPerformanceSnapshotService snapshots,
            IPerformanceInspiredService inspired)
            : base(db, venue, windows, music, songs, snapshots, inspired)
        {
        }

        public DateTime Now { get; set; }
        public Func<uint, Vector3> PositionOf { get; set; } = _ => Vector3.Zero;
        public int Debt { get; set; }
        public uint FailFor { get; set; }
        public bool FailSessions { get; set; }
        public List<Schedule> Schedules { get; } = new();
        public Dictionary<uint, PerformanceSession> Sessions { get; } = new();
        public Dictionary<uint, PerformancePreference> Preferences { get; } = new();
        public Dictionary<uint, InspiredStatusEffect> Effects { get; } = new();
        public List<(uint Creature, double Seconds, bool BankedAgain)> Applied { get; } = new();
        public List<(uint Creature, double Seconds, bool BankedAgain)> Remaining { get; } = new();
        public List<(uint Player, SkillType Skill, int Xp)> SkillXp { get; } = new();

        public void RunScheduledTick() => Schedules[^1].Action();

        public void OnFlourishNow(PerformanceSession session) => OnFlourish(session);

        protected override DateTime UtcNow() => Now;

        protected override IDisposable ScheduleRepeating(Action action, TimeSpan interval)
        {
            var schedule = new Schedule(action, interval);
            Schedules.Add(schedule);
            return schedule;
        }

        protected override bool TryGetSession(uint performer, out PerformanceSession session) =>
            Sessions.TryGetValue(performer, out session!);

        protected override IReadOnlyList<PerformanceSession> GetSessions()
        {
            if (FailSessions)
                throw new InvalidOperationException("tick failure");

            return Sessions.Values.ToArray();
        }

        protected override PerformancePreference GetPreference(uint player)
        {
            if (!Preferences.TryGetValue(player, out var preference))
            {
                preference = new PerformancePreference();
                Preferences[player] = preference;
            }

            return preference;
        }

        protected override InspiredStatusEffect GetInspiredEffect(uint creature) => Effects.GetValueOrDefault(creature)!;

        protected override void GiveSkillXP(uint player, SkillType skill, int xp) => SkillXp.Add((player, skill, xp));

        protected override int GetPerkLevel(uint creature, PerkType perk) => 0;

        protected override int GetStatAdjustment(uint creature, StatType stat) => 0;

        protected override string GetDisplayName(uint observer, uint target) => $"Name{target}";

        protected override PerformerSnapshot BuildSnapshot(PerformanceSession session, int venue, int songTrackId)
        {
            if (session.Performer == FailFor)
                throw new InvalidOperationException("snapshot failure");

            return new PerformerSnapshot
            {
                Performer = session.Performer,
                Kind = session.Kind,
                StartedUtc = session.StartedUtc,
                Quality = PerformanceQuality.Steady,
                Venue = venue,
                XpBase = 6,
                XpScaled = 6,
                DebtClearPercent = session.Kind == PerformanceKind.Dance ? Debt : 0,
                Position = PositionOf(session.Performer),
                SongTrackId = songTrackId,
                Session = session
            };
        }

        protected override int ChooseSong(string playerId, int preferredTrackId) => preferredTrackId >= 0 ? preferredTrackId : 22;

        protected override bool ApplyInspired(uint creature, InspiredEffectData data, double remainingSeconds, bool bankedAgain)
        {
            Applied.Add((creature, remainingSeconds, bankedAgain));
            return true;
        }

        protected override double GetInspiredRemainingSeconds(uint creature, DateTime nowUtc) =>
            Effects.ContainsKey(creature) ? 300.0 : 0.0;

        protected override void SetInspiredRemaining(uint creature, double remainingSeconds, bool bankedAgain = false) =>
            Remaining.Add((creature, remainingSeconds, bankedAgain));
    }
}
