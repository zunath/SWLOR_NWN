using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Legacy.Crafting
{
    /// <summary>The old window's enhancement rules, with values worked by hand from CraftViewModel.cs (float arithmetic and truncation included).</summary>
    [TestFixture]
    public class LegacyEnhancementRulesTests
    {
        [TestCase(45, 40, true, TestName = "An enhancement 5 levels above the recipe is accepted")]
        [TestCase(45, 39, false, TestName = "An enhancement 6 levels above the recipe is refused")]
        [TestCase(5, 50, true, TestName = "A low enhancement is accepted on any higher recipe")]
        [TestCase(50, 45, true, TestName = "Level 50 on a level 45 recipe is accepted")]
        [TestCase(1, 1, true, TestName = "The same level is accepted")]
        public void IsAllowedFor_RefusesAnEnhancementMoreThanFiveLevelsAboveTheRecipe(int enhancementLevel, int recipeLevel, bool expected)
        {
            LegacyEnhancementRules.IsAllowedFor(enhancementLevel, recipeLevel).Should().Be(expected);
        }

        [TestCase(186, 60, 246)]
        [TestCase(186, 180, 366)]
        [TestCase(423, 0, 423)]
        public void MaxProgressWithEnhancements_AddsThePenaltyToTheRankAdjustedMaximum(int maxProgress, int penalty, int expected)
        {
            LegacyEnhancementRules.MaxProgressWithEnhancements(maxProgress, penalty).Should().Be(expected);
        }

        [TestCase(0, 100, 0)]
        [TestCase(100, 100, 100)]
        [TestCase(2641, 2641, 100)]
        [TestCase(2112, 2641, 79)]
        [TestCase(57, 100, 57)]
        [TestCase(53, 100, 52, TestName = "53 of 100 truncates to 52 in float arithmetic, as the old window does")]
        [TestCase(59, 100, 58, TestName = "59 of 100 truncates to 58 in float arithmetic, as the old window does")]
        public void TransferChancePercent_IsTheQualityShareTruncatedInFloats(int quality, int maxQuality, int expected)
        {
            LegacyEnhancementRules.TransferChancePercent(quality, maxQuality).Should().Be(expected);
        }

        [Test]
        public void ExpectedTransfers_IsSlotsTimesTheChance_SoEightyPercentAveragesTwoPointFourOfThree()
        {
            LegacyEnhancementRules.ExpectedTransfers(3, 80).Should().BeApproximately(2.4, 1e-12);
            LegacyEnhancementRules.ExpectedTransfers(1, 80).Should().BeApproximately(0.8, 1e-12);
            LegacyEnhancementRules.ExpectedTransfers(2, 100).Should().Be(2);
            LegacyEnhancementRules.ExpectedTransfers(3, 0).Should().Be(0);
            LegacyEnhancementRules.ExpectedTransfers(3, 250).Should().Be(3, "a chance cannot pass 100%");
        }

        [Test]
        public void TheRollIsAtOrBelowTheChanceSoAChanceOfNPercentTransfersNOutOfAHundredRolls()
        {
            // Random.D100(1) <= propertyTransferChance (CraftViewModel.cs:1455): rolls 1..chance transfer, so exactly `chance` of 100 faces do.
            foreach (var chance in new[] { 0, 1, 53, 99, 100 })
                Enumerable.Range(1, 100).Count(roll => roll <= chance).Should().Be(chance);
        }

        [TestCase(1, 0, 1)]
        [TestCase(2, 1, 3)]
        [TestCase(2, 10, 8)]
        public void Slots_AddTheBlueprintsToTheRecipesUpToEight(int recipeSlots, int blueprintSlots, int expected)
        {
            LegacyEnhancementRules.Slots(recipeSlots, blueprintSlots).Should().Be(expected);
        }

        [Test]
        public void TheEnhancementRulesAreRegisteredWithTheirSources()
        {
            LegacySource.All.Select(r => r.Name).Should().Contain(new[]
            {
                "Enhancement slots", "Enhancement kinds and level limit", "Enhancement progress penalty", "Enhancement transfer chance"
            });
            LegacySource.EnhancementProgressPenalty.Citation.Should().Be("CraftViewModel.cs:696-755");
        }
    }

    [TestFixture]
    public class LegacyEnhancementContextTests
    {
        [Test]
        public void TheContextAddsTheTotalPenaltyToTheRankAdjustedProgressMaximum()
        {
            // Level 51 at rank 50 is one level short: the chart's 339 gets +25%, then the three penalties are added flat.
            var plain = new LegacyCraftContext(51, 50, 29, 30, 37);
            var enhanced = new LegacyCraftContext(51, 50, 29, 30, 37, new CraftEnhancementConfiguration(3, 60));

            plain.MaxProgress.Should().Be(423);
            enhanced.MaxProgress.Should().Be(423 + 180);
            enhanced.MaxQuality.Should().Be(plain.MaxQuality);
            enhanced.MaxDurability.Should().Be(plain.MaxDurability);
            enhanced.MaxCp.Should().Be(plain.MaxCp);
            enhanced.Enhancements.TotalProgressPenalty.Should().Be(180);
            plain.Enhancements.IsNone.Should().BeTrue();
        }

        [Test]
        public void AnEnhancedCraftEndsAtLowerQualityBecauseTheExtraProgressSpendsTheCpThatWouldHaveBeenQuality()
        {
            var gear = SharedData.Gear.Levels(SkillType.Smithery)[^1];
            var plain = LegacyEnhancementBaseline.PlayCell(46, 50, gear, 2, LegacyEnhancementBaseline.LookaheadIndex, CraftEnhancementConfiguration.None, 150, 3);
            var enhanced = LegacyEnhancementBaseline.PlayCell(46, 50, gear, 2, LegacyEnhancementBaseline.LookaheadIndex, new CraftEnhancementConfiguration(3, 60), 150, 3);

            enhanced.MeanQualityOfSuccesses.Should().BeLessThan(plain.MeanQualityOfSuccesses, "the extra progress costs CP and durability that would have gone to quality");
            plain.ExpectedTransfersPerAttempt.Should().Be(0, "a craft with no enhancement has nothing to transfer");
        }

        [Test]
        public void ACellsExpectedTransfersAreSlotsTimesTheChanceOfEveryCraftThatWasMade()
        {
            var gear = SharedData.Gear.Levels(SkillType.Smithery)[^1];

            var cell = LegacyEnhancementBaseline.PlayCell(45, 50, gear, 2, LegacyEnhancementBaseline.LookaheadIndex, new CraftEnhancementConfiguration(2, 60), 80, 5);

            cell.Successes.Should().BeGreaterThan(0);
            cell.TransfersSum.Should().BeApproximately(2 * cell.TransferChanceSum / 100.0, 1e-9);
            cell.ExpectedTransfersPerAttempt.Should().BeApproximately(cell.TransfersSum / 80, 1e-12);
            cell.ExpectedTransfersPerSuccess.Should().BeApproximately(2 * cell.MeanTransferChancePercent / 100.0, 1e-9);
            cell.MeanTransferChancePercent.Should().BeApproximately(cell.MeanQualityOfSuccesses * 100, 1.0, "the chance is the quality share, truncated");
            cell.Policy.Should().Be("Lookahead");
            cell.Qualities.Should().HaveCount(80);
            cell.Succeeded.Count(made => made).Should().Be(cell.Successes);
        }

        [Test]
        public void ASeedRepeatsACellAndEachSeedPlaysDifferentCrafts()
        {
            var gear = SharedData.Gear.Levels(SkillType.Smithery)[^1];
            var config = new CraftEnhancementConfiguration(3, 60);

            LegacyEnhancementCell Cell(int seed) =>
                LegacyEnhancementBaseline.PlayCell(45, 50, gear, 2, LegacyEnhancementBaseline.LookaheadIndex, config, 60, seed);

            Cell(2).TransfersSum.Should().Be(Cell(2).TransfersSum);
            Cell(2).TransfersSum.Should().NotBe(Cell(3).TransfersSum);
        }

        [Test]
        public void TheDominantRotationAndTheLookaheadAreBothPlayable()
        {
            var gear = SharedData.Gear.Levels(SkillType.Smithery)[^1];
            var config = new CraftEnhancementConfiguration(1, 60);

            var rotation = LegacyEnhancementBaseline.PlayCell(45, 50, gear, 2, 0, config, 30, 1);
            var lookahead = LegacyEnhancementBaseline.PlayCell(45, 50, gear, 2, LegacyEnhancementBaseline.LookaheadIndex, config, 30, 1);

            rotation.Policy.Should().Be("Dominant rotation");
            lookahead.Policy.Should().Be("Lookahead");
            LegacyEnhancementBaseline.Policies.Select(p => p.Name).Should().Equal("Dominant rotation", "Lookahead");
        }

        [Test]
        public void XpPerAttemptFollowsTodaysFormulaWithAFailureAtFifteenPercent()
        {
            var cell = new LegacyEnhancementCell
            {
                Runs = 2,
                Succeeded = new[] { true, false },
                Qualities = new[] { 0.5, 0.0 }
            };

            var expected = (CraftOutcomeRules.CalculateXp(100, 0, false, 0.5f) + CraftOutcomeRules.CalculateFailureXp(100, 0)) / 2.0;

            cell.XpPerAttempt(100).Should().Be(expected).And.Be(82.5);
        }

        [Test]
        public void TheResultCarriesTheTruncatedChanceOfAMadeCraft()
        {
            var context = new LegacyCraftContext(1, 50, 29, 30, 37, new CraftEnhancementConfiguration(1, 10));

            var result = LegacyCraftRunner.Play(context, new DominantRotationPolicy(), new SimRandom(4));

            result.Succeeded.Should().BeTrue();
            Math.Abs(result.TransferChancePercent - result.QualityFraction * 100).Should().BeLessThan(1.0 + 1e-6, "the chance is the quality share truncated to a whole percent");
        }

        [Test]
        public void AFailedCraftHasNoTransferChance()
        {
            var context = new LegacyCraftContext(53, 5, 0, 0, 0, new CraftEnhancementConfiguration(3, 100));

            var result = LegacyCraftRunner.Play(context, new DominantRotationPolicy(), new SimRandom(4));

            result.Succeeded.Should().BeFalse();
            result.TransferChancePercent.Should().Be(0);
        }
    }
}
