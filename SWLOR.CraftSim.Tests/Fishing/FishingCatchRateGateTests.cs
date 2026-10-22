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
        public void AGenerousTolerance_PassesAndTheTablesShowEveryRow()
        {
            var gate = new FishingCatchRateGate(new FishingParity(30, 1), Small with { TolerancePoints = 100 });

            var result = gate.Evaluate(Context());

            result.Gate.Should().Be(GateNames.EconomyParity);
            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            result.Tables.Should().HaveCount(2);
            var byDelta = result.Tables[0];
            byDelta.Rows.Should().HaveCount(5);
            byDelta.Rows.Select(row => row[0]).Should().Equal("-2", "-1", "0", "+1", "+2");
            byDelta.Header.Should().Contain("Today, Willpower 8").And.Contain("Today, Willpower 20").And.Contain("Fight, adaptive");
            byDelta.Rows[2][2].Should().Be("60%", "today's chance at Willpower 10 and no rank difference (the second of seven columns)");
            result.Tables[1].Rows.Select(row => row[0]).Should().Equal("0", "30");
        }

        [Test]
        public void ATolerance_NoCatchRateCanMeet_FailsAndNamesTheRowsOutside()
        {
            var gate = new FishingCatchRateGate(new FishingParity(30, 1), Small with { TolerancePoints = -1 });

            var result = gate.Evaluate(Context());

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("more than -1 points outside").And.Contain("row(s)");
            result.Tables[0].Rows.Select(row => row.Last()).Should().OnlyContain(verdict => verdict == "Outside");
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
            FishingParityOptions.Default.Deltas.Should().HaveCount(15).And.StartWith(-8).And.EndWith(6);
        }
    }
}
