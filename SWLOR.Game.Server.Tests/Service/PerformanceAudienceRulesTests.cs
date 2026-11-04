using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerformanceService;
using static SWLOR.Game.Server.Tests.Service.PerformanceTestSnapshots;

namespace SWLOR.Game.Server.Tests.Service;

public class PerformanceAudienceRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

    [TestCase(0, 0)]
    [TestCase(6, 8)]
    [TestCase(12, 15)]
    [TestCase(18, 22)]
    [TestCase(24, 30)]
    public void XpBonusMaximum_ReadsTheRankFromTheGrantedStat(int baseBonus, int expectedMaximum)
    {
        PerformanceAudienceRules.XpBonusMaximum(baseBonus).Should().Be(expectedMaximum);
    }

    [Test]
    public void Range_IsTwentyMetersPlusResonance()
    {
        PerformanceAudienceRules.GetRange(0).Should().Be(20f);
        PerformanceAudienceRules.GetRange(6).Should().Be(26f);
        PerformanceAudienceRules.GetRange(-4).Should().Be(20f);
    }

    [Test]
    public void IsLiveShow_NeedsAtLeastOneWatcher()
    {
        PerformanceAudienceRules.IsLiveShow(0).Should().BeFalse();
        PerformanceAudienceRules.IsLiveShow(1).Should().BeTrue();
    }

    [Test]
    public void PerformerXP_IsPracticeUntilAWatcherFollowsThem()
    {
        var practice = PerformanceMath.PerformerXP(120, PerformanceQuality.Steady, 0, PerformanceTempo.Relaxed);
        var oneWatcher = PerformanceMath.PerformerXP(120, PerformanceQuality.Steady, 1, PerformanceTempo.Relaxed);

        practice.Should().Be(30);
        oneWatcher.Should().Be(120);
    }

    [TestCase(false, 2)]
    [TestCase(true, 5)]
    public void FlourishBaseMinutes_AreTwoOrFiveWithTheCapstone(bool capstone, int expected)
    {
        PerformanceAudienceRules.FlourishBaseMinutes(capstone).Should().Be(expected);
    }

    [TestCase(0.0, true)]
    [TestCase(1.5, true)]
    [TestCase(2.0, true)]
    [TestCase(2.1, false)]
    [TestCase(30.0, false)]
    public void AreFlourishesSynchronized_MeansWithinTwoSeconds(double secondsApart, bool expected)
    {
        PerformanceAudienceRules.AreFlourishesSynchronized(Now, Now.AddSeconds(-secondsApart)).Should().Be(expected);
    }

    [Test]
    public void FlourishMinutes_PaysTheBaseAmountToAWatcherWhoHasNotBankedOne()
    {
        PerformanceAudienceRules.FlourishMinutesToBank(2, false, null, 0.0, Now).Should().Be(2.0);
        PerformanceAudienceRules.FlourishMinutesToBank(5, false, null, 0.0, Now).Should().Be(5.0);
    }

    [Test]
    public void FlourishMinutes_AWatcherBanksOneFlourishPerTwoMinutes()
    {
        var banked = Now.AddSeconds(-90);

        PerformanceAudienceRules.FlourishMinutesToBank(2, false, banked, 2.0, Now).Should().Be(0.0);
        PerformanceAudienceRules.FlourishMinutesToBank(2, false, Now.AddSeconds(-119), 2.0, Now).Should().Be(0.0);
        PerformanceAudienceRules.FlourishMinutesToBank(2, false, Now.AddSeconds(-120), 2.0, Now).Should().Be(2.0);
    }

    [Test]
    public void FlourishMinutes_ASynchronizedPairDoublesTheMinutes()
    {
        PerformanceAudienceRules.FlourishMinutesToBank(2, true, null, 0.0, Now).Should().Be(4.0);
        PerformanceAudienceRules.FlourishMinutesToBank(5, true, null, 0.0, Now).Should().Be(10.0);
    }

    [Test]
    public void FlourishMinutes_TheSecondLegOfAPairTopsAWatcherUpToTheDoubledAmount()
    {
        // The first performer's Flourish paid 2; the second lands a second later and the pair doubles to 4.
        var first = Now.AddSeconds(-1);

        PerformanceAudienceRules.FlourishMinutesToBank(2, true, first, 2.0, Now).Should().Be(2.0);
        // A watcher already topped up gets nothing more.
        PerformanceAudienceRules.FlourishMinutesToBank(2, true, first, 4.0, Now).Should().Be(0.0);
        // Outside the two-second window the earlier bank blocks a new one.
        PerformanceAudienceRules.FlourishMinutesToBank(2, true, Now.AddSeconds(-10), 2.0, Now).Should().Be(0.0);
    }

    [Test]
    public void FlourishBankedTotal_TracksTheGroupThatPaidTheWatcher()
    {
        PerformanceAudienceRules.FlourishBankedTotal(null, 0.0, 2.0, Now).Should().Be(2.0);
        PerformanceAudienceRules.FlourishBankedTotal(Now.AddSeconds(-1), 2.0, 2.0, Now).Should().Be(4.0);
        PerformanceAudienceRules.FlourishBankedTotal(Now.AddMinutes(-5), 4.0, 2.0, Now).Should().Be(2.0);
    }

    [TestCase(100, 80, 100, true)]
    [TestCase(100, 75, 100, false)]
    [TestCase(100, 76, 100, true)]
    [TestCase(100, 10, 100, false)]
    [TestCase(100, 100, 100, false)]
    [TestCase(100, 150, 100, false)]
    [TestCase(100, 0, 100, false)]
    [TestCase(50, 30, 0, false)]
    public void StandingOvationTriggers_WhenASurvivedHitLeavesTheHolderBelowAQuarter(int current, int damage, int max, bool expected)
    {
        // 25 of 100 left is exactly a quarter, which is not below it; lethal hits never fire it.
        PerformanceAudienceRules.StandingOvationTriggers(current, damage, max).Should().Be(expected);
    }

    [Test]
    public void StandingOvation_HealsTenPercentOfMaximumHpOncePerTenMinutes()
    {
        PerformanceAudienceRules.StandingOvationHeal(500).Should().Be(50);
        PerformanceAudienceRules.StandingOvationHeal(5).Should().Be(1);

        PerformanceAudienceRules.IsStandingOvationReady(null, Now).Should().BeTrue();
        PerformanceAudienceRules.IsStandingOvationReady(Now.AddMinutes(-9), Now).Should().BeFalse();
        PerformanceAudienceRules.IsStandingOvationReady(Now.AddMinutes(-10), Now).Should().BeTrue();
    }

    [Test]
    public void FrozenRemaining_PutsTheTimeBackToWhatTheLastTickSet()
    {
        // 55 minutes were set a minute ago; a minute of countdown has since run.
        PerformanceAudienceRules.FrozenRemainingSeconds(3240, true, 3300).Should().Be(3300);
        // Without a stored value, or after the effect was replaced, the real time stands.
        PerformanceAudienceRules.FrozenRemainingSeconds(3240, false, 3300).Should().Be(3240);
        PerformanceAudienceRules.FrozenRemainingSeconds(0, false, 3300).Should().Be(0);
        // Time added by something else is never taken away.
        PerformanceAudienceRules.FrozenRemainingSeconds(4000, true, 3300).Should().Be(4000);
    }

    [Test]
    public void BankedRemaining_AddsMinutesUpToTheCap()
    {
        PerformanceAudienceRules.BankedRemainingSeconds(50 * 60, 7, 108).Should().Be(57 * 60);
        PerformanceAudienceRules.BankedRemainingSeconds(105 * 60, 7, 108).Should().Be(108 * 60);
    }

    [Test]
    public void BankedRemaining_ALowerCapNeverTakesBankedTimeAway()
    {
        // 100 minutes banked in a venue-4 cantina, then watching in the open world where the cap is 60.
        PerformanceAudienceRules.BankedRemainingSeconds(100 * 60, 4, 60).Should().Be(100 * 60);
    }

    [TestCase(0, false, 0)]
    [TestCase(3, false, 3)]
    [TestCase(3, true, 4)]
    [TestCase(5, true, 5)]
    [TestCase(0, true, 1)]
    [TestCase(-2, false, 0)]
    [TestCase(9, false, 5)]
    public void VenueRating_IsTheAreaRatingPlusADanceFloorPointHeldToZeroThroughFive(int area, bool danceFloor, int expected)
    {
        PerformanceVenue.Combine(area, danceFloor).Should().Be(expected);
    }

    [Test]
    public void InRange_UsesTheSquaredDistanceAndEachPerformersOwnRange()
    {
        var near = Dancer(1, x: 0f);
        var far = Dancer(2, x: 25f);

        PerformanceAudienceRules.IsWithinRange(near.Position, far.Position, 25f).Should().BeTrue();
        PerformanceAudienceRules.IsWithinRange(near.Position, far.Position, 24.9f).Should().BeFalse();
        PerformanceAudienceRules.AreInEachOthersRange(near, far).Should().BeFalse();
    }
}
