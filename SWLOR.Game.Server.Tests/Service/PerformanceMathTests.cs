using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Tests.Service;

public class PerformanceMathTests
{
    [TestCase(0.00, PerformanceQuality.Rough)]
    [TestCase(0.49, PerformanceQuality.Rough)]
    [TestCase(0.50, PerformanceQuality.Steady)]
    [TestCase(0.79, PerformanceQuality.Steady)]
    [TestCase(0.80, PerformanceQuality.Great)]
    [TestCase(0.94, PerformanceQuality.Great)]
    [TestCase(0.95, PerformanceQuality.Dazzling)]
    [TestCase(1.00, PerformanceQuality.Dazzling)]
    public void QualityFromAccuracy_UsesThresholdsAtTheirBoundaries(double accuracy, PerformanceQuality expected)
    {
        PerformanceMath.QualityFromAccuracy(accuracy).Should().Be(expected);
    }

    [TestCase(49, PerformanceQuality.Rough)]
    [TestCase(50, PerformanceQuality.Steady)]
    [TestCase(79, PerformanceQuality.Steady)]
    [TestCase(80, PerformanceQuality.Great)]
    [TestCase(94, PerformanceQuality.Great)]
    [TestCase(95, PerformanceQuality.Dazzling)]
    public void QualityFromRecord_UsesPercentBoundariesExactly(int hitsOutOfHundred, PerformanceQuality expected)
    {
        PerformanceMath.QualityFromRecord(hitsOutOfHundred, 100).Should().Be(expected);
    }

    [TestCase(0, 0)]
    [TestCase(0, 4)]
    public void QualityFromRecord_StaysSteadyUntilFiveCuesAreRecorded(int hits, int recorded)
    {
        PerformanceMath.QualityFromRecord(hits, recorded).Should().Be(PerformanceQuality.Steady);
    }

    [Test]
    public void QualityFromRecord_JudgesAfterFiveCues()
    {
        PerformanceMath.QualityFromRecord(0, 5).Should().Be(PerformanceQuality.Rough);
        PerformanceMath.QualityFromRecord(5, 5).Should().Be(PerformanceQuality.Dazzling);
    }

    [Test]
    public void QualityMultipliers_MatchTheDesign()
    {
        PerformanceQuality.Rough.GetBankingMultiplier().Should().Be(0.5);
        PerformanceQuality.Steady.GetBankingMultiplier().Should().Be(1.0);
        PerformanceQuality.Great.GetBankingMultiplier().Should().Be(1.25);
        PerformanceQuality.Dazzling.GetBankingMultiplier().Should().Be(1.5);
    }

    [Test]
    public void Tempos_MatchTheDesign()
    {
        PerformanceTempo.Relaxed.GetBeatSeconds().Should().Be(1.5);
        PerformanceTempo.Lively.GetBeatSeconds().Should().Be(1.2);
        PerformanceTempo.Upbeat.GetBeatSeconds().Should().Be(1.0);
        PerformanceTempo.Relaxed.GetXPMultiplier().Should().Be(1.0);
        PerformanceTempo.Lively.GetXPMultiplier().Should().Be(1.15);
        PerformanceTempo.Upbeat.GetXPMultiplier().Should().Be(1.3);
        PerformanceTempo.Relaxed.Next().Should().Be(PerformanceTempo.Lively);
        PerformanceTempo.Lively.Next().Should().Be(PerformanceTempo.Upbeat);
        PerformanceTempo.Upbeat.Next().Should().Be(PerformanceTempo.Relaxed);
    }

    [Test]
    public void BankingMinutes_VenueFourGreatBanksSevenPerMinute()
    {
        var rate = PerformanceMath.BankingMinutes(PerformanceQuality.Great.GetBankingMultiplier(), 4, 1, 0, 0);

        rate.Should().BeApproximately(7.0, 1e-9);
        PerformanceMath.InspiredCapMinutes(4, 0).Should().Be(108);
    }

    [Test]
    public void BankingMinutes_OpenWorldSteadyBanksFourPerMinute()
    {
        var rate = PerformanceMath.BankingMinutes(PerformanceQuality.Steady.GetBankingMultiplier(), 0, 1, 0, 0);

        rate.Should().BeApproximately(4.0, 1e-9);
        PerformanceMath.InspiredCapMinutes(0, 0).Should().Be(60);
    }

    [TestCase(1, 4.0)]
    [TestCase(2, 4.4)]
    [TestCase(3, 4.8)]
    [TestCase(4, 5.2)]
    [TestCase(9, 5.2)]
    public void BankingMinutes_TroupeBonusIsTenPercentPerExtraPerformerCappedAtThirty(int troupeCount, double expected)
    {
        PerformanceMath.BankingMinutes(1.0, 0, troupeCount, 0, 0).Should().BeApproximately(expected, 1e-9);
    }

    [Test]
    public void BankingMinutes_AddsCrowdPleaserAndHarmonyToTheSameBonusPool()
    {
        PerformanceMath.BankingMinutes(1.0, 0, 1, 20, 10).Should().BeApproximately(5.2, 1e-9);
        PerformanceMath.BankingMinutes(1.0, 2, 3, 10, 0).Should().BeApproximately(4.0 * (1 + 0.2 + 0.2 + 0.1), 1e-9);
    }

    [Test]
    public void VenueRating_IsClampedToTheFivePointScale()
    {
        PerformanceMath.BankingMinutes(1.0, 9, 1, 0, 0).Should().BeApproximately(6.0, 1e-9);
        PerformanceMath.BankingMinutes(1.0, -3, 1, 0, 0).Should().BeApproximately(4.0, 1e-9);
        PerformanceMath.InspiredCapMinutes(9, 0).Should().Be(120);
    }

    [Test]
    public void InspiredCapMinutes_AddsEncoreMinutes()
    {
        PerformanceMath.InspiredCapMinutes(4, 60).Should().Be(168);
        PerformanceMath.InspiredCapMinutes(0, 20).Should().Be(80);
    }

    [Test]
    public void AddInspiredMinutes_NeverExceedsTheCap()
    {
        PerformanceMath.AddInspiredMinutes(50, 7, 108).Should().Be(57);
        PerformanceMath.AddInspiredMinutes(105, 7, 108).Should().Be(108);
    }

    [Test]
    public void WarmupPayout_BanksThreeMinutesAtOnce()
    {
        PerformanceMath.WarmupPayout(7.0).Should().BeApproximately(21.0, 1e-9);
        PerformanceMath.WarmupPayout(4.0).Should().BeApproximately(12.0, 1e-9);
    }

    [Test]
    public void DebtCleared_FivePercentPerMinuteLeavesAboutSixtyPercentAfterTenMinutes()
    {
        var debt = 10000;
        for (var minute = 0; minute < 10; minute++)
            debt -= PerformanceMath.DebtCleared(debt, 5);

        debt.Should().BeInRange(5987, 6100);
    }

    [Test]
    public void DebtCleared_RoundsDownClearsAtLeastOneAndNeverExceedsTheDebt()
    {
        PerformanceMath.DebtCleared(1000, 5).Should().Be(50);
        PerformanceMath.DebtCleared(19, 4).Should().Be(1);
        PerformanceMath.DebtCleared(1, 1).Should().Be(1);
        PerformanceMath.DebtCleared(100, 150).Should().Be(100);
        PerformanceMath.DebtCleared(0, 5).Should().Be(0);
        PerformanceMath.DebtCleared(500, 0).Should().Be(0);
        PerformanceMath.DebtCleared(int.MaxValue, 100).Should().Be(int.MaxValue);
    }

    [TestCase(0, 1.0)]
    [TestCase(1, 1.0)]
    [TestCase(2, 1.25)]
    [TestCase(3, 1.5)]
    [TestCase(5, 2.0)]
    [TestCase(40, 2.0)]
    public void CrowdFactor_GrowsPerExtraWatcherAndCapsAtTwo(int crowd, double expected)
    {
        PerformanceMath.CrowdFactor(crowd).Should().Be(expected);
    }

    [Test]
    public void PerformerXP_ScalesByQualityCrowdAndTempo()
    {
        PerformanceMath.PerformerXP(120, PerformanceQuality.Steady, 1, PerformanceTempo.Relaxed).Should().Be(120);
        PerformanceMath.PerformerXP(120, PerformanceQuality.Steady, 1, PerformanceTempo.Lively).Should().Be(138);
        PerformanceMath.PerformerXP(120, PerformanceQuality.Great, 5, PerformanceTempo.Upbeat).Should().Be(390);
        PerformanceMath.PerformerXP(120, PerformanceQuality.Rough, 1, PerformanceTempo.Relaxed).Should().Be(60);
    }

    [Test]
    public void PerformerXP_WithoutWatchersIsPracticeAtAQuarterRate()
    {
        PerformanceMath.PerformerXP(120, PerformanceQuality.Steady, 0, PerformanceTempo.Relaxed).Should().Be(30);
    }

    [Test]
    public void HornRests_CountAsNeitherHitNorMiss()
    {
        var session = new PerformanceSession(
            1u, PerformanceKind.Music, PerformanceInstrumentFamily.Horns, PerformanceTempo.Relaxed,
            DateTime.UtcNow, new Random(7));

        var restsSeen = 0;
        for (var beat = 0; beat < 200; beat++)
        {
            if (session.Current.IsRest)
            {
                var recorded = session.RecordedCount;
                var hits = session.RecentHits;
                var streak = session.Streak;
                var meter = session.FlourishMeter;

                session.RegisterCue(PerformanceCue.Mid, DateTime.UtcNow).Should().Be(PerformanceInputResult.Ignored);
                session.AdvanceBeat().Should().Be(PerformanceBeatOutcome.Rest);

                session.RecordedCount.Should().Be(recorded);
                session.RecentHits.Should().Be(hits);
                session.Streak.Should().Be(streak);
                session.FlourishMeter.Should().Be(meter);
                restsSeen++;
                continue;
            }

            session.RegisterCue(session.Current.Primary, DateTime.UtcNow);
            session.AdvanceBeat().Should().Be(PerformanceBeatOutcome.Hit);
        }

        restsSeen.Should().BeGreaterThan(0);
        session.Quality.Should().Be(PerformanceQuality.Dazzling);
    }
}
