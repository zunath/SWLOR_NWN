using System.ComponentModel;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The Performance windows are bound to a view model that pushes a value to the client on every assignment, so these tests
/// count the pushes (property notifications) a beat, a step and a tick cause. The cost rule: only changed values are pushed.
/// </summary>
public class PerformanceWindowViewTests
{
    private static readonly DateTime Start = new(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

    private static PerformanceSession CreateSession(PerformanceKind kind, int seed = 7)
    {
        return new PerformanceSession(
            1u, kind, PerformanceInstrumentFamily.Strings, PerformanceTempo.Relaxed, Start, new Random(seed));
    }

    private static List<string> Track(INotifyPropertyChanged model)
    {
        var pushed = new List<string>();
        model.PropertyChanged += (_, args) => pushed.Add(args.PropertyName);
        return pushed;
    }

    private static string[] IconsOf(PerformanceSession session)
    {
        var upcoming = new PerformanceBeat[PerformanceSession.VisibleBeatCount - 1];
        session.CopyUpcomingTo(upcoming);
        return new[] { session.Current }.Concat(upcoming)
            .Select(beat => beat.IsRest ? string.Empty : PerformanceCueIcons.GetIcon(beat.Primary))
            .ToArray();
    }

    private static void HitAndAdvance(PerformanceSession session)
    {
        session.RegisterCue(session.Current.Primary, Start);
        session.AdvanceBeat();
    }

    private static string[] CueImageNames()
    {
        return Enumerable.Range(0, PerformanceFootworkViewModel.CueCount).Select(index => $"Cue{index}Image").ToArray();
    }

    [Test]
    public void Footwork_FirstBeatShowsTheWholeStrip()
    {
        var model = new PerformanceFootworkViewModel();
        var pushed = Track(model);
        var session = CreateSession(PerformanceKind.Dance);

        model.ShowBeat(session, PerformanceBeatOutcome.Hit);

        pushed.Should().Contain(CueImageNames());
        pushed.Should().Contain(Enumerable.Range(0, 6).Select(index => $"Cue{index}Slot"));
        model.Cue0Image.Should().Be(PerformanceCueIcons.GetIcon(session.Current.Primary));
        model.Cue0Slot.Should().Be(PerformanceCueIcons.GetSlotText(session.Current.Primary));
        model.BeatProgress.Should().Be(1f);
        model.LastResultText.Should().Be("Hit last cue");
    }

    [Test]
    public void Footwork_SameBeatAgain_PushesNothing()
    {
        var model = new PerformanceFootworkViewModel();
        var session = CreateSession(PerformanceKind.Dance);
        model.ShowBeat(session, PerformanceBeatOutcome.Hit);
        var pushed = Track(model);

        model.ShowBeat(session, PerformanceBeatOutcome.Hit);

        pushed.Should().BeEmpty("nothing changed, and every assignment to a bound property sends a bind");
    }

    [Test]
    public void Footwork_NextBeat_PushesOnlyTheTilesWhoseCueChangedPlusTheReadouts()
    {
        var model = new PerformanceFootworkViewModel();
        var session = CreateSession(PerformanceKind.Dance);
        model.ShowBeat(session, PerformanceBeatOutcome.Hit);
        var before = IconsOf(session);
        var pushed = Track(model);

        HitAndAdvance(session);
        model.ShowBeat(session, PerformanceBeatOutcome.Hit);

        var after = IconsOf(session);
        var expectedImages = Enumerable.Range(0, before.Length)
            .Where(index => before[index] != after[index])
            .Select(index => $"Cue{index}Image");
        pushed.Where(name => name.EndsWith("Image")).Should().BeEquivalentTo(expectedImages);
        pushed.Should().Contain(new[] { "StreakText", "FlourishText", "FlourishProgress" });
        pushed.Should().NotContain(new[] { "RoutineText", "TempoText", "VenueText", "FollowingText", "InspiringText" });
        pushed.Should().NotContain("BeatProgress", "the bar was already full");
        pushed.Should().OnlyHaveUniqueItems("a value is pushed at most once per beat");
    }

    [Test]
    public void Footwork_BeatBarStepsPushOnlyTheBar()
    {
        var model = new PerformanceFootworkViewModel();
        model.ShowBeat(CreateSession(PerformanceKind.Dance), PerformanceBeatOutcome.Hit);
        var pushed = Track(model);

        model.ShowBeatProgress(2f / 3f);
        model.ShowBeatProgress(2f / 3f);
        model.ShowBeatProgress(1f / 3f);

        pushed.Should().Equal("BeatProgress", "BeatProgress");
    }

    [Test]
    public void Footwork_SetupAndAudienceArePushedOnlyWhenTheyChange()
    {
        var model = new PerformanceFootworkViewModel();
        var setup = new PerformerSetup { RoutineText = "Power Step", TempoText = "Relaxed 1.5s" };
        var audience = new PerformerAudienceInfo
        {
            VenueText = "4 / 5",
            FollowingText = "2 of 3 in range",
            InspiringText = "Inspiring 2 followers"
        };
        model.ShowSetup(setup);
        model.ShowAudience(audience);
        var pushed = Track(model);

        model.ShowSetup(setup);
        model.ShowAudience(audience);
        pushed.Should().BeEmpty();

        model.ShowAudience(audience with { FollowingText = "3 of 3 in range" });
        pushed.Should().Equal("FollowingText");
        model.FollowingText.Should().Be("3 of 3 in range");
    }

    [Test]
    public void Footwork_QualityHighlightsTheCurrentTierWithItsPercent()
    {
        var model = new PerformanceFootworkViewModel();
        var session = CreateSession(PerformanceKind.Dance);
        for (var beat = 0; beat < 20; beat++)
            HitAndAdvance(session);

        model.ShowBeat(session, PerformanceBeatOutcome.Hit);

        model.QualityDazzlingText.Should().Be("Dazzling 100%");
        model.QualityDazzlingColor.Should().BeSameAs(PerformanceWindowColors.Green);
        model.QualityRoughText.Should().Be("Rough");
        model.QualityRoughColor.Should().BeSameAs(PerformanceWindowColors.Dim);
        model.StreakText.Should().Be("20");
        model.FlourishText.Should().Be("Flourish 16 / 16 - ready");
        model.FlourishProgress.Should().Be(1f);
    }

    [Test]
    public void Footwork_QualityTierChangeRecolorsTheSegmentsButAPercentChangeTouchesOneLabel()
    {
        var model = new PerformanceFootworkViewModel();
        var session = CreateSession(PerformanceKind.Dance);
        for (var beat = 0; beat < 10; beat++)
            HitAndAdvance(session);
        model.ShowBeat(session, PerformanceBeatOutcome.Hit);
        model.QualityDazzlingText.Should().Be("Dazzling 100%");
        var pushed = Track(model);

        // One miss in 11 recorded cues: 90%, which is Great.
        session.AdvanceBeat();
        model.ShowBeat(session, PerformanceBeatOutcome.Miss);
        pushed.Should().Contain(new[] { "QualityGreatText", "QualityGreatColor", "QualityDazzlingColor" });
        model.QualityGreatText.Should().Be("Great 90%");

        pushed.Clear();
        HitAndAdvance(session);
        model.ShowBeat(session, PerformanceBeatOutcome.Hit);
        pushed.Where(name => name.StartsWith("Quality")).Should().Equal("QualityGreatText");
        model.QualityGreatText.Should().Be("Great 91%");
    }

    [Test]
    public void Session_FirstBeatShowsTheLanesAndAnUnchangedBeatPushesNothing()
    {
        var model = new PerformanceSessionViewModel();
        var pushed = Track(model);
        var session = CreateSession(PerformanceKind.Music);

        model.ShowBeat(session, PerformanceBeatOutcome.Hit);

        pushed.Should().Contain(new[] { "LowLane", "MidLane", "HighLane", "LowStrike", "MidStrike", "HighStrike" });
        model.LowLane.Count.Should().Be(PerformanceSessionViewModel.UpcomingRows);

        pushed.Clear();
        model.ShowBeat(session, PerformanceBeatOutcome.Hit);
        pushed.Should().BeEmpty();
    }

    [Test]
    public void Session_LanesListTheUpcomingNotesWithTheNextOneNextToTheStrikeRow()
    {
        var model = new PerformanceSessionViewModel();
        var session = CreateSession(PerformanceKind.Music);
        var upcoming = new PerformanceBeat[PerformanceSession.VisibleBeatCount - 1];
        session.CopyUpcomingTo(upcoming);

        model.ShowBeat(session, PerformanceBeatOutcome.Hit);

        var lanes = new[] { model.LowLane, model.MidLane, model.HighLane };
        var cues = new[] { PerformanceCue.Low, PerformanceCue.Mid, PerformanceCue.High };
        var strikes = new[] { model.LowStrike, model.MidStrike, model.HighStrike };
        for (var lane = 0; lane < 3; lane++)
        {
            var icon = PerformanceCueIcons.GetIcon(cues[lane]);
            strikes[lane].Should().Be(session.Current.Requires(cues[lane]) ? icon : string.Empty);
            for (var row = 0; row < upcoming.Length; row++)
            {
                var beat = upcoming[upcoming.Length - 1 - row];
                lanes[lane][row].Should().Be(beat.Requires(cues[lane]) ? icon : string.Empty);
            }
        }
    }

    [Test]
    public void Session_NextBeat_PushesOnlyLanesStrikesAndReadouts()
    {
        var model = new PerformanceSessionViewModel();
        var session = CreateSession(PerformanceKind.Music);
        model.ShowBeat(session, PerformanceBeatOutcome.Hit);
        var pushed = Track(model);

        HitAndAdvance(session);
        model.ShowBeat(session, PerformanceBeatOutcome.Hit);

        pushed.Should().OnlyContain(name =>
            name.EndsWith("Lane") || name.EndsWith("Strike") ||
            name == "StreakText" || name == "FlourishText" || name == "FlourishProgress");
        pushed.Should().NotContain("BeatProgress", "the Session window has no beat bar");
        pushed.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void Session_HasNoBeatBar_SoStepsPushNothing()
    {
        var model = new PerformanceSessionViewModel();
        var pushed = Track(model);

        model.ShowBeatProgress(0.5f);

        model.WantsBeatProgress.Should().BeFalse();
        pushed.Should().BeEmpty();
    }

    [Test]
    public void Session_BandLineAndSongArePushedOnlyWhenTheyChange()
    {
        var model = new PerformanceSessionViewModel();
        var setup = new PerformerSetup
        {
            InstrumentText = "Mandoviol",
            SongText = "Cantina Pazaak",
            SetText = "Steady Hands I",
            TempoText = "Lively 1.2s"
        };
        var audience = new PerformerAudienceInfo
        {
            VenueText = "2 / 5",
            FollowingText = "1 of 1 in range",
            InspiringText = "Inspiring 1 follower",
            BandText = "You lead the band.",
            AudienceHearsText = "Audience hears: Cantina Pazaak"
        };
        model.ShowSetup(setup);
        model.ShowAudience(audience);
        var pushed = Track(model);

        model.ShowSetup(setup);
        model.ShowAudience(audience);
        pushed.Should().BeEmpty();

        model.ShowSetup(setup with { SongText = "Another Song" });
        pushed.Should().Equal("SongText");
    }

    [Test]
    public void Watching_SameViewAgain_PushesNothing()
    {
        var model = new PerformanceWatchingViewModel();
        model.ShowView(BuildView(banked: "46 / 108 min"));
        var pushed = Track(model);

        model.ShowView(BuildView(banked: "46 / 108 min"));

        pushed.Should().BeEmpty();
    }

    [Test]
    public void Watching_ATickThatOnlyChangesTheBankedTime_PushesOnlyTheBankedValues()
    {
        var model = new PerformanceWatchingViewModel();
        model.ShowView(BuildView(banked: "46 / 108 min", fraction: 0.43f));
        var pushed = Track(model);

        model.ShowView(BuildView(banked: "55 / 108 min", fraction: 0.51f));

        pushed.Should().BeEquivalentTo(new[] { "BankedFraction", "BankedText" });
    }

    [Test]
    public void Watching_ChangedRows_ReplaceTheListAndMarkTheFollowedPerformer()
    {
        var model = new PerformanceWatchingViewModel();
        model.ShowView(BuildView(followed: 1u));
        model.FollowTexts.Should().Equal("Following", "Follow");
        model.FollowEnabled.Should().Equal(false, true);
        model.RowTexts[0].Should().Be("Tala Venn - Power Step - Great");
        model.RowColors[0].Should().BeSameAs(PerformanceWindowColors.Gold);
        var pushed = Track(model);

        model.ShowView(BuildView(followed: 2u));

        pushed.Should().BeEquivalentTo(new[] { "RowTexts", "RowColors", "FollowTexts", "FollowEnabled" });
        model.FollowTexts.Should().Equal("Follow", "Following");
    }

    private static PerformanceWatchView BuildView(string banked = "0 / 60 min", float fraction = 0f, uint followed = 1u)
    {
        return new PerformanceWatchView
        {
            HeaderText = "2 performers in range - Troupe +10%",
            Rows = new[]
            {
                new PerformanceWatchRow(1u, "Tala Venn", "Power Step - Great", followed == 1u),
                new PerformanceWatchRow(2u, "Kira Sol", "Quickstep - Steady", followed == 2u)
            },
            BankedFraction = fraction,
            BankedText = banked,
            DebtText = "No XP debt",
            LeaveWithText = "+7% skill XP"
        };
    }
}
