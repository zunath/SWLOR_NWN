using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Tests.Support;
using static SWLOR.CraftSim.Tests.Support.BenchHarness;

namespace SWLOR.CraftSim.Tests.Gates
{
    [TestFixture]
    public class NoFixedRotationGateTests
    {
        private static readonly int[] Levels = { 15, 45 };

        [Test]
        public void PassesWhenAdaptivePlayBeatsTheBestFixedRotationInEveryBandAndGearTier()
        {
            var result = Evaluate("rotation", new BenchScenario(), Context(Levels));

            result.Gate.Should().Be(GateNames.NoFixedRotation);
            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            RowVerdicts(result).Should().HaveCount(6, "two bands at three gear tiers").And.OnlyContain(verdict => verdict == "Passed");
            result.Summary.Should().Contain("by at least 10 points");
        }

        [Test]
        public void ReportsEveryBandAndGearTierWithTheMarginAndTheRotationFound()
        {
            var result = Evaluate("rotation", new BenchScenario(), Context(Levels));

            result.Tables.Should().HaveCount(3);
            result.Tables[0].Header.Should().Equal(
                "Band", "Gear", "Recipes played", "Adaptive avg quality", "Best single rotation avg quality", "Margin", "Required",
                "Best rotation per recipe avg quality (reported)", "Verdict");
            result.Tables[0].Rows.Select(row => row[0] + "/" + row[1]).Should().Equal(
                "11-20/no gear", "11-20/mid gear", "11-20/best gear", "41-53/no gear", "41-53/mid gear", "41-53/best gear");
            result.Tables[1].Header.Should().Contain("Best single rotation for the band");
            result.Tables[1].Rows.Should().HaveCount(6, "one rotation for each band and gear tier");
            result.Tables[1].Rows.Should().OnlyContain(row => row[2].Contains("stoke") || row[2].Contains("press") || row[2].Contains("polish"));

            foreach (var row in result.Tables[0].Rows)
            {
                var adaptive = double.Parse(row[3]);
                var singleBest = double.Parse(row[4]);
                double.Parse(row[5]).Should().BeApproximately(adaptive - singleBest, 0.11, "the margin is the difference of the two shown figures");
                adaptive.Should().BeInRange(0, 100);
            }
        }

        [Test]
        public void PassesWhenEveryRecipeNeedsItsOwnRotationEvenWithNoRandomness()
        {
            // No dice and the whole state shown: each recipe has a fixed rotation that is as good as adaptive play, and no single rotation
            // serves all three recipes of a band. The old per-recipe gate could only fail this; the gate is about one rotation winning everywhere.
            var result = Evaluate("rotation", new ComboScenario(), Context(Levels, runs: 6, perLevel: 3), recipesPerBand: 3);

            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            RowVerdicts(result).Should().HaveCount(6).And.OnlyContain(verdict => verdict == "Passed");
            foreach (var row in result.Tables[0].Rows)
            {
                row[2].Should().Be("3");
                double.Parse(row[3]).Should().BeApproximately(100, 0.01, "adaptive play picks each recipe's favored action");
                double.Parse(row[4]).Should().BeApproximately(46.7, 0.1, "ten actions that favor one recipe in ten and give the others a fifth, whatever the order");
                double.Parse(row[5]).Should().BeApproximately(53.3, 0.1);
                row[6].Should().Be(">= 10");
                double.Parse(row[7]).Should().BeApproximately(100, 0.01, "the best rotation of each recipe alone matches adaptive play: reported, not gated");
            }

            result.Summary.Should().Contain("smallest margin 53.3").And.Contain("a reported metric");
        }

        [Test]
        public void ReportsEachRecipesOwnBestRotationAndWhatTheSingleRotationScoresOnIt()
        {
            var result = Evaluate("rotation", new ComboScenario(), Context(Levels, runs: 6, perLevel: 3), recipesPerBand: 3);

            var table = result.Tables[2];
            table.Header.Should().Equal(
                "Band", "Gear", "Recipe", "Best rotation for this recipe alone (reported)", "Its hold-out", "The single rotation on this recipe", "Adaptive on this recipe");
            table.Rows.Should().HaveCount(18, "three recipes in each of two bands at three gear tiers");
            table.Rows.Should().OnlyContain(row => double.Parse(row[4]) > 99.99 && double.Parse(row[6]) > 99.99);
            foreach (var group in table.Rows.GroupBy(row => row[0] + "/" + row[1]))
            {
                group.Select(row => double.Parse(row[5])).Average().Should().BeApproximately(46.7, 0.2, "one rotation cannot favor all three recipes");
                group.Select(row => row[3]).Distinct().Should().HaveCount(3, "each recipe's own best rotation is a different one");
            }
        }

        [Test]
        public void FailsWhenOneRotationIsTheBestForEveryRecipeEvenWithNoRandomness()
        {
            var result = Evaluate("rotation", new ComboScenario { SameFavoriteEverywhere = true }, Context(Levels, runs: 6, perLevel: 3), recipesPerBand: 3);

            result.Verdict.Should().Be(GateVerdict.Failed);
            RowVerdicts(result).Should().OnlyContain(verdict => verdict == "Failed");
            result.Tables[0].Rows.Should().OnlyContain(row => Math.Abs(double.Parse(row[5])) < 0.01, "the same sequence scores what adaptive play scores on every recipe");
            result.Tables[1].Rows.Should().OnlyContain(row => row[2].Contains("red"), "the favored action repeated is the single rotation that wins everywhere");
            result.Summary.Should().Contain("One fixed rotation comes within 10 points").And.Contain("smallest margin 0");
        }

        [Test]
        public void FailsWhenASingleFixedRotationDoesAsWellAsAdaptivePlay()
        {
            var result = Evaluate("rotation", new BenchScenario { Dominant = true }, Context(Levels));

            result.Verdict.Should().Be(GateVerdict.Failed);
            RowVerdicts(result).Should().OnlyContain(verdict => verdict == "Failed");
            result.Tables[0].Rows.Should().OnlyContain(row => Math.Abs(double.Parse(row[5])) < 1.0, "the dominant rotation scores exactly what adaptive play scores");
            result.Tables[1].Rows.Should().OnlyContain(row => row[2].Contains("perfect"), "the solver found the dominant action");
            result.Summary.Should().Contain("11-20 / no gear").And.Contain("smallest margin 0");
        }

        [Test]
        public void TheMarginIsTheNamedThreshold()
        {
            var strict = GateThresholds.Default with { RotationMarginPoints = 99 };

            var result = Evaluate("rotation", new BenchScenario(), Context(Levels), thresholds: strict);

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Tables[0].Rows.Should().OnlyContain(row => row[6] == ">= 99");
        }

        [Test]
        public void ASeedRepeatsTheResultsExactlyAndAnotherSeedChangesThem()
        {
            GateResult Run(int seed) => Evaluate("rotation", new BenchScenario(), Context(new[] { 15 }, runs: 16, seed: seed));

            var first = Run(3);
            var second = Run(3);
            var other = Run(4);

            second.Tables[0].Rows.Select(r => string.Join("|", r)).Should().Equal(first.Tables[0].Rows.Select(r => string.Join("|", r)));
            second.Tables[1].Rows.Select(r => string.Join("|", r)).Should().Equal(first.Tables[1].Rows.Select(r => string.Join("|", r)));
            second.Tables[2].Rows.Select(r => string.Join("|", r)).Should().Equal(first.Tables[2].Rows.Select(r => string.Join("|", r)));
            other.Tables[0].Rows.Select(r => string.Join("|", r)).Should().NotEqual(first.Tables[0].Rows.Select(r => string.Join("|", r)));
        }

        [Test]
        public void IsNotRunWhenTheScenarioHasNoRecipes()
        {
            var empty = new GateContext(RecipeCatalog.From(Array.Empty<RecipeEntry>()), SharedData.Gear, 10, 1, AttributeSweep.Default);

            var result = Evaluate("rotation", new BenchScenario(), empty);

            result.Verdict.Should().Be(GateVerdict.NotRun);
        }
    }
}
