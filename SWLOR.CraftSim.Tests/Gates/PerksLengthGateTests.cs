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
    public class PerksOptionalGateTests
    {
        private static readonly int[] AtLevel = { 45, 52 };

        [Test]
        public void PassesWhenAnUnperkedRankFiftyCrafterWithTheBestGearUsuallyReachesMaximumQuality()
        {
            var result = Evaluate("perks", new BenchScenario(), Context(AtLevel));

            result.Gate.Should().Be(GateNames.PerksOptional);
            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            result.Tables[0].Rows.Select(row => row[0]).Should().Equal("41-45", "51-53");
            RowVerdicts(result).Should().OnlyContain(verdict => verdict == "Passed");
            result.Tables[0].Title.Should().Contain("Rank 50").And.Contain("best gear").And.Contain("no perks");
        }

        [Test]
        public void FailsWhenMaximumQualityIsOutOfReach()
        {
            var impossible = new BenchScenario { QualityDemand = 400 };

            var result = Evaluate("perks", impossible, Context(AtLevel));

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("41-45").And.Contain("51-53");
            result.Tables[0].Rows.Should().OnlyContain(row => row.Last() == "Failed" && row[6] == ">= 50%");
        }

        [Test]
        public void TheRequiredShareIsTheNamedThreshold()
        {
            var easy = GateThresholds.Default with { PerksOptionalMinimumFullQualityShare = 0.0 };

            var result = Evaluate("perks", new BenchScenario { QualityDemand = 400 }, Context(AtLevel), thresholds: easy);

            result.Verdict.Should().Be(GateVerdict.Passed);
        }

        [Test]
        public void PlaysTheRankTheThresholdNames()
        {
            var lowRank = GateThresholds.Default with { PerksOptionalRank = 10 };

            var result = Evaluate("perks", new BenchScenario(), Context(AtLevel), thresholds: lowRank);

            result.Tables[0].Title.Should().Contain("Rank 10");
        }

        [Test]
        public void IsNotRunWhenNoRecipeIsAtLevelForRankFifty()
        {
            var result = Evaluate("perks", new BenchScenario(), Context(new[] { 5, 15 }));

            result.Verdict.Should().Be(GateVerdict.NotRun);
        }

        [Test]
        public void TheAtLevelBandsAreAThreshold()
        {
            var bands = GateThresholds.Default with { PerksOptionalBands = new[] { new RecipeBand("10-20", 10, 20) } };

            var result = Evaluate("perks", new BenchScenario(), Context(new[] { 15 }), thresholds: bands);

            result.Tables[0].Rows.Select(row => row[0]).Should().Equal("10-20");
        }
    }

    [TestFixture]
    public class LengthGateTests
    {
        private static readonly int[] Levels = { 15, 45 };

        [Test]
        public void PassesWhenEveryBandAndGearTierTakesTheTargetNumberOfActions()
        {
            var result = Evaluate("length", new BenchScenario(), Context(Levels));

            result.Gate.Should().Be(GateNames.Length);
            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            RowVerdicts(result).Should().HaveCount(6).And.OnlyContain(verdict => verdict == "Passed");
            result.Tables[0].Rows.Select(row => double.Parse(row[3])).Should().OnlyContain(actions => actions >= 10 && actions <= 20);
            result.Tables[0].Rows.Should().OnlyContain(row => row[7] == "10-20" && row[6] == "0", "no attempt stalls");
        }

        [Test]
        public void FailsWhenTheScenariosTargetIsNotMet()
        {
            var longer = new BenchScenario { LengthTarget = new LengthTarget(30, 40, "a long craft") };

            var result = Evaluate("length", longer, Context(Levels));

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("30-40 actions (a long craft)").And.Contain("11-20 / no gear");
            result.Tables[0].Title.Should().Contain("a long craft");
        }

        [Test]
        public void APerkedBuildKeepsAFloorOfEightActionsWhereTheUnperkedFloorIsTen()
        {
            var actions = Evaluate("length", new BenchScenario(), Context(Levels)).Tables[0].Rows.Select(row => double.Parse(row[3])).ToList();
            var floorAboveTheBench = new LengthTarget(actions.Max() + 0.5, 20, "a floor above the bench's length");

            Evaluate("length", new BenchScenario { LengthTarget = floorAboveTheBench }, Context(Levels)).Verdict.Should().Be(GateVerdict.Failed);
            var perked = Evaluate("length", new BenchScenario { LengthTarget = LengthTarget.PerkedItemCraft }, Context(Levels));
            perked.Verdict.Should().Be(GateVerdict.Passed, perked.Summary);
            perked.Tables[0].Rows.Should().OnlyContain(row => row[7] == "8-20");
            perked.Tables[0].Title.Should().Contain("perked build");
            LengthTarget.PerkedItemCraft.MinimumActions.Should().Be(8);
            LengthTarget.PerkedItemCraft.MaximumActions.Should().Be(LengthTarget.ItemCraft.MaximumActions);
            LengthTarget.PerkedItemCraft.Contains(8.0).Should().BeTrue();
            LengthTarget.PerkedItemCraft.Contains(7.9).Should().BeFalse();
            LengthTarget.ItemCraft.Contains(9.9).Should().BeFalse("an unperked crafter keeps the floor of 10");
        }

        [Test]
        public void ReportsTheShortestAndLongestRecipeOfEachBand()
        {
            var result = Evaluate("length", new BenchScenario(), Context(Levels), recipesPerBand: 2);

            foreach (var row in result.Tables[0].Rows)
            {
                double.Parse(row[4]).Should().BeLessThanOrEqualTo(double.Parse(row[3]) + 0.05);
                double.Parse(row[5]).Should().BeGreaterThanOrEqualTo(double.Parse(row[3]) - 0.05);
            }
        }

        [Test]
        public void CountsAnAttemptThatNeverEndsAsStalled()
        {
            // With no CP and a free Press that only fills progress slowly outside the window, nothing here stalls; a limit of 3 actions makes every attempt stall.
            var thresholds = GateThresholds.Default with { ActionLimit = 3 };

            var result = Evaluate("length", new BenchScenario(), Context(Levels), thresholds: thresholds);

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Tables[0].Rows.Should().OnlyContain(row => row[6] == "20", "all 20 attempts of the single recipe stop at the action limit");
        }
    }
}
