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
                "New success", "Legacy success", "New avg quality", "Legacy avg quality", "Quality matched", "Never made (new / legacy)", "Parity rule", "Verdict");
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
            noGear[^2].Should().Be("n/a", "there is no parity rule without a legacy figure");
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

        // Level 52: the old minigame's lookahead player succeeds 20% of the time with mid gear and about 65% with best gear (and never with no gear).
        private static readonly int[] OftenFails = { 52 };

        [Test]
        public void WhereTodaysCrafterFailsOftenUsingLessMaterialThanTodayPasses()
        {
            // The table's minigame never fails, so it uses far fewer stacks per item than a crafter who loses the materials of most crafts.
            var reliable = new TableScenario { Quality = (_, _) => 100 };

            var result = Evaluate("materials", reliable, Context(OftenFails));

            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            var byGear = result.Tables[0].Rows.ToDictionary(row => row[1]);
            byGear["mid gear"][7].Should().Be("20%", "the legacy success rate that makes the cell one-sided");
            double.Parse(byGear["mid gear"][5]).Should().BeLessThan(0.9, "the new minigame uses far less material, which the two-sided rule would have failed");
            byGear["mid gear"][^2].Should().Contain("less, passes");
            byGear["mid gear"][^1].Should().Be("Passed");
            double.Parse(byGear["best gear"][5]).Should().BeLessThan(0.9);
            byGear["best gear"][^2].Should().Contain("less, passes");
            byGear["no gear"][^1].Should().StartWith("Skipped");
            result.Summary.Should().Contain("at most 10% above it where it is lower").And.Contain("use more than 10% less material than today, which passes");
        }

        [Test]
        public void TheSameLowerUseFailsWhereTodaysCrafterSucceedsOftenEnough()
        {
            var reliable = new TableScenario { Quality = (_, _) => 100 };
            var strict = GateThresholds.Default with { MaterialTwoSidedMinimumLegacySuccess = 0.10 };

            var result = Evaluate("materials", reliable, Context(OftenFails), thresholds: strict);

            result.Verdict.Should().Be(GateVerdict.Failed, "a legacy success rate of 20% is now enough for the two-sided rule");
            var byGear = result.Tables[0].Rows.ToDictionary(row => row[1]);
            byGear["mid gear"][^2].Should().Be("within 10%");
            byGear["mid gear"][^1].Should().Be("Failed");
            byGear["best gear"][^1].Should().Be("Failed");
            result.Summary.Should().Contain("Outside 10%");
        }

        [Test]
        public void TheLegacySuccessRateThatTurnsTheRuleOneSidedIsTheNamedThreshold()
        {
            var reliable = new TableScenario { Quality = (_, _) => 100 };
            var above = GateThresholds.Default with { MaterialTwoSidedMinimumLegacySuccess = 0.70 };
            var below = GateThresholds.Default with { MaterialTwoSidedMinimumLegacySuccess = 0.60 };

            var oneSided = Evaluate("materials", reliable, Context(OftenFails), thresholds: above);
            var twoSided = Evaluate("materials", reliable, Context(OftenFails), thresholds: below);

            oneSided.Tables[0].Rows.Single(row => row[1] == "best gear")[^1].Should().Be("Passed", "a legacy success rate of about 65% is under 70%");
            twoSided.Tables[0].Rows.Single(row => row[1] == "best gear")[^1].Should().Be("Failed", "and over 60%");
            GateThresholds.Default.MaterialTwoSidedMinimumLegacySuccess.Should().Be(0.90);
        }

        [TestCase(0.91, 0.80, false, true, TestName = "At 91% legacy success using 20% less fails: the parity is two-sided")]
        [TestCase(0.89, 0.80, true, false, TestName = "At 89% legacy success using 20% less passes: the parity is one-sided")]
        [TestCase(0.89, 0.50, true, false, TestName = "At 89% legacy success using half as much passes")]
        [TestCase(0.91, 1.15, false, true, TestName = "At 91% legacy success using 15% more fails")]
        [TestCase(0.89, 1.15, false, false, TestName = "At 89% legacy success using 15% more fails too")]
        [TestCase(0.91, 1.05, true, true, TestName = "At 91% legacy success using 5% more passes")]
        [TestCase(0.89, 1.05, true, false, TestName = "At 89% legacy success using 5% more passes")]
        [TestCase(0.91, 0.95, true, true, TestName = "At 91% legacy success using 5% less passes")]
        [TestCase(0.89, 0.95, true, false, TestName = "At 89% legacy success using 5% less passes")]
        [TestCase(0.90, 0.80, false, true, TestName = "At exactly 90% legacy success the parity is two-sided")]
        [TestCase(0.9 - 1e-12, 0.80, false, true, TestName = "A rounding error under 90% is still 90%")]
        [TestCase(0.0, 0.01, true, false, TestName = "Where the legacy crafter nearly always fails, using almost nothing passes")]
        public void TheParityRuleIsTwoSidedFromNinetyPercentLegacySuccessAndOneSidedBelow(double legacySuccess, double ratio, bool passes, bool twoSided)
        {
            var judgment = MaterialParityRule.Judge(ratio, legacySuccess, GateThresholds.Default);

            judgment.Passed.Should().Be(passes);
            judgment.TwoSided.Should().Be(twoSided);
        }

        [Test]
        public void TheToleranceEdgeIsInsideTheRuleOnBothSides()
        {
            var thresholds = GateThresholds.Default;

            MaterialParityRule.Judge(1.10, 0.95, thresholds).Passed.Should().BeTrue();
            MaterialParityRule.Judge(1.101, 0.95, thresholds).Passed.Should().BeFalse();
            MaterialParityRule.Judge(0.90, 0.95, thresholds).Passed.Should().BeTrue();
            MaterialParityRule.Judge(0.899, 0.95, thresholds).Passed.Should().BeFalse();
            MaterialParityRule.Judge(1.10, 0.50, thresholds).Passed.Should().BeTrue();
            MaterialParityRule.Judge(1.101, 0.50, thresholds).Passed.Should().BeFalse();
        }

        [Test]
        public void ANewMinigameThatMakesNoItemFailsWhateverTheLegacySuccessRate()
        {
            MaterialParityRule.Judge(double.PositiveInfinity, 0.95, GateThresholds.Default).Passed.Should().BeFalse();
            MaterialParityRule.Judge(double.PositiveInfinity, 0.20, GateThresholds.Default).Passed.Should().BeFalse();
        }

        [Test]
        public void TheReportSaysWhichRuleAppliedAndHowMuchLessMaterialAPassUses()
        {
            var thresholds = GateThresholds.Default;

            MaterialParityRule.Judge(0.62, 0.91, thresholds).Describe(0.62, thresholds).Should().Be("within 10%");
            MaterialParityRule.Judge(0.62, 0.89, thresholds).Describe(0.62, thresholds).Should().Be("at most +10%; uses 38% less, passes");
            MaterialParityRule.Judge(0.95, 0.89, thresholds).Describe(0.95, thresholds).Should().Be("at most +10%");
        }
    }
}