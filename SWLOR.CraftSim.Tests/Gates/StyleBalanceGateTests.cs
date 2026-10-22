using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService;
using static SWLOR.CraftSim.Tests.Support.BenchHarness;

namespace SWLOR.CraftSim.Tests.Gates
{
    [TestFixture]
    public class StyleBalanceGateTests
    {
        private static readonly int[] Levels = { 25, 45 };
        private static readonly CraftStyle Press = BenchScenario.PressStyle;
        private static readonly CraftStyle Polish = BenchScenario.PolishStyle;

        private static bool Armor(RecipeEntry recipe) => recipe.Category == RecipeCategoryType.Breastplate;

        private static GateResult Run(Func<RecipeEntry, CraftStyle, int> quality, int minimumRank = 20, GateThresholds thresholds = null) =>
            Evaluate(
                "styles",
                new TableScenario { Quality = quality, StylePairs = new[] { new CraftStylePair(Press, Polish, minimumRank) } },
                Context(Levels),
                recipesPerBand: 2,
                thresholds: thresholds);

        [Test]
        public void IsNotApplicableUntilAScenarioOffersStyles()
        {
            var result = Evaluate("styles", new BenchScenario(), Context(Levels), recipesPerBand: 2);

            result.Gate.Should().Be(GateNames.StyleBalance);
            result.Verdict.Should().Be(GateVerdict.NotApplicable);
            result.Summary.Should().Contain("no style configurations");
            result.Tables.Should().BeEmpty();
        }

        [Test]
        public void PassesWhenEachStyleSuitsDifferentRecipesAndTheAveragesMatch()
        {
            // Heavy Press makes armor better and Fine Polish makes weapons better, by the same amount: a mirror.
            var result = Run((recipe, style) => (Armor(recipe) == (style == Press)) ? 90 : 60);

            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            RowVerdicts(result).Should().HaveCount(3, "one pair at three gear tiers").And.OnlyContain(verdict => verdict == "Passed");
            result.Tables[0].Rows.Should().OnlyContain(row =>
                row[0] == "Heavy Press vs Fine Polish" && row[2] == "4" && row[3] == "50%" && row[4] == "50%" && row[5] == "75.0" && row[6] == "75.0");
        }

        [Test]
        public void FailsWhenOneStyleIsBetterOnEveryRecipe()
        {
            var result = Run((recipe, style) => style == Press ? 80 : 65);

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("Heavy Press vs Fine Polish / no gear").And.Contain("3 style comparison");
            result.Tables[0].Rows.Should().OnlyContain(row => row[3] == "100%" && row[4] == "0%" && row[7] == "+15.0" && row.Last() == "Failed");
        }

        [Test]
        public void FailsWhenTheAverageGapIsTooLargeEvenThoughBothStylesWinOften()
        {
            // Each style is better on half the recipes, but Heavy Press wins armor by 60 and loses weapons by 20: 20 points apart on average.
            var result = Run((recipe, style) => Armor(recipe) ? (style == Press ? 100 : 40) : (style == Press ? 40 : 60));

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Tables[0].Rows.Should().OnlyContain(row => row[3] == "50%" && row[4] == "50%" && row[7] == "+20.0" && row.Last() == "Failed");
        }

        [Test]
        public void FailsWhenAStyleIsBetterOnTooFewRecipes()
        {
            // Heavy Press is better on one recipe in four (25%) and Fine Polish on the other three, each by two points.
            var result = Run((recipe, style) =>
            {
                var first = recipe.Level == 25 && Armor(recipe);
                return (style == Press) == first ? 72 : 70;
            });

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Tables[0].Rows.Should().OnlyContain(row => row[3] == "25%" && row[4] == "75%" && row.Last() == "Failed");
            result.Tables[0].Rows.Should().OnlyContain(row => Math.Abs(double.Parse(row[7])) <= 5, "the averages are close, so only the share fails");
        }

        [Test]
        public void RecipesWithinTheTieMarginCountHalfAWinForEachStyle()
        {
            var result = Run((_, _) => 70);

            result.Verdict.Should().Be(GateVerdict.Passed);
            result.Tables[0].Rows.Should().OnlyContain(row => row[3] == "50%" && row[4] == "50%" && row[7] == "0.0");
        }

        [Test]
        public void TheWinShareAndMeanGapAreTheNamedThresholds()
        {
            Func<RecipeEntry, CraftStyle, int> mirror = (recipe, style) => (Armor(recipe) == (style == Press)) ? 90 : 60;

            Run(mirror, thresholds: GateThresholds.Default with { StyleMinimumWinShare = 0.6 }).Verdict.Should().Be(GateVerdict.Failed);

            Func<RecipeEntry, CraftStyle, int> small = (recipe, style) => style == Press ? 73 : 70;
            Run(small, thresholds: GateThresholds.Default with { StyleMaximumMeanDifferencePoints = 5, StyleMinimumWinShare = 0 }).Verdict
                .Should().Be(GateVerdict.Passed, "a 3 point gap is inside the 5 points allowed");
            Run(small, thresholds: GateThresholds.Default with { StyleMaximumMeanDifferencePoints = 2, StyleMinimumWinShare = 0 }).Verdict
                .Should().Be(GateVerdict.Failed);
        }

        [Test]
        public void IsNotRunWhenNoRecipeIsPlayedAtTheStylesMinimumRank()
        {
            var result = Run((_, _) => 70, minimumRank: 60);

            result.Verdict.Should().Be(GateVerdict.NotRun);
            result.Summary.Should().Contain("minimum rank");
        }

        [Test]
        public void OnlyRecipesAtOrAboveTheMinimumRankCount()
        {
            var result = Run((recipe, style) => (Armor(recipe) == (style == Press)) ? 90 : 60, minimumRank: 30);

            result.Tables[0].Rows.Should().OnlyContain(row => row[2] == "2", "only the two level-45 recipes can use a style from rank 30");
        }

        [Test]
        public void EveryPairIsComparedAtEveryGearTier()
        {
            var scenario = new TableScenario
            {
                Quality = (_, _) => 70,
                StylePairs = new[] { new CraftStylePair(Press, Polish), new CraftStylePair(BenchScenario.Overpowered, BenchScenario.Plain) }
            };

            var result = Evaluate("styles", scenario, Context(Levels), recipesPerBand: 2);

            result.Tables[0].Rows.Select(row => row[0] + "/" + row[1]).Should().Equal(
                "Heavy Press vs Fine Polish/no gear", "Heavy Press vs Fine Polish/mid gear", "Heavy Press vs Fine Polish/best gear",
                "Overpowered vs Plain/no gear", "Overpowered vs Plain/mid gear", "Overpowered vs Plain/best gear");
        }

        [Test]
        public void BothStylesArePlayedByTheAdaptivePlayerOnTheSameDice()
        {
            // On the test bench the styles change how strong Press and Polish are; the gate plays each recipe in both and compares them.
            var scenario = new BenchScenario { StylePairs = new[] { new CraftStylePair(Press, Polish) } };

            var result = Evaluate("styles", scenario, Context(Levels), recipesPerBand: 2);

            result.Tables[0].Rows.Should().HaveCount(3);
            result.Tables[0].Rows.Should().OnlyContain(row => row[2] == "4");
            result.Tables[0].Rows.Select(row => double.Parse(row[5])).Should().OnlyContain(quality => quality > 50 && quality <= 100);
        }
    }
}
