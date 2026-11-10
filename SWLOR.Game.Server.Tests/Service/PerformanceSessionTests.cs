using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Service;

public class PerformanceSessionTests
{
    private static readonly DateTime Start = new(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

    private static PerformanceSession CreateDance(int seed = 3)
    {
        return new PerformanceSession(
            1u, PerformanceKind.Dance, PerformanceInstrumentFamily.None, PerformanceTempo.Relaxed, Start, new Random(seed));
    }

    private static PerformanceCue WrongCueFor(PerformanceSession session)
    {
        return PerformanceCueGenerator.GetLanes(session.Kind).First(cue => !session.Current.Requires(cue));
    }

    [Test]
    public void NewSession_ShowsSixCuesAndStartsSteady()
    {
        var session = CreateDance();

        session.Upcoming.Count.Should().Be(PerformanceSession.VisibleBeatCount - 1);
        session.Current.IsRest.Should().BeFalse();
        session.Quality.Should().Be(PerformanceQuality.Steady);
        session.FlourishMeter.Should().Be(0);
    }

    [Test]
    public void CorrectPress_HitsOncePerBeatAndExtraPressesAreIgnored()
    {
        var session = CreateDance();
        var cue = session.Current.Primary;

        session.RegisterCue(cue, Start).Should().Be(PerformanceInputResult.Hit);
        session.RegisterCue(cue, Start).Should().Be(PerformanceInputResult.Ignored);
        session.RegisterCue(WrongCueFor(session), Start).Should().Be(PerformanceInputResult.Ignored);
        session.AdvanceBeat().Should().Be(PerformanceBeatOutcome.Hit);
        session.Streak.Should().Be(1);
        session.FlourishMeter.Should().Be(1);
    }

    [Test]
    public void WrongPress_MissesTheBeatEvenIfTheRightCueFollows()
    {
        var session = CreateDance();
        var right = session.Current.Primary;

        session.RegisterCue(WrongCueFor(session), Start).Should().Be(PerformanceInputResult.Miss);
        session.RegisterCue(right, Start).Should().Be(PerformanceInputResult.Ignored);
        session.AdvanceBeat().Should().Be(PerformanceBeatOutcome.Miss);
        session.Streak.Should().Be(0);
    }

    [Test]
    public void UnansweredBeat_IsAMissAndResetsTheStreak()
    {
        var session = CreateDance();
        session.RegisterCue(session.Current.Primary, Start);
        session.AdvanceBeat();
        session.Streak.Should().Be(1);

        session.AdvanceBeat().Should().Be(PerformanceBeatOutcome.Miss);
        session.Streak.Should().Be(0);
        session.BestStreak.Should().Be(1);
    }

    [Test]
    public void FlourishMeter_FillsAtSixteenHitsAndResetsWhenUsed()
    {
        var session = CreateDance();
        for (var beat = 0; beat < PerformanceMath.FlourishMeterMax + 4; beat++)
        {
            session.RegisterCue(session.Current.Primary, Start);
            session.AdvanceBeat();
        }

        session.FlourishMeter.Should().Be(PerformanceMath.FlourishMeterMax);
        session.TryConsumeFlourish(Start).Should().BeTrue();
        session.FlourishMeter.Should().Be(0);
        session.LastFlourishUtc.Should().Be(Start);
        session.TryConsumeFlourish(Start).Should().BeFalse();
    }

    [Test]
    public void Quality_UsesTheLastTwentyScoredCues()
    {
        var session = CreateDance();
        for (var beat = 0; beat < 20; beat++)
            session.AdvanceBeat();

        session.RecordedCount.Should().Be(PerformanceMath.QualityWindowSize);
        session.Quality.Should().Be(PerformanceQuality.Rough);

        for (var beat = 0; beat < 20; beat++)
        {
            session.RegisterCue(session.Current.Primary, Start);
            session.AdvanceBeat();
        }

        session.RecordedCount.Should().Be(PerformanceMath.QualityWindowSize);
        session.RecentHits.Should().Be(PerformanceMath.QualityWindowSize);
        session.Quality.Should().Be(PerformanceQuality.Dazzling);
    }

    [Test]
    public void Chord_NeedsBothLanesAndAWrongLaneMisses()
    {
        var session = new PerformanceSession(
            1u, PerformanceKind.Music, PerformanceInstrumentFamily.Strings, PerformanceTempo.Upbeat, Start, new Random(5));
        while (!session.Current.IsChord)
            session.AdvanceBeat();

        var chord = session.Current;
        session.RegisterCue(chord.Primary, Start).Should().Be(PerformanceInputResult.Partial);
        session.RegisterCue(chord.Primary, Start).Should().Be(PerformanceInputResult.Ignored);
        session.RegisterCue(chord.Second.Value, Start).Should().Be(PerformanceInputResult.Hit);
        session.AdvanceBeat().Should().Be(PerformanceBeatOutcome.Hit);

        while (!session.Current.IsChord)
            session.AdvanceBeat();

        session.RegisterCue(session.Current.Primary, Start).Should().Be(PerformanceInputResult.Partial);
        session.RegisterCue(WrongCueFor(session), Start).Should().Be(PerformanceInputResult.Miss);
        session.AdvanceBeat().Should().Be(PerformanceBeatOutcome.Miss);
    }

    [Test]
    public void IsIdle_AfterSixtySecondsWithoutInput()
    {
        var session = CreateDance();

        session.IsIdle(Start.AddSeconds(59.9)).Should().BeFalse();
        session.IsIdle(Start.AddSeconds(60)).Should().BeTrue();

        session.RegisterCue(session.Current.Primary, Start.AddSeconds(50));
        session.IsIdle(Start.AddSeconds(100)).Should().BeFalse();
    }

    [Test]
    public void AFlourishPressedBeforeTheMeterIsFullDoesNotCountAsInput()
    {
        var session = CreateDance();

        session.TryConsumeFlourish(Start.AddSeconds(50)).Should().BeFalse();

        session.IsIdle(Start.AddSeconds(60)).Should().BeTrue();
    }

    [Test]
    public void RoleplayMode_ScoresRoughIgnoresCuesAndNeverFlourishes()
    {
        var session = CreateDance();
        session.SetRoleplay(true, Start);

        session.Quality.Should().Be(PerformanceQuality.Rough);
        session.RegisterCue(session.Current.Primary, Start).Should().Be(PerformanceInputResult.Ignored);
        session.TryConsumeFlourish(Start).Should().BeFalse();
    }

    [Test]
    public void RoleplayMode_TalkingKeepsItGoingUntilTenQuietMinutes()
    {
        var session = CreateDance();
        session.SetRoleplay(true, Start);

        session.IsIdle(Start.AddMinutes(9)).Should().BeFalse("a quiet scene gets ten minutes, not one");
        session.NoteActivity(Start.AddMinutes(9));
        session.IsIdle(Start.AddMinutes(18)).Should().BeFalse();
        session.IsIdle(Start.AddMinutes(19)).Should().BeTrue();
    }

    [Test]
    public void ActivePlay_IgnoresTalkingForTheIdleTimer()
    {
        var session = CreateDance();

        session.NoteActivity(Start.AddSeconds(50));

        session.IsIdle(Start.AddSeconds(60)).Should().BeTrue("active play is kept going by cue presses");
    }

    [Test]
    public void SwitchingModes_RestartsTheIdleTimerAndKeepsTheActiveRecord()
    {
        var session = CreateDance();
        for (var beat = 0; beat < PerformanceMath.QualityMinimumRecorded; beat++)
        {
            if (!session.Current.IsRest)
                session.RegisterCue(session.Current.Primary, Start);
            if (session.Current.IsChord)
                session.RegisterCue(session.Current.Second!.Value, Start);
            session.AdvanceBeat();
        }

        var activeQuality = session.Quality;
        activeQuality.Should().Be(PerformanceQuality.Dazzling);

        session.SetRoleplay(true, Start.AddSeconds(55));
        session.Quality.Should().Be(PerformanceQuality.Rough);
        session.IsIdle(Start.AddSeconds(70)).Should().BeFalse();

        session.SetRoleplay(false, Start.AddMinutes(30));
        session.Quality.Should().Be(activeQuality);
        session.IsIdle(Start.AddMinutes(30).AddSeconds(59)).Should().BeFalse();
        session.IsIdle(Start.AddMinutes(31)).Should().BeTrue();
    }
}
