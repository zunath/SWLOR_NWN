using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Tests.Service.PerformanceTestSnapshots;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>
/// The Performance windows instance: opening and closing the cue and Watching windows, the cue strip text, and the
/// wording of the setup and audience lines. The unconverted Gui, Perk, Stat and PlayerName statics and the main
/// Performance facade are answered by a subclass; the engine is a recording fake.
/// </summary>
[TestFixture]
public class PerformanceWindowsServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 1001;

    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private ScriptedProxy _music = null!;
    private ScriptedProxy _venue = null!;
    private TestWindows _windows = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetName", arguments => $"Item{(uint)arguments[0]!}");
        _engine.SetReturnHandler("GetObjectUUID", arguments => $"uuid-{(uint)arguments[0]!}");
        _scope = NWScriptRuntime.Override(_engine);

        var music = ScriptedProxy.Create<IMusicService>(out _music);
        _music.On("GetTrackName", arguments => $"Track {(int)arguments[0]!}");
        var venue = ScriptedProxy.Create<IPerformanceVenueService>(out _venue);
        _venue.On("GetRating", _ => 2);
        _windows = new TestWindows(venue, music, new PerformanceSongService(music), new PerformanceSnapshotService(AmbientServices.LazyOf<IPerformanceService>(), AmbientServices.Of<IPerkService>(), AmbientServices.Of<IStatService>()));
        _windows.ReserveGuiIds();
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    private static PerformanceSession Session(PerformanceKind kind, uint instrument = ObjectInvalid, PerformanceInstrumentFamily family = PerformanceInstrumentFamily.Strings) =>
        new(Player, kind, family, PerformanceTempo.Relaxed, Start, new System.Random(3))
        {
            PlayerId = "uuid-1001",
            Instrument = instrument
        };

    [Test]
    public void TheCueStripIsDrawnCenteredUnderTheReservedIdAndThenCleared()
    {
        _windows.ShowFallbackStrip(Player, "abcdef", 1.5f);
        _windows.ClearFallbackStrip(Player);

        var posts = _engine.CallsTo("PostString");
        posts.Should().HaveCount(2);
        posts[0].Arguments[1].Should().Be("abcdef");
        posts[0].Arguments[2].Should().Be(-3);
        posts[0].Arguments[5].Should().Be(2.0f);
        posts[0].Arguments[8].Should().Be(70);
        posts[1].Arguments[1].Should().Be(string.Empty);
        posts[1].Arguments[8].Should().Be(70);
    }

    [Test]
    public void ADancerUsesTheFootworkWindowAndAMusicianTheSessionWindow()
    {
        _windows.GetCueWindowType(PerformanceKind.Dance).Should().Be(GuiWindowType.PerformanceFootwork);
        _windows.GetCueWindowType(PerformanceKind.Music).Should().Be(GuiWindowType.PerformanceSession);
    }

    [Test]
    public void OpeningTheCueWindowTogglesItAndAttachesItsView()
    {
        var view = new FakeView { IsOpen = true };
        var session = Session(PerformanceKind.Dance);
        _windows.ViewModels[(Player, GuiWindowType.PerformanceFootwork)] = view;

        _windows.OpenCueWindow(session).Should().BeTrue();

        _windows.Toggled.Should().Equal((Player, GuiWindowType.PerformanceFootwork));
        session.View.Should().BeSameAs(view);
    }

    [Test]
    public void AnAlreadyOpenCueWindowIsNotToggled()
    {
        var view = new FakeView { IsOpen = true };
        var session = Session(PerformanceKind.Music);
        _windows.Open.Add((Player, GuiWindowType.PerformanceSession));
        _windows.ViewModels[(Player, GuiWindowType.PerformanceSession)] = view;

        _windows.OpenCueWindow(session).Should().BeTrue();

        _windows.Toggled.Should().BeEmpty();
    }

    [Test]
    public void AWindowWithNoUsableViewIsNotAttached()
    {
        var session = Session(PerformanceKind.Dance);
        _windows.ViewModels[(Player, GuiWindowType.PerformanceFootwork)] = new object();
        _windows.OpenCueWindow(session).Should().BeFalse("the view model is not a performance view");

        _windows.ViewModels[(Player, GuiWindowType.PerformanceFootwork)] = new FakeView { IsOpen = false };
        _windows.OpenCueWindow(session).Should().BeFalse("the view is not open");
        session.View.Should().BeNull();
    }

    [Test]
    public void AFailureOpeningTheCueWindowIsLoggedAndReportedAsNotOpened()
    {
        _windows.Fail = true;

        _windows.OpenCueWindow(Session(PerformanceKind.Dance)).Should().BeFalse();
    }

    [Test]
    public void ClosingTheCueWindowDetachesTheViewAndClosesTheWindow()
    {
        var view = new FakeView { IsOpen = true };
        var session = Session(PerformanceKind.Dance);
        session.View = view;
        _windows.Open.Add((Player, GuiWindowType.PerformanceFootwork));

        _windows.CloseCueWindow(session, PerformanceEndReason.Stopped);

        session.View.Should().BeNull();
        view.Ended.Should().BeTrue();
        _windows.Closed.Should().Equal((Player, GuiWindowType.PerformanceFootwork));
    }

    [Test]
    public void ALoggedOutPlayersWindowIsLeftAlone_AndNoViewMeansNothingToClose()
    {
        var view = new FakeView { IsOpen = true };
        var session = Session(PerformanceKind.Dance);
        session.View = view;
        _windows.Open.Add((Player, GuiWindowType.PerformanceFootwork));

        _windows.CloseCueWindow(session, PerformanceEndReason.LoggedOut);
        _windows.CloseCueWindow(session, PerformanceEndReason.Stopped);

        view.Ended.Should().BeTrue();
        _windows.Closed.Should().BeEmpty();
    }

    [Test]
    public void AFailureClosingTheCueWindowIsLogged()
    {
        var session = Session(PerformanceKind.Dance);
        session.View = new FakeView { IsOpen = true };
        _windows.Fail = true;

        var close = () => _windows.CloseCueWindow(session, PerformanceEndReason.Stopped);

        close.Should().NotThrow();
    }

    [Test]
    public void TheOptionsButtonTogglesThePerformWindow_AndReopensAClosedCueWindow()
    {
        var session = Session(PerformanceKind.Music);
        _windows.ViewModels[(Player, GuiWindowType.PerformanceSession)] = new FakeView { IsOpen = true };

        _windows.ToggleOptions(session);

        _windows.Toggled.Should().Equal(
            (Player, GuiWindowType.PerformanceSession), (Player, GuiWindowType.PerformanceOptions));

        _windows.Toggled.Clear();
        session.View = new FakeView { IsOpen = true };
        _windows.ToggleOptions(session);
        _windows.Toggled.Should().Equal((Player, GuiWindowType.PerformanceOptions));
    }

    [Test]
    public void RefreshSetupPushesTheSetupAndTheAudienceLineToAnOpenWindowOnly()
    {
        var session = Session(PerformanceKind.Dance);
        _windows.RefreshSetup(session);

        var view = new FakeView { IsOpen = true };
        session.View = view;
        _windows.RefreshSetup(session);

        view.Setups.Should().HaveCount(1);
        view.AudienceGets.Should().HaveCount(1);
        view.AudienceGets[0].Should().NotBeNullOrEmpty();
    }

    [Test]
    public void ADancersSetupNamesTheRoutineAndASecondOneWithShowstopper()
    {
        var session = Session(PerformanceKind.Dance);
        session.Routine = PerformanceRoutineType.CantinaShuffle;
        _windows.Perks[PerkType.Showstopper] = 1;

        var plain = _windows.BuildSetup(session);
        plain.RoutineText.Should().Be(PerformanceRiderText.GetName(PerformanceRoutineType.CantinaShuffle, _windows.PerkLevelFor(session.Routine)));
        plain.TempoText.Should().Be(PerformanceTempo.Relaxed.GetLabel());

        var second = (PerformanceRoutineType)Enum.GetValues<PerformanceRoutineType>().First(routine => routine != PerformanceRoutineType.CantinaShuffle);
        _windows.Preference.SecondRoutine = second;
        _windows.UnlockedRoutines.Add(second);

        _windows.BuildSetup(session).RoutineText.Should().EndWith($" + {PerformanceRiderText.GetName(second)}");
    }

    [Test]
    public void AMusiciansSetupNamesTheSetTheSongAndTheInstrument()
    {
        var withItem = Session(PerformanceKind.Music, instrument: 9001);
        _windows.Preference.SongTrackId = 22;
        _windows.ChosenSong = 22;

        var setup = _windows.BuildSetup(withItem);

        setup.SongText.Should().Be("Track 22");
        setup.InstrumentText.Should().Be("Item9001");
        setup.SetText.Should().Be(PerformanceRiderText.GetName(PerformanceSetType.CantinaStandards, _windows.PerkLevelFor(PerformanceSetType.CantinaStandards)));

        _windows.ChosenSong = -1;
        var horns = Session(PerformanceKind.Music, family: PerformanceInstrumentFamily.Horns);
        var withHorns = _windows.BuildSetup(horns);
        withHorns.SongText.Should().Be("No song");
        withHorns.InstrumentText.Should().Be("Horn");
        _windows.BuildSetup(Session(PerformanceKind.Music)).InstrumentText.Should().Be("Strings");
    }

    [Test]
    public void AMusicianWithVirtuosoAndAnUnlockedSecondSetShowsBoth()
    {
        var second = Enum.GetValues<PerformanceSetType>().First(set => set != PerformanceSetType.CantinaStandards);
        _windows.Perks[PerkType.Virtuoso] = 1;
        _windows.Preference.SecondSet = second;
        _windows.UnlockedSets.Add(second);

        _windows.BuildSetup(Session(PerformanceKind.Music)).SetText.Should().EndWith($" + {PerformanceRiderText.GetName(second)}");
    }

    [Test]
    public void TheAudienceBonusScalesTheXpStatAndReadsTheDebtStat()
    {
        _windows.Stats[StatType.PerformanceDanceXPBonus] = 12;
        _windows.Stats[StatType.PerformanceDanceDebtClearPercent] = 5;
        _windows.Stats[StatType.PerformanceMusicXPBonus] = -3;
        _windows.Stats[StatType.PerformanceMusicDebtClearPercent] = -1;

        _windows.GetAudienceBonus(Player, PerformanceKind.Dance).Should().Be(new PerformanceAudienceBonus(12, 5));
        _windows.GetAudienceBonus(Player, PerformanceKind.Music).Should().Be(new PerformanceAudienceBonus(0, 0));
    }

    [Test]
    public void ADancersAudienceInfoCarriesTheVenueAndTheFollowerLines()
    {
        var dancer = Dancer(1, venue: 3);
        dancer.Crowd = 2;
        dancer.WatcherCount = 4;
        var show = new PerformanceAreaShow(500);
        show.Performers.Add(dancer);

        var info = _windows.BuildAudienceInfo(show, dancer);

        info.VenueText.Should().Be(PerformanceAudienceText.Venue(3));
        info.FollowingText.Should().Be(PerformanceAudienceText.Following(2, 4));
        info.InspiringText.Should().Be(PerformanceAudienceText.Inspiring(2, 4));
        info.AudienceGetsText.Should().Be(PerformanceAudienceText.AudienceGets(dancer));
        info.BandText.Should().BeEmpty();
    }

    [Test]
    public void AMusiciansAudienceInfoNamesTheBandTheDancersAndTheSong()
    {
        var leader = Musician(10, minutesAgo: 10);
        var follower = Musician(11, minutesAgo: 1, x: 1f, song: 30);
        var dancer = Dancer(12, x: 2f);
        var show = new PerformanceAreaShow(500);
        show.Performers.AddRange(new[] { leader, follower, dancer });
        PerformanceShow.Prepare(show.Performers);
        follower.BandLeader.Should().BeSameAs(leader);

        var info = _windows.BuildAudienceInfo(show, follower);

        info.BandText.Should().Contain("Name10");
        info.BandText.Should().Contain("Name12");
        info.AudienceHearsText.Should().Be(PerformanceAudienceText.Hears($"Track {follower.BandTrackId!.Value}"));

        var alone = Musician(20);
        var soloShow = new PerformanceAreaShow(501);
        soloShow.Performers.Add(alone);
        PerformanceShow.Prepare(soloShow.Performers);
        _windows.BuildAudienceInfo(soloShow, alone).AudienceHearsText.Should().Be(PerformanceAudienceText.Hears("Track 22"));
    }

    [Test]
    public void TheWatchingWindowOpensShowsAndCloses()
    {
        _windows.IsWatchingOpen(Player).Should().BeFalse();
        _windows.CloseWatching(Player);
        _windows.Closed.Should().BeEmpty();

        _windows.OpenOnToggle = true;
        _windows.OpenWatching(Player).Should().BeTrue();
        _windows.IsWatchingOpen(Player).Should().BeTrue();

        _windows.CloseWatching(Player);
        _windows.Closed.Should().Equal((Player, GuiWindowType.PerformanceWatching));
    }

    [Test]
    public void AFailureOpeningTheWatchingWindowIsLoggedAndReportedAsNotOpened()
    {
        _windows.Fail = true;

        _windows.OpenWatching(Player).Should().BeFalse();
    }

    [Test]
    public void ShowingTheWatchingViewIgnoresAWindowWhoseViewModelIsNotTheWatchingOne()
    {
        _windows.ViewModels[(Player, GuiWindowType.PerformanceWatching)] = new object();

        var show = () => _windows.ShowWatching(Player, null!);

        show.Should().NotThrow();
    }

    private sealed class FakeView : IPerformanceView
    {
        public bool IsOpen { get; set; }
        public bool WantsBeatProgress => false;
        public bool Ended { get; private set; }
        public List<PerformerSetup> Setups { get; } = new();
        public List<string> AudienceGets { get; } = new();

        public void ShowBeat(PerformanceSession session, PerformanceBeatOutcome lastOutcome)
        {
        }

        public void ShowBeatProgress(float timeLeft)
        {
        }

        public void ShowSetup(PerformerSetup setup) => Setups.Add(setup);

        public void ShowAudience(PerformerAudienceInfo info)
        {
        }

        public void ShowAudienceGets(string text) => AudienceGets.Add(text);

        public void ShowMode(bool roleplay)
        {
        }

        public void MarkEnded() => Ended = true;
    }

    private sealed class TestWindows : PerformanceWindowsService
    {
        public TestWindows(
            IPerformanceVenueService venue,
            IMusicService music,
            IPerformanceSongService songs,
            IPerformanceSnapshotService snapshots)
            : base(venue, music, songs, snapshots, AmbientServices.Of<IGuiService>(), AmbientServices.LazyOf<IPerformanceService>(), AmbientServices.Of<IPerkService>(), AmbientServices.Of<IPlayerNameService>(), AmbientServices.Of<IStatService>())
        {
        }

        public HashSet<(uint, GuiWindowType)> Open { get; } = new();
        public List<(uint, GuiWindowType)> Toggled { get; } = new();
        public List<(uint, GuiWindowType)> Closed { get; } = new();
        public Dictionary<(uint, GuiWindowType), object> ViewModels { get; } = new();
        public Dictionary<PerkType, int> Perks { get; } = new();
        public Dictionary<StatType, int> Stats { get; } = new();
        public HashSet<PerformanceRoutineType> UnlockedRoutines { get; } = new();
        public HashSet<PerformanceSetType> UnlockedSets { get; } = new();
        public PerformancePreference Preference { get; } = new();
        public int ChosenSong { get; set; } = -1;
        public bool Fail { get; set; }
        public bool OpenOnToggle { get; set; }

        public int PerkLevelFor(PerformanceRoutineType routine) => Perks.GetValueOrDefault(PerformanceRiderCatalog.GetUnlockPerk(routine));

        public int PerkLevelFor(PerformanceSetType set) => Perks.GetValueOrDefault(PerformanceRiderCatalog.GetUnlockPerk(set));

        protected override IdReservation ReserveIds(string systemName, int amount) =>
            new() { Count = amount, StartId = 70, EndId = 70 + amount - 1 };

        protected override bool IsWindowOpen(uint player, GuiWindowType type) => Open.Contains((player, type));

        protected override void TogglePlayerWindow(uint player, GuiWindowType type)
        {
            if (Fail)
                throw new InvalidOperationException("no window");

            Toggled.Add((player, type));
            if (OpenOnToggle)
                Open.Add((player, type));
        }

        protected override void ClosePlayerWindow(uint player, GuiWindowType type)
        {
            if (Fail)
                throw new InvalidOperationException("no window");

            Closed.Add((player, type));
        }

        protected override object GetWindowViewModel(uint player, GuiWindowType type) => ViewModels[(player, type)];

        protected override int GetPerkLevel(uint creature, PerkType perk) => Perks.GetValueOrDefault(perk);

        protected override int GetStatAdjustment(uint creature, StatType stat) => Stats.GetValueOrDefault(stat);

        protected override string GetDisplayName(uint observer, uint target) => $"Name{target}";

        protected override PerformancePreference GetPreference(uint player) => Preference;

        protected override bool IsRoutineUnlocked(uint player, PerformanceRoutineType routine) => UnlockedRoutines.Contains(routine);

        protected override bool IsSetUnlocked(uint player, PerformanceSetType set) => UnlockedSets.Contains(set);

        protected override PerformerSnapshot BuildSnapshot(PerformanceSession session, int venue, int songTrackId) =>
            session.Kind == PerformanceKind.Dance ? Dancer(session.Performer, venue: venue) : Musician(session.Performer, venue: venue);

        protected override int ChooseSong(string playerId, int preferredTrackId) => ChosenSong;

        protected override int ScaleBySocial(uint performer, int baseValue, int maximum) => baseValue;
    }
}
