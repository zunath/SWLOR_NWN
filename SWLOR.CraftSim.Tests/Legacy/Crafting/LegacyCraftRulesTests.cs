using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Legacy.Crafting;

namespace SWLOR.CraftSim.Tests.Legacy.Crafting
{
    /// <summary>Known values worked by hand from CraftViewModel.cs, float arithmetic and truncation included.</summary>
    [TestFixture]
    public class LegacyCraftRulesTests
    {
        [TestCase(0, 0, 0)]
        [TestCase(0, 24, 18)]
        [TestCase(0, 25, 49)]
        [TestCase(23, 25, 72)]
        [TestCase(37, 50, 105)]
        public void MaxCp_AddsRankAndTheRank25Passive(int cpBonus, int rank, int expected)
        {
            LegacyCraftRules.MaxCp(cpBonus, rank).Should().Be(expected);
        }

        [TestCase(19, 1, 1, 19)]
        [TestCase(21, 1, 4, 36)]
        [TestCase(100, 5, 10, 225)]
        [TestCase(102, 33, 30, 117)]
        [TestCase(102, 40, 30, 127)]
        public void MaxProgress_RaisesThePieceForEitherSignOfTheRankDifference(int levelProgress, int rank, int level, int expected)
        {
            LegacyCraftRules.MaxProgress(levelProgress, rank, level).Should().Be(expected);
        }

        [Test]
        public void MaxProgress_BelowTheRecipeIsNotCapped()
        {
            // The cap tests a negative adjustment against +2.0, so 25% per level keeps growing past +200%.
            LegacyCraftRules.MaxProgress(100, 0, 20).Should().Be(600);
        }

        [TestCase(10, 0, 19, 19, 10)]
        [TestCase(10, 0, 20, 20, 31)]
        [TestCase(80, 30, 30, 30, 120)]
        [TestCase(10, 0, 17, 20, 8)]
        [TestCase(10, 0, 25, 20, 38)]
        public void Progress_AddsTheRank20PassiveAndCraftsmanshipThenScalesByTheRankDifference(
            int baseProgress, int craftsmanship, int rank, int level, int expected)
        {
            LegacyCraftRules.Progress(baseProgress, craftsmanship, rank, level).Should().Be(expected);
        }

        [TestCase(10, 0, 39, 39, 10)]
        [TestCase(10, 29, 40, 40, 146)]
        [TestCase(10, 29, 45, 40, 146)]
        [TestCase(80, 20, 37, 40, 80)]
        public void Quality_AddsTheRank40PassiveAndOnlyPenalisesAShortfall(
            int baseQuality, int control, int rank, int level, int expected)
        {
            LegacyCraftRules.Quality(baseQuality, control, rank, level).Should().Be(expected);
        }

        [TestCase(1, 0)]
        [TestCase(3, 0)]
        [TestCase(4, 1)]
        [TestCase(53, 50)]
        public void RequiredRank_IsTheLevelMinusThreeAndNeverNegative(int level, int expected)
        {
            LegacyCraftRules.RequiredRank(level).Should().Be(expected);
        }

        [Test]
        public void StacksPerSuccess_CountsEachFailureAtTheLossChance()
        {
            LegacyCraftRules.StacksPerSuccess(3, 1.0).Should().BeApproximately(3, 1e-9);
            LegacyCraftRules.StacksPerSuccess(3, 0.5).Should().BeApproximately(3 * (1 + 0.65), 1e-9);
            LegacyCraftRules.StacksPerSuccess(2, 0.25).Should().BeApproximately(2 * (1 + 0.65 * 3), 1e-9);
            LegacyCraftRules.StacksPerSuccess(3, 0).Should().Be(double.PositiveInfinity);
        }

        [Test]
        public void Context_ReadsDurabilityProgressAndQualityFromTheServersLevelChart()
        {
            var context = new LegacyCraftContext(50, 50, 29, 30, 37);

            context.MaxDurability.Should().Be(80);
            context.MaxProgress.Should().Be(186);
            context.MaxQuality.Should().Be(2641);
            context.MaxCp.Should().Be(105);
            context.ProgressOf(LegacyAction.BasicSynthesis).Should().Be(50);
            context.ProgressOf(LegacyAction.CarefulSynthesis).Should().Be(120);
            context.QualityOf(LegacyAction.BasicTouch).Should().Be(146);
        }

        [Test]
        public void Context_AtLevelOne_UsesTheFirstChartRow()
        {
            var context = new LegacyCraftContext(1, 1, 0, 0, 0);

            context.MaxDurability.Should().Be(60);
            context.MaxProgress.Should().Be(19);
            context.MaxQuality.Should().Be(312);
        }
    }
}
