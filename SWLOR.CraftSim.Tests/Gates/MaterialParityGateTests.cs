using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
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
            var fragile = new BenchScenario { PressIntegrityCost = 42 };

            var result = Evaluate("materials", fragile, Context(Reliable));

            result.Verdict.Should().Be(GateVerdict.Failed);
            var failing = result.Tables[0].Rows.Where(row => row.Last() == "Failed").ToList();
            failing.Should().NotBeEmpty();
            failing.Where(row => row[5] != "n/a").Should().OnlyContain(row => double.Parse(row[5]) > 1.1, "far more stacks go into each item");
            result.Summary.Should().Contain("Outside 10%");
        }

        [Test]
        public void ReportsTheSuccessRateAndQualityOnBothSidesAndTheRecipesNeverMade()
        {
            var result = Evaluate("materials", new BenchScenario(), Context(Reliable));

            result.Tables[0].Header.Should().Equal(
                "Band", "Gear", "Recipes played", "New stacks per item", "Legacy stacks per item", "Ratio",
                "New success", "Legacy success", "New avg quality", "Legacy avg quality", "Quality matched", "Never made (new / legacy)", "Verdict");
            result.Tables[0].Rows.Should().OnlyContain(row => row[10] == "yes" && row[11] == "0% / 0%");
        }

        [Test]
        public void MatchingMaterialsIsNotEnoughWhenTheItemsAreMuchWorseThanToday()
        {
            // Every craft succeeds, so stacks per item match the legacy player's, but the items have no quality at all.
            var worse = new TableScenario { Quality = (_, _) => 0 };

            var result = Evaluate("materials", worse, Context(Reliable));

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("below its quality").And.Contain("quality 0% against");
            var byGear = result.Tables[0].Rows.ToDictionary(row => row[0] + "/" + row[1]);
            byGear["1-10/no gear"].Last().Should().Be("Passed", "the legacy player with no gear makes 2% quality, inside the 5 points allowed");
            byGear["1-10/best gear"][10].Should().Be("no");
            byGear["1-10/best gear"].Last().Should().Be("Failed");
            double.Parse(byGear["1-10/best gear"][5]).Should().BeInRange(0.9, 1.1, "the stacks per item alone would have passed");
        }

        [Test]
        public void QualityIsWeightedByTheItemsEachRecipeMakes()
        {
            // One recipe makes a single item at full quality and another makes ten at no quality: most items are worthless, though the
            // two recipes' qualities average 50%.
            var stacks = new Dictionary<string, int> { ["plate"] = 3, ["wire"] = 1 };
            var catalog = RecipeCatalog.From(new[]
            {
                new RecipeEntry((RecipeType)91001, SkillType.Smithery, 5, "single", RecipeCategoryType.Breastplate, stacks, quantity: 1),
                new RecipeEntry((RecipeType)91002, SkillType.Smithery, 5, "batch", RecipeCategoryType.Longsword, stacks, quantity: 10)
            });
            var scenario = new TableScenario { Quality = (recipe, _) => recipe.Quantity == 1 ? 100 : 0 };
            var context = new GateContext(catalog, SharedData.Gear, 20, 1, AttributeSweep.Default);

            var result = Evaluate("materials", scenario, context, recipesPerBand: 2);

            var best = result.Tables[0].Rows.Single(row => row[1] == "best gear");
            best[8].Should().Be("9%", "100 x 1 and 0 x 10 over eleven items");
            best[10].Should().Be("no", "the legacy player's best-gear items average well above that");
            result.Verdict.Should().Be(GateVerdict.Failed);
        }

        [Test]
        public void ItemsAsGoodAsTodaysPassWithTheSameMaterials()
        {
            var good = new TableScenario { Quality = (_, _) => 100 };

            var result = Evaluate("materials", good, Context(Reliable));

            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            result.Tables[0].Rows.Should().OnlyContain(row => row[10] == "yes");
        }

        [Test]
        public void TheQualityShortfallIsTheNamedThreshold()
        {
            var worse = new TableScenario { Quality = (_, _) => 0 };
            var lenient = GateThresholds.Default with { MaterialQualityShortfallPoints = 100 };

            Evaluate("materials", worse, Context(Reliable), thresholds: lenient).Verdict.Should().Be(GateVerdict.Passed);
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
