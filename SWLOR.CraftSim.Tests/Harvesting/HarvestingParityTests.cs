using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Harvesting;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

namespace SWLOR.CraftSim.Tests.Harvesting
{
    [TestFixture]
    public class HarvestingParityTests
    {
        private static GateContext Context(int runs = 600) => new(null, null, runs, 1, AttributeSweep.Default);

        [Test]
        public void QuickHarvestIsTodaysLoop_TheSameUnitsXpAndSecondsAtAMightModifierOfZero()
        {
            ProspectingQuickHarvest.ExpectedUnits.Should().Be(HarvestModel.ExpectedHarvestsPerNode).And.Be(HarvestModel.ExpectedItemsPerNode(0));
            ProspectingRules.HarvestSeconds.Should().Be(HarvestingParityOptions.Default.SecondsPerHarvest);

            foreach (var tier in new[] { 1, 2, 3, 4, 5 })
            {
                foreach (var rank in new[] { 0, 5, 25, 50 })
                {
                    HarvestingParity.QuickHarvestXpPerUnit(tier, rank).Should().Be(HarvestModel.XpPerItem(tier, rank), $"tier {tier} rank {rank}");
                    GatheringLevels.ForTier(tier).Should().Be(HarvestModel.VeinLevel(tier));
                }
            }
        }

        [Test]
        public void QuickHarvestPlayedFromTheSeedMatchesTodaysMean_AndSpendsAChargeAndADelayForEachUnit()
        {
            var parity = HarvestingFixture.Parity(runs: 3000);

            var sample = parity.QuickHarvestSample(new HarvestCase(2, 15, 0.1));

            sample.ItemsPerNode.Should().BeApproximately(2.5, 0.08);
            sample.ChargesPerNode.Should().Be(sample.ItemsPerNode);
            sample.SecondsPerNode.Should().BeApproximately(sample.ItemsPerNode * ProspectingRules.HarvestSeconds, 1e-9);
            sample.ActionsPerNode.Should().Be(sample.ItemsPerNode);
            sample.GemsPerNode.Should().Be(0);
            sample.RichRollsPerNode.Should().Be(0);
            sample.PoolLeftPerNode.Should().Be(0, "a fresh harvester takes the whole pool");
        }

        [Test]
        public void ALimitedHarvesterLimitsQuickHarvest()
        {
            var parity = HarvestingFixture.Parity(runs: 500, options: new HarvestingParityOptions { Ranks = new[] { 0 }, Charges = 1 });

            var sample = parity.QuickHarvestSample(new HarvestCase(1, 0, 0.1));

            sample.ItemsPerNode.Should().Be(1.0);
            sample.ChargesPerNode.Should().Be(1.0);
        }

        [Test]
        public void AStrikeOnlySessionYieldsTodaysItemsEvenThoughItNeverLooksForASeam()
        {
            var parity = HarvestingFixture.Parity(runs: 3000);

            foreach (var tier in new[] { 1, 3, 5 })
            {
                foreach (var rank in new[] { 0, 25, 50 })
                {
                    if (rank < CraftRankMilestones.RequiredRankForTier(tier))
                        continue;

                    var casual = parity.Mean(tier, rank, ProspectorPlan.Casual);
                    casual.Items.Should().BeApproximately(2.5, 0.08, $"tier {tier} rank {rank}");
                    casual.PocketGems.Should().Be(0, "a player who never looks for a seam finds no pocket");
                    casual.Gems.Should().BeApproximately(parity.TodayGemsPerNode(tier), 0.02, "its units are today's rolls of the same table");
                    casual.Collapsed.Should().Be(0);
                }
            }
        }

        [Test]
        public void ASkilledSessionYieldsMoreThanTodayButNotMoreThanFifteenPercent()
        {
            var parity = HarvestingFixture.Parity(runs: 3000);

            foreach (var tier in new[] { 1, 3, 5 })
            {
                var rank = Math.Min(50, GatheringLevels.ForTier(tier));
                var ratio = parity.Skilled(tier, rank).Mean.Items / HarvestingParity.TodayItemsPerNode;

                ratio.Should().BeInRange(1.02, 1.15, $"tier {tier} rank {rank}");
                parity.Ceiling(tier, rank).Mean.Items.Should().BeGreaterThanOrEqualTo(parity.Skilled(tier, rank).Mean.Items);
            }
        }

        [Test]
        public void ASkilledSessionTakesFewerSecondsPerItemThanTodayAndMoreChargesPerItem()
        {
            var parity = HarvestingFixture.Parity(runs: 2000);

            var skilled = parity.Skilled(3, 25).Mean;

            (skilled.Seconds / skilled.Items).Should().BeLessThan(ProspectingRules.HarvestSeconds);
            (skilled.Charges / skilled.Items).Should().BeGreaterThan(1.0, "Survey and Extract cost charges and make no unit of their own");
            skilled.PocketGems.Should().BeGreaterThan(0, "a gem pocket is the upside");
            skilled.Gems.Should().BeInRange(parity.TodayGemsPerNode(3), parity.TodayGemsPerNode(3) * 1.15, "the gems of the rolls and the pockets are within the cap");
        }

        [Test]
        public void ASkilledSessionSpendsAtMostAboutOneAndAHalfTimesTodaysChargesPerNode()
        {
            var parity = HarvestingFixture.Parity(runs: 3000);

            foreach (var tier in new[] { 1, 3, 5 })
            {
                var rank = Math.Min(50, GatheringLevels.ForTier(tier));
                var charges = parity.Skilled(tier, rank).Mean.Charges;

                charges.Should().BeGreaterThan(HarvestingParity.TodayItemsPerNode, $"tier {tier}: a skilled session still pays for its extracts");
                charges.Should().BeLessThanOrEqualTo(HarvestingParity.TodayItemsPerNode * 1.55, $"tier {tier}: a Survey costs no charge");
            }
        }

        [Test]
        public void TheGemsOfASessionAreItsUnitsAsRollsOfTheTable_PlusTheGemsOfItsPockets()
        {
            var plan = ProspectorPlan.Skilled[1];

            // A table that gave a gem on a third of its rolls: pockets are common enough to find in a short run.
            var sample = ProspectingPlays.Sample(3, 25, 1.0 / 3, plan, 3000, 1, 7);
            var mean = ProspectingMean.Of(sample, 0, 0, 1.0 / 3);

            sample.GemsPerNode.Should().BeGreaterThan(0, "a skilled player claims the pockets they find");
            mean.PocketGems.Should().Be(sample.GemsPerNode);
            mean.Gems.Should().BeApproximately(sample.UnitsPerNode / 3 + sample.GemsPerNode, 1e-9);
            ProspectingMean.Of(ProspectingPlays.QuickHarvest(3, 25, 1.0 / 3, 3000, 1, 7), 0, 0, 1.0 / 3).Gems
                .Should().BeApproximately(ProspectingQuickHarvest.ExpectedUnits / 3, 0.03, "Quick Harvest rolls the table as today's harvests did");
        }

        [Test]
        public void TheGemsOfEachPlanStayNearTodays_AndTheSkilledPlayersWithinTheCap()
        {
            var parity = HarvestingFixture.Parity(runs: 3000);

            foreach (var tier in new[] { 1, 5 })
            {
                var today = parity.TodayGemsPerNode(tier);
                var casual = parity.Mean(tier, 50, ProspectorPlan.Casual);
                var skilled = parity.Skilled(tier, 50).Mean;
                var quick = parity.QuickHarvest(tier, 50);

                quick.Gems.Should().BeApproximately(today, today * 0.06, $"tier {tier}: Quick Harvest rolls the table as today does");
                casual.Gems.Should().BeApproximately(today, today * 0.06);
                (skilled.Gems / today).Should().BeInRange(1.0, 1.15, $"tier {tier}: the skilled gems are within the cap");
            }
        }

        [Test]
        public void TheSameSeedPlaysTheSameNodes_AndAnotherSeedDoesNot()
        {
            var first = HarvestingFixture.Parity(runs: 400, seed: 4).Skilled(2, 25).Mean;
            var again = HarvestingFixture.Parity(runs: 400, seed: 4).Skilled(2, 25).Mean;
            var other = HarvestingFixture.Parity(runs: 400, seed: 5).Skilled(2, 25).Mean;

            again.Should().Be(first);
            other.Should().NotBe(first);
        }

        [Test]
        public void TheRecklessPlayerCanCollapseANodeAndLoseTheUnitsLeftInIt()
        {
            var parity = HarvestingFixture.Parity(runs: 2000);

            var sample = parity.Play(new HarvestCase(1, 0, 0.1), ProspectorPlan.Reckless);

            sample.CollapsedShare.Should().BeGreaterThan(0, "spending the Stability on seams first can leave too little for the pool");
            sample.PoolLeftPerNode.Should().BeGreaterThan(0);
            parity.Play(new HarvestCase(1, 0, 0.1), ProspectorPlan.Skilled[0]).CollapsedShare.Should().Be(0);
        }

        // The gates.

        [Test]
        public void TheEconomyParityGatePassesWithTheShippedNumbers()
        {
            var parity = HarvestingFixture.Shipped;

            var result = new HarvestingYieldGate(parity).Evaluate(Context());

            result.Gate.Should().Be(GateNames.EconomyParity);
            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            result.Tables.Should().HaveCount(4);
            result.Tables[0].Rows.Should().HaveCount(5, "one row for each node tier");
            result.Tables[0].Rows.Should().OnlyContain(row => row[row.Count - 1] == "Passed");
            result.Tables[2].Rows.Should().NotBeEmpty().And.OnlyContain(row => row[row.Count - 1] == "Passed");
            result.Tables[3].Rows.Should().NotBeEmpty().And.OnlyContain(row => row[row.Count - 1] == "Passed");
            result.Summary.Should().Contain("Quick Harvest equals today's 2.50 items, 12.5 s and charges per node");
            result.Summary.Should().Contain("Gems per node").And.Contain("harvester charges per node");
        }

        [Test]
        public void TheEconomyParityGateFailsWhenASkilledSessionBeatsTheGemCap()
        {
            var parity = HarvestingFixture.Parity(
                runs: 1500, options: new HarvestingParityOptions { Ranks = new[] { 0, 50 }, SkilledCapShare = 0.5, GemCapShare = 0.01 });

            var result = new HarvestingYieldGate(parity).Evaluate(Context());

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("today's gems per node");
        }

        [Test]
        public void TheEconomyParityGateFailsWhenASkilledSessionWearsOutTheHarvesterTooFast()
        {
            var parity = HarvestingFixture.Parity(
                runs: 1500, options: new HarvestingParityOptions { Ranks = new[] { 0, 50 }, SkilledCapShare = 0.5, GemCapShare = 0.5, ChargeCapShare = 0.1 });

            var result = new HarvestingYieldGate(parity).Evaluate(Context());

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("harvester charges per node");
        }

        [Test]
        public void TheEconomyParityGateFailsWhenASkilledSessionBeatsTheCap()
        {
            var parity = HarvestingFixture.Parity(
                runs: 1500, options: new HarvestingParityOptions { Ranks = new[] { 0, 50 }, SkilledCapShare = 0.01 });

            var result = new HarvestingYieldGate(parity).Evaluate(Context());

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("leaves the band");
        }

        [Test]
        public void TheEconomyParityGateFailsWhenPlayingIsWorseThanQuickHarvest()
        {
            var parity = HarvestingFixture.Parity(
                runs: 1500, options: new HarvestingParityOptions { Ranks = new[] { 0, 50 }, SkilledFloorShare = 1.12 });

            new HarvestingYieldGate(parity).Evaluate(Context()).Verdict.Should().Be(GateVerdict.Failed);
        }

        [Test]
        public void TheLengthGatePassesAtThePlansTargetAndFailsOutsideIt()
        {
            var parity = HarvestingFixture.Parity(runs: 1500);
            new HarvestingLengthGate(parity).Evaluate(Context()).Verdict.Should().Be(GateVerdict.Passed);

            var strict = HarvestingFixture.Parity(
                runs: 1500, options: new HarvestingParityOptions { Ranks = new[] { 0, 50 }, LeastActions = 6 });
            var result = new HarvestingLengthGate(strict).Evaluate(Context());

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Gate.Should().Be(GateNames.Length);
        }

        [Test]
        public void TheXpRateGatePassesForQuickHarvestExactlyAndForASkilledSession()
        {
            var parity = HarvestingFixture.Parity(runs: 1500);

            var quick = new XpRateGate(new HarvestingXpRateCohort(parity, HarvestingLoop.QuickHarvest)).Evaluate(Context());
            var skilled = new XpRateGate(new HarvestingXpRateCohort(parity, HarvestingLoop.SkilledSession)).Evaluate(Context());
            var casual = new XpRateGate(new HarvestingXpRateCohort(parity, HarvestingLoop.StrikeOnlySession)).Evaluate(Context());

            quick.Verdict.Should().Be(GateVerdict.Passed, quick.Summary);
            skilled.Verdict.Should().Be(GateVerdict.Passed, skilled.Summary);
            casual.Verdict.Should().Be(GateVerdict.Passed, casual.Summary);
            quick.Tables[0].Rows.Should().OnlyContain(row => row[8] == "1.00");
        }

        [Test]
        public void TheXpRateCohortHasACaseForEachNodeTierAtTheVeinsLevel()
        {
            var cohort = new HarvestingXpRateCohort(HarvestingFixture.Parity(), HarvestingLoop.QuickHarvest);

            var cases = cohort.Cases(Context());

            cases.Select(xpCase => xpCase.Group).Should().Equal(
                "Tier 1 vein (level 5)", "Tier 2 vein (level 15)", "Tier 3 vein (level 25)", "Tier 4 vein (level 35)", "Tier 5 vein (level 45)");
            cases.Select(xpCase => xpCase.Label).Should().Equal(
                "harvester tier 1, rank = vein level", "harvester tier 2, rank = vein level", "harvester tier 3, rank = vein level",
                "harvester tier 4, rank = vein level", "harvester tier 5, rank = vein level");
            cohort.RunName.Should().Be("node");

            var legacy = cohort.PlayLegacy(cases[2], Context());
            legacy.XpPerRun.Should().Be(HarvestModel.ExpectedXpPerNode(3, 25, 0));
            legacy.ActionsPerRun.Should().Be(2.5);
        }

        [Test]
        public void TheXpRateGateWouldFailASessionThatPaidLessPerHourThanToday()
        {
            // A session that took twice as long for the same items would need a multiplier; here the cohort is made to play it.
            var slow = new HarvestingParityOptions { Ranks = new[] { 0, 50 }, SecondsPerHarvest = 1.0 };
            var parity = HarvestingFixture.Parity(runs: 1500, options: slow);

            var result = new XpRateGate(new HarvestingXpRateCohort(parity, HarvestingLoop.SkilledSession)).Evaluate(Context());

            result.Verdict.Should().Be(GateVerdict.Failed, "with a harvest delay of 1 s, a session's seconds are five times today's");
        }

        [Test]
        public void TheSessionsXpPerItemIsTodaysTimesTheMultiplier_AndQuickHarvestsIsTodays()
        {
            foreach (var tier in new[] { 1, 3, 5 })
            {
                HarvestingParity.SessionXpPerItem(tier, 25).Should().Be(
                    CraftMinigameXp.ScaleBaseXp(HarvestModel.XpPerItem(tier, 25), ProspectingRules.XpMultiplierFor(GatheringLevels.ForTier(tier))));
                HarvestingParity.QuickHarvestXpPerUnit(tier, 25).Should().Be(HarvestModel.XpPerItem(tier, 25));
            }
        }
    }
}
