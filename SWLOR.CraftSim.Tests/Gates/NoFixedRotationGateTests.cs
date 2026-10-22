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

            result.Tables.Should().HaveCount(2);
            result.Tables[0].Header.Should().Equal("Band", "Gear", "Recipes played", "Adaptive avg quality", "Best fixed avg quality", "Margin", "Required", "Verdict");
            result.Tables[0].Rows.Select(row => row[0] + "/" + row[1]).Should().Equal(
                "11-20/no gear", "11-20/mid gear", "11-20/best gear", "41-53/no gear", "41-53/mid gear", "41-53/best gear");
            result.Tables[1].Header.Should().Contain("Best fixed rotation");
            result.Tables[1].Rows.Should().OnlyContain(row => row[3].Contains("stoke") || row[3].Contains("press") || row[3].Contains("polish"));

            foreach (var row in result.Tables[0].Rows)
            {
                var adaptive = double.Parse(row[3]);
                var fixedBest = double.Parse(row[4]);
                double.Parse(row[5]).Should().BeApproximately(adaptive - fixedBest, 0.11, "the margin is the difference of the two shown figures");
                adaptive.Should().BeInRange(0, 100);
            }
        }

        [Test]
        public void FailsWhenASingleFixedRotationDoesAsWellAsAdaptivePlay()
        {
            var result = Evaluate("rotation", new BenchScenario { Dominant = true }, Context(Levels));

            result.Verdict.Should().Be(GateVerdict.Failed);
            RowVerdicts(result).Should().OnlyContain(verdict => verdict == "Failed");
            result.Tables[0].Rows.Should().OnlyContain(row => Math.Abs(double.Parse(row[5])) < 1.0, "the dominant rotation scores exactly what adaptive play scores");
            result.Tables[1].Rows.Should().OnlyContain(row => row[3].Contains("perfect"), "the solver found the dominant action");
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
