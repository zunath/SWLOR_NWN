using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Fishing;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.CraftSim.Tests.Fishing
{
    [TestFixture]
    public class FishingCatchRateGateTests
    {
        private static readonly FishingParityOptions Small = FishingParityOptions.Default with
        {
            ReferenceRanks = new[] { 30 },
            PooledRanks = new[] { 0, 30 },
            LowestDelta = -2,
            HighestDelta = 2
        };

        private static GateContext Context() => new(null, null, 30, 1, AttributeSweep.Default);

        [Test]
        public void TodaysCatchChance_IsTheOldFormula()
        {
            FishingCatchRateGate.Today(0, 10).Should().Be(60);
            FishingCatchRateGate.Today(0, 14).Should().Be(68);
            FishingCatchRateGate.Today(-2, 20).Should().Be(60);
            FishingCatchRateGate.Today(4, 20).Should().Be(95, "the chance stops at 95");
            FishingCatchRateGate.Today(-7, 8).Should().Be(0);
        }

        [Test]
        public void QuickReel_StaysInsideTodaysCorridor_AndTheTablesShowEveryRow()
        {
            var gate = new FishingCatchRateGate(new FishingParity(30, 1), Small);

            var result = gate.Evaluate(Context());

            result.Gate.Should().Be(GateNames.EconomyParity);
            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            result.Tables.Should().HaveCount(3);
            var byDelta = result.Tables[0];
            byDelta.Rows.Should().HaveCount(5);
            byDelta.Rows.Select(row => row[0]).Should().Equal("-2", "-1", "0", "+1", "+2");
            byDelta.Header.Should().Contain("Today, Willpower 8").And.Contain("Today, Willpower 20").And.Contain("Quick Reel (Willpower 12)");
            byDelta.Rows[2][2].Should().Be("60%", "today's chance at Willpower 10 and no rank difference (the second of seven columns)");
            byDelta.Rows[2].Should().Contain("64%", "Quick Reel at Willpower 12 and no rank difference");
            byDelta.Rows.Select(row => row.Last()).Should().OnlyContain(verdict => verdict == "Inside");
            result.Tables[1].Rows.Select(row => row[0]).Should().Equal("0", "30");
            result.Tables[1].Rows.Select(row => row.Last()).Should().OnlyContain(verdict => verdict == "Inside");
            result.Summary.Should().Contain("Willpower 12").And.Contain("pending the owner's attribute-median decision").And.Contain("1.00x");
        }

        [Test]
        public void QuickReel_LandsTheSameFishAndPaysTheSameXpPerHourAsToday_ByBand()
        {
            var parity = new FishingParity(30, 1);
            var options = Small;
            var cohort = new FishingXpRateCohort(parity, options);

            var rows = FishingPerHour.Rows(cohort, parity, options, Context());

            rows.Select(row => row.Band).Should().Equal("0-10", "11-20", "21-30", "31-40", "41-52");
            foreach (var row in rows)
            {
                (row.QuickReelFish / row.TodayFishReference).Should().BeApproximately(1.0, 1e-9, $"{row.Band}: Quick Reel is today's formula at the reference Willpower");
                (row.QuickReelXp / row.TodayXp).Should().BeApproximately(1.0, 1e-9, $"{row.Band}: Quick Reel pays today's XP in today's time");
                row.QuickReelFish.Should().BeInRange(Math.Min(row.TodayFishLow, row.TodayFishHigh) - 1e-6, Math.Max(row.TodayFishLow, row.TodayFishHigh) + 1e-6);
                row.FightFish.Should().BeLessThan(row.QuickReelFish / 3, "the fight takes several times the cast");
                row.FightXp.Should().BeGreaterThan(row.TodayXp * 0.5, "the multipliers keep the fight's XP per hour near today's (the gate checks it exactly at the report's runs)");
            }
        }

        [Test]
        public void AReferenceWillpowerOutsideTheCorridor_FailsTheGateAndNamesTheRows()
        {
            var gate = new FishingCatchRateGate(new FishingParity(30, 1), Small with { QuickReelWillpowerScore = 20 });

            var result = gate.Evaluate(Context());

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("Quick Reel leaves today's corridor in").And.Contain("row(s)");
            result.Tables[0].Rows.Select(row => row.Last()).Should().Contain("Outside");
        }

        [Test]
        public void QuickReelsFormula_IsTheSameInTheGameServerAndInTheLegacyModel()
        {
            foreach (var rank in new[] { 0, 3, 10, 25, 50 })
            {
                foreach (var level in new[] { 0, 1, 8, 20, 33, 52, 80 })
                {
                    foreach (var willpower in new[] { 0, 8, 12, 14, 20 })
                    {
                        FishingQuickReel.CatchChancePercent(rank, level, willpower).Should().Be(
                            FishingCatchModel.CatchChancePercent(rank, level, willpower), $"rank {rank}, level {level}, Willpower {willpower}");
                    }
                }
            }
        }

        [Test]
        public void ARankOnlyReachesThePoolsItsRodCanUse()
        {
            var pools = FishingCatchModel.Pools();

            var novice = FishingCatchRateGate.Reachable(pools, 0).ToList();
            var master = FishingCatchRateGate.Reachable(pools, 40).ToList();

            novice.Should().NotBeEmpty().And.OnlyContain(pool => FishingRodTier.RequiredRank(pool.Rod) == 0);
            master.Count.Should().BeGreaterThan(novice.Count).And.Be(pools.Count);
            FishingCatchRateGate.Reachable(pools, 20).Count().Should().BeInRange(novice.Count + 1, master.Count - 1);
        }

        [Test]
        public void TheSpecialFishAreRare_AndTheOthersAreNot()
        {
            FishingCatchRateGate.RarityOf(FishType.Mercanbaligi).Should().Be(FishRarity.Rare);
            FishingCatchRateGate.RarityOf(FishType.Alzabi).Should().Be(FishRarity.Rare);
            FishingCatchRateGate.RarityOf(FishType.Frigorifish).Should().Be(FishRarity.Common);
            FishingCatchRateGate.RarityOf(FishType.MoatCarp).Should().Be(FishRarity.Common);
        }

        [Test]
        public void TheStudyJudgesTheCorridorBetweenTwoWillpowerScores()
        {
            FishingParityOptions.Default.CorridorLowScore.Should().Be(10);
            FishingParityOptions.Default.CorridorHighScore.Should().Be(14);
            FishingParityOptions.Default.SecondsPerTurn.Should().Be(8);
            FishingParityOptions.Default.QuickReelWillpowerScore.Should().Be(12, "the middle of the corridor, pending the owner's attribute-median decision");
            FishingParityOptions.Default.FightBoundRelative.Should().Be(0.15);
            FishingParityOptions.Default.FightBoundSlackPoints.Should().Be(3);
            FishingParityOptions.Default.FightBound(64).Should().BeApproximately(76.6, 1e-9);
            FishingParityOptions.Default.FightBound(0).Should().Be(3);
            FishingParityOptions.Default.Deltas.Should().HaveCount(15).And.StartWith(-8).And.EndWith(6);
        }
    }
}
