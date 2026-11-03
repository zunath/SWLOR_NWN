using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerformanceService;
using static SWLOR.Game.Server.Tests.Service.PerformanceTestSnapshots;

namespace SWLOR.Game.Server.Tests.Service;

public class PerformanceWatchPlanTests
{
    [TestCase(1)]
    [TestCase(2)]
    public void Create_BanksAndClearsNothingDuringTheThreeMinuteWarmup(int continuousMinutes)
    {
        var plan = PerformanceWatchPlan.Create(continuousMinutes, Dancer(1, debtClearPercent: 4), null, 1);

        plan.IsPaying.Should().BeFalse();
        plan.BankedMinutes.Should().Be(0.0);
        plan.DebtClearPercent.Should().Be(0);
    }

    [Test]
    public void Create_TheTickThatEndsTheWarmupBanksThreeMinutesWorthAtOnce()
    {
        var dancer = Dancer(1, venue: 4, quality: PerformanceQuality.Great);

        var warmup = PerformanceWatchPlan.Create(3, dancer, null, 1);
        var after = PerformanceWatchPlan.Create(4, dancer, null, 1);

        warmup.IsPaying.Should().BeTrue();
        warmup.WarmupCompletesNow.Should().BeTrue();
        warmup.BankedMinutes.Should().BeApproximately(21.0, 1e-9);
        after.WarmupCompletesNow.Should().BeFalse();
        after.BankedMinutes.Should().BeApproximately(7.0, 1e-9);
        after.CapMinutes.Should().Be(108);
    }

    [Test]
    public void Create_OpenWorldSteadyBanksFourMinutesAndCapsAtSixty()
    {
        var plan = PerformanceWatchPlan.Create(5, Dancer(1), null, 1);

        plan.BankedMinutes.Should().BeApproximately(4.0, 1e-9);
        plan.CapMinutes.Should().Be(60);
    }

    [Test]
    public void Create_UsesTheBetterVenueAndTheBetterQualityOfTheTwoFollowedPerformers()
    {
        var dancer = Dancer(1, venue: 1, quality: PerformanceQuality.Rough);
        var musician = Musician(2, venue: 3, quality: PerformanceQuality.Dazzling);

        var plan = PerformanceWatchPlan.Create(5, dancer, musician, 2);

        plan.Venue.Should().Be(3);
        plan.Quality.Should().Be(PerformanceQuality.Dazzling);
        // 4 x 1.5 x (1 + 0.3 venue + 0.1 troupe)
        plan.BankedMinutes.Should().BeApproximately(4.0 * 1.5 * 1.4, 1e-9);
    }

    [Test]
    public void Create_TroupeCountsEveryPerformerInRangeAndCapsAtThirtyPercent()
    {
        var dancer = Dancer(1);

        PerformanceWatchPlan.Create(5, dancer, null, 3).BankedMinutes.Should().BeApproximately(4.8, 1e-9);
        PerformanceWatchPlan.Create(5, dancer, null, 12).BankedMinutes.Should().BeApproximately(5.2, 1e-9);
    }

    [Test]
    public void Create_CrowdPleaserBelongsToTheDancerAndHarmonyNeedsADancerInTheMusiciansRange()
    {
        var dancer = Dancer(1, crowdPleaserPercent: 20);
        var musician = Musician(2, harmonyPercent: 10);

        PerformanceWatchPlan.Create(5, dancer, null, 1).BankedMinutes.Should().BeApproximately(4.8, 1e-9);
        PerformanceWatchPlan.Create(5, null, musician, 1).BankedMinutes.Should().BeApproximately(4.0, 1e-9);

        musician.HarmonyActive = true;
        PerformanceWatchPlan.Create(5, null, musician, 1).BankedMinutes.Should().BeApproximately(4.4, 1e-9);
        PerformanceWatchPlan.Create(5, dancer, musician, 1).BankedMinutes.Should().BeApproximately(4.0 * 1.3, 1e-9);
    }

    [Test]
    public void Create_DebtClearingAddsTheDancerAndMusicianPercentages()
    {
        var dancer = Dancer(1, debtClearPercent: 3);
        var musician = Musician(2, debtClearPercent: 4);

        PerformanceWatchPlan.Create(4, dancer, musician, 2).DebtClearPercent.Should().Be(7);
        PerformanceWatchPlan.Create(4, dancer, null, 1).DebtClearPercent.Should().Be(3);
        PerformanceWatchPlan.Create(4, null, musician, 1).DebtClearPercent.Should().Be(4);
    }

    [Test]
    public void Create_TheFollowedDancersEncoreRaisesTheCap()
    {
        var dancer = Dancer(1, venue: 2, capBonusMinutes: 40);

        PerformanceWatchPlan.Create(4, dancer, null, 1).CapMinutes.Should().Be(60 + 24 + 40);
    }

    [Test]
    public void Create_NobodyFollowedPaysNothing()
    {
        var plan = PerformanceWatchPlan.Create(9, null, null, 0);

        plan.IsPaying.Should().BeFalse();
        plan.BankedMinutes.Should().Be(0.0);
    }

    [Test]
    public void Create_ClearingDebtOverTenMinutesFollowsTheSumOfBothPercentages()
    {
        var plan = PerformanceWatchPlan.Create(4, Dancer(1, debtClearPercent: 3), Musician(2, debtClearPercent: 2), 2);
        var debt = 10000;

        for (var minute = 0; minute < 10; minute++)
            debt -= PerformanceMath.DebtCleared(debt, plan.DebtClearPercent);

        debt.Should().BeInRange(5987, 6100);
    }
}
