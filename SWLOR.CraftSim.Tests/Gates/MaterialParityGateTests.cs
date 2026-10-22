using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Tests.Support;
using static SWLOR.CraftSim.Tests.Support.BenchHarness;

namespace SWLOR.CraftSim.Tests.Gates
{
    [TestFixture]
    public class MaterialParityGateTests
    {
        // Levels where the old minigame's lookahead player almost always succeeds, so a reliable new minigame matches it.
        private static readonly int[] Reliable = { 5, 15 };

        [Test]
        public void PassesWhenComponentStacksPerItemMatchTheLegacyBaseline()
        {
            var result = Evaluate("materials", new BenchScenario(), Context(Reliable));

            result.Gate.Should().Be(GateNames.EconomyParity);
            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            RowVerdicts(result).Should().HaveCount(6).And.OnlyContain(verdict => verdict == "Passed");
            foreach (var row in result.Tables[0].Rows)
            {
                // Each recipe has two component stacks and makes one item, so a reliable minigame uses about two stacks an item.
                double.Parse(row[3]).Should().BeInRange(2.0, 2.6);
                double.Parse(row[5]).Should().BeInRange(0.9, 1.1);
            }
        }

        [Test]
        public void FailsWhenCraftsBreakSoOftenThatMaterialsPerItemRise()
        {
            var fragile = new BenchScenario { PressIntegrityCost = 35 };

            var result = Evaluate("materials", fragile, Context(Reliable));

            result.Verdict.Should().Be(GateVerdict.Failed);
            RowVerdicts(result).Should().OnlyContain(verdict => verdict == "Failed");
            result.Tables[0].Rows.Where(row => row[5] != "n/a").Should().OnlyContain(row => double.Parse(row[5]) > 1.5, "far more stacks go into each item");
            result.Summary.Should().Contain("Outside 10%");
        }

        [Test]
        public void ReportsTheSuccessRateAndQualityOnBothSidesAndTheRecipesNeverMade()
        {
            var result = Evaluate("materials", new BenchScenario(), Context(Reliable));

            result.Tables[0].Header.Should().Equal(
                "Band", "Gear", "Recipes played", "New stacks per item", "Legacy stacks per item", "Ratio",
                "New success", "Legacy success", "New avg quality", "Legacy avg quality", "Never made (new / legacy)", "Verdict");
            result.Tables[0].Rows.Should().OnlyContain(row => row[10] == "0% / 0%");
        }

        [Test]
        public void SkipsACaseWhereTheLegacyBaselineMakesNothing()
        {
            // At level 52 with no gear the old minigame never finishes a craft.
            var result = Evaluate("materials", new BenchScenario(), Context(new[] { 52 }));

            var noGear = result.Tables[0].Rows.Single(row => row[1] == "no gear");
            noGear[4].Should().Be("n/a");
            noGear[^1].Should().StartWith("Skipped");
        }

        [Test]
        public void TheToleranceIsTheNamedThreshold()
        {
            var tight = GateThresholds.Default with { MaterialTolerance = 0.0 };

            var result = Evaluate("materials", new BenchScenario(), Context(Reliable), thresholds: tight);

            result.Verdict.Should().Be(GateVerdict.Failed, "no two independent simulations match to the last digit");
        }

        [Test]
        public void ARecipeThatNeverSucceedsIsCountedNotHiddenInTheAverage()
        {
            var impossible = new BenchScenario { PressIntegrityCost = 60 };

            var result = Evaluate("materials", impossible, Context(Reliable));

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Tables[0].Rows.Should().Contain(row => row[3] == "never made" && row.Last() == "Failed");
        }
    }
}
