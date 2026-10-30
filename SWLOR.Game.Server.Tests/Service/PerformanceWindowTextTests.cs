using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>The wording and numbers the four Performance windows show. All of it is pure, so no engine is needed.</summary>
public class PerformanceWindowTextTests
{
    [TestCase(PerformanceCue.Left, 1, "ife_pf_stepl")]
    [TestCase(PerformanceCue.Up, 2, "ife_pf_stepu")]
    [TestCase(PerformanceCue.Down, 3, "ife_pf_stepd")]
    [TestCase(PerformanceCue.Right, 4, "ife_pf_stepr")]
    [TestCase(PerformanceCue.Low, 1, "ife_pf_notelo")]
    [TestCase(PerformanceCue.Mid, 2, "ife_pf_notemid")]
    [TestCase(PerformanceCue.High, 3, "ife_pf_notehi")]
    public void Cue_ShowsTheHotbarButtonIconAndSlot(PerformanceCue cue, int slot, string icon)
    {
        PerformanceCueIcons.GetIcon(cue).Should().Be(icon);
        PerformanceCueIcons.GetHotbarSlot(cue).Should().Be(slot);
        PerformanceCueIcons.GetSlotText(cue).Should().Be(slot.ToString());
    }

    [Test]
    public void CueSlots_FollowTheLaneOrderTheHotbarIsBuiltFrom()
    {
        foreach (var kind in new[] { PerformanceKind.Dance, PerformanceKind.Music })
        {
            var lanes = PerformanceCueGenerator.GetLanes(kind);
            for (var index = 0; index < lanes.Count; index++)
                PerformanceCueIcons.GetHotbarSlot(lanes[index]).Should().Be(index + 1);
        }
    }

    [Test]
    public void StayMinutes_RoundsUpToFillTheCap()
    {
        PerformanceWatchText.StayMinutes(108, 46, 9.0).Should().Be(7);
        PerformanceWatchText.StayMinutes(108, 108, 9.0).Should().Be(0);
        PerformanceWatchText.StayMinutes(108, 46, 0.0).Should().Be(0);
    }

    [Test]
    public void Banked_QuotesTheCapTheRateAndHowLongToStay()
    {
        var text = PerformanceWatchText.Banked(46.4, 108, 9.0, isPaying: true, warmupMinutesLeft: 0);

        text.Should().StartWith("46 / 108 min  (+9 min per minute)");
        text.Should().Contain("Stay about 7 more minutes to fill it. Your timer is paused while you watch.");
    }

    [Test]
    public void Banked_DuringTheWarmupSaysHowManyMinutesAreLeft()
    {
        PerformanceWatchText.Banked(0, 60, 0.0, isPaying: false, warmupMinutesLeft: 2)
            .Should().Contain("watch 2 more minutes");
        PerformanceWatchText.Banked(0, 60, 0.0, isPaying: false, warmupMinutesLeft: 1)
            .Should().Contain("watch 1 more minute and");
    }

    [Test]
    public void Banked_AFullBankSaysSo()
    {
        PerformanceWatchText.Banked(108, 108, 9.0, isPaying: true, warmupMinutesLeft: 0)
            .Should().Contain("Your Inspired is full.");
    }

    [Test]
    public void BankedFraction_IsHeldBetweenZeroAndOne()
    {
        PerformanceWatchText.BankedFraction(54, 108).Should().BeApproximately(0.5f, 0.001f);
        PerformanceWatchText.BankedFraction(200, 108).Should().Be(1f);
        PerformanceWatchText.BankedFraction(10, 0).Should().Be(0f);
    }

    [Test]
    public void Debt_ShowsTheDebtTheRateAndLastMinutesClearing()
    {
        PerformanceWatchText.Debt(11780, 5, 620).Should().Be("11,780  (-5% per minute, 620 cleared last minute)");
        PerformanceWatchText.Debt(0, 5, 0).Should().Be("No XP debt");
        PerformanceWatchText.Debt(-1, 5, 0).Should().Be("XP debt unavailable");
    }

    [Test]
    public void LeaveWith_ListsTheXpThenEveryRider()
    {
        var data = new InspiredEffectData
        {
            XPBonusPercent = 37,
            Might = 1,
            CraftBonus = 2,
            OutOfCombatMovementSpeedPercent = 10
        };

        PerformanceWatchText.LeaveWith(data).Should().Be(
            "+37% skill XP\n+10% move speed out of combat; +1 MGT; +2 Control, +2 Craftsmanship");
        PerformanceWatchText.LeaveWith(new InspiredEffectData { XPBonusPercent = 7 }).Should().Be("+7% skill XP");
    }

    [Test]
    public void Header_NamesTheTroupeBonusOnlyWhenThereIsOne()
    {
        PerformanceWatchText.Header(3, 0.2).Should().Be("3 performers in range - Troupe +20%");
        PerformanceWatchText.Header(1, 0.0).Should().Be("1 performer in range");
    }

    [Test]
    public void Band_NamesTheLeaderTheDancersAndTheBonuses()
    {
        PerformanceAudienceText.Band(true, true, string.Empty, new[] { "Tala Venn", "Kira Sol" }, 10, 3)
            .Should().Be("You lead the band. Dancing in range: Tala Venn, Kira Sol. Harmony +10%, Troupe of 3 +20%.");
        PerformanceAudienceText.Band(false, true, "Oro Dask", Array.Empty<string>(), 0, 1)
            .Should().Be("You play in Oro Dask's band. No one is dancing in range.");
        PerformanceAudienceText.Band(true, false, string.Empty, Array.Empty<string>(), 0, 1)
            .Should().Be("You are playing on your own. No one is dancing in range.");
    }

    [Test]
    public void AudienceLines_CountFollowersOfWatchersInRange()
    {
        PerformanceAudienceText.Following(2, 3).Should().Be("2 of 3 in range");
        PerformanceAudienceText.Inspiring(2, 3).Should().Be("Inspiring 2 followers");
        PerformanceAudienceText.Inspiring(1, 1).Should().Be("Inspiring 1 follower");
        PerformanceAudienceText.Inspiring(0, 3).Should().Be("Nobody is following you yet");
        PerformanceAudienceText.Inspiring(0, 0).Should().Be("Practice: nobody in range");
        PerformanceAudienceText.Venue(9).Should().Be("5 / 5");
        PerformanceAudienceText.Hears("Cantina Pazaak").Should().Be("Audience hears: Cantina Pazaak");
        PerformanceAudienceText.Hears(string.Empty).Should().BeEmpty();
    }

    [Test]
    public void RiderText_NamesRoutinesAndSetsWithTheirRiders()
    {
        PerformanceRiderText.GetName(PerformanceRoutineType.LightFeet, 1).Should().Be("Light Feet I");
        PerformanceRiderText.GetName(PerformanceRoutineType.PowerStep, 1).Should().Be("Power Step");
        PerformanceRiderText.GetName(PerformanceSetType.SteadyHands, 2).Should().Be("Steady Hands II");
        PerformanceRiderText.GetName(PerformanceSetType.AnglersLullaby).Should().Be("Angler's Lullaby");

        PerformanceRiderText.GetRider(PerformanceRoutineType.CantinaShuffle, 0, 0).Should().Be("XP and debt only");
        PerformanceRiderText.GetRider(PerformanceRoutineType.LightFeet, 10, 0).Should().Be("+10% move speed");
        PerformanceRiderText.GetRider(PerformanceRoutineType.PowerStep, 0, 0).Should().Be("+1 MGT");
        PerformanceRiderText.GetRider(PerformanceRoutineType.VigorReel, 0, 1).Should().Be("+2 VIT");
        PerformanceRiderText.GetRider(PerformanceSetType.SteadyHands, 2, 0, 0).Should().Be("+2 Control, +2 Craftsmanship");
        PerformanceRiderText.GetRider(PerformanceSetType.SilverTongue, 0, 0, 1).Should().Be("+2 SOC");
        PerformanceRiderText.GetRider(PerformanceSetType.AnglersLullaby, 0, 10, 0).Should().Be("+10% fishing");
        PerformanceRiderText.GetLockedText(25).Should().Be("Performance 25");
    }

    [Test]
    public void TempoLabel_ShowsTheBeatLength()
    {
        PerformanceTempo.Relaxed.GetLabel().Should().Be("Relaxed 1.5s");
        PerformanceTempo.Lively.GetLabel().Should().Be("Lively 1.2s");
        PerformanceTempo.Upbeat.GetLabel().Should().Be("Upbeat 1.0s");
    }

    [Test]
    public void WatchView_ListsDancersFirstAndMarksTheFollowedPerformers()
    {
        var dancer = PerformanceTestSnapshots.Dancer(
            1u, xpScaled: 12, routine: PerformanceRoutineType.PowerStep, quality: PerformanceQuality.Great, debtClearPercent: 3);
        var otherDancer = PerformanceTestSnapshots.Dancer(2u, xpScaled: 6, routine: PerformanceRoutineType.Quickstep);
        var musician = PerformanceTestSnapshots.Musician(3u, xpScaled: 6, set: PerformanceSetType.SteadyHands, debtClearPercent: 2);
        var candidates = new[] { musician, otherDancer, dancer };
        var plan = PerformanceWatchPlan.Create(5, dancer, musician, 3);

        var view = PerformanceWatchViewBuilder.Build(
            candidates, dancer, musician, plan, 5, 46 * 60.0, 11780, 620,
            new InspiredEffectData { XPBonusPercent = 20 }, performer => $"P{performer}");

        view.Rows.Select(row => row.Performer).Should().Equal(2u, 1u, 3u);
        view.Rows.Select(row => row.IsFollowed).Should().Equal(false, true, true);
        view.Rows[1].Name.Should().Be("P1");
        view.Rows[1].Detail.Should().Be("Power Step - Great");
        view.Rows[2].Detail.Should().Be("Steady Hands - Steady");
        view.HeaderText.Should().Be("3 performers in range - Troupe +20%");
        view.DebtText.Should().Be("11,780  (-5% per minute, 620 cleared last minute)");
        view.BankedText.Should().StartWith($"46 / {plan.CapMinutes} min");
        view.LeaveWithText.Should().Be("+20% skill XP");
    }

    [Test]
    public void WatchView_TheWarmupCompletingTickReportsTheRateNotTheThreeMinutePayout()
    {
        var dancer = PerformanceTestSnapshots.Dancer(1u);
        var warmupTick = PerformanceWatchPlan.Create(PerformanceMath.WarmupMinutes, dancer, null, 1);
        var steadyTick = PerformanceWatchPlan.Create(PerformanceMath.WarmupMinutes + 1, dancer, null, 1);

        warmupTick.WarmupCompletesNow.Should().BeTrue();
        warmupTick.BankedMinutes.Should().BeGreaterThan(steadyTick.BankedMinutes, "the warm-up pays three minutes at once");
        PerformanceWatchViewBuilder.GetRatePerMinute(warmupTick)
            .Should().BeApproximately(steadyTick.BankedMinutes, 0.0001);
        PerformanceWatchViewBuilder.GetRatePerMinute(PerformanceWatchPlan.Create(1, dancer, null, 1)).Should().Be(0.0);
    }

    [Test]
    public void AudienceState_ReusesNamesForAFewTicksThenLooksThemUpAgain()
    {
        var state = new PerformanceAudienceState(1u);
        var lookups = 0;
        string Lookup() => $"Name{++lookups}";

        state.GetName(5u, 1, Lookup).Should().Be("Name1");
        state.GetName(5u, 2, Lookup).Should().Be("Name1");
        state.GetName(5u, 1 + PerformanceAudienceState.NameLifetimeTicks, Lookup).Should().Be("Name2");
        lookups.Should().Be(2);
    }

    [Test]
    public void AudienceState_StopWatchingForgetsAClosedWindowAndTheLastTick()
    {
        var state = new PerformanceAudienceState(1u)
        {
            WatchingDismissed = true,
            WatchingArea = 9u,
            LastEntry = new PerformanceWatcherEntry(1u, System.Numerics.Vector3.Zero, new List<PerformerSnapshot>()),
            LastReport = new PerformanceWatcherReport()
        };

        state.StopWatching();

        state.WatchingDismissed.Should().BeFalse("a window closed for one show may open for the next");
        state.WatchingArea.Should().Be(0u);
        state.LastEntry.Should().BeNull();
        state.LastReport.Should().BeNull();
    }

    [Test]
    public void BindCache_ReportsAChangeOnlyOnce()
    {
        var shown = "a";

        PerformanceBindCache.Changed(ref shown, "a").Should().BeFalse();
        PerformanceBindCache.Changed(ref shown, "b").Should().BeTrue();
        PerformanceBindCache.Changed(ref shown, "b").Should().BeFalse();
        shown.Should().Be("b");
    }

    [Test]
    public void Preference_DefaultsToTheAutomaticSongAndHearingThePerformer()
    {
        var preference = new PerformancePreference();

        preference.SongTrackId.Should().Be(PerformanceSongs.AutomaticSong);
        preference.PlayPerformerSong.Should().BeTrue();
    }

    [Test]
    public void Session_OnlyDancesStepInsideABeat()
    {
        var dance = new PerformanceSession(
            1u, PerformanceKind.Dance, PerformanceInstrumentFamily.None, PerformanceTempo.Relaxed, DateTime.UtcNow, new Random(1));
        var music = new PerformanceSession(
            1u, PerformanceKind.Music, PerformanceInstrumentFamily.Strings, PerformanceTempo.Relaxed, DateTime.UtcNow, new Random(1));

        dance.StepsPerBeat.Should().Be(PerformanceSession.DanceStepsPerBeat);
        music.StepsPerBeat.Should().Be(1);
    }
}
