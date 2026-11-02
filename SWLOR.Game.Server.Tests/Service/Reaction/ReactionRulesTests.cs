using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

namespace SWLOR.Game.Server.Tests.Service.Reaction;

/// <summary>Covers the rank-only stand-ins for Espionage's missing crafting gear, and the settings built from them.</summary>
public class ReactionRulesTests
{
    private static ReactionSettings Settings(int rank, int level, int intensity = 2) =>
        new(new ReactionParameters(ReactionCyclePattern.Brew, 6, intensity), rank, level);

    [TestCase(0, 0)]
    [TestCase(10, 6)]
    [TestCase(25, 15)]
    [TestCase(50, 30)]
    public void CraftsmanshipAndControlComeFromRankAlone(int rank, int expected)
    {
        ReactionRules.EquivalentCraftsmanship(rank).Should().Be(expected);
        ReactionRules.EquivalentControl(rank).Should().Be(expected);
    }

    [Test]
    public void ABestGearCrafterOfRank50CarriesTheBestGearTierStats()
    {
        // The legacy baseline puts best-tier gear at Control 29 and Craftsmanship 30.
        ReactionRules.EquivalentCraftsmanship(50).Should().BeInRange(29, 30);
    }

    [TestCase(0, 0)]
    [TestCase(1, 0)]
    [TestCase(4, 3)]
    [TestCase(24, 18)]
    [TestCase(25, 18)]
    [TestCase(50, 37)]
    public void CpIsThreeQuartersOfRank_WithNoGearCpAndNoRank25Passive(int rank, int expected)
    {
        ReactionRules.MaxCp(rank).Should().Be(expected);
        ReactionRules.CpPassive.Should().Be(0, "the simulator tuned the Reaction's rank-25 passive to nothing");
    }

    [Test]
    public void StabilizeCostsThreeCpPerFiveRecipeLevels_AndAMinimumRankCrafterCanAlwaysAffordOne()
    {
        ReactionRules.StabilizeCp(3).Should().Be(ReactionRules.MinimumStabilizeCp);
        ReactionRules.StabilizeCp(16).Should().Be(9);
        ReactionRules.StabilizeCp(50).Should().Be(30);

        foreach (var level in new[] { 16, 18, 26, 30, 36, 45, 46, 50 })
        {
            var cost = ReactionRules.StabilizeCp(level);

            ReactionRules.MaxCp(level).Should().BeGreaterThanOrEqualTo(cost, $"level {level} at its level");
            ReactionRules.MaxCp(level - 3).Should().BeGreaterThanOrEqualTo(cost, $"level {level} at its minimum rank");
            ReactionRules.MaxCp(level).Should().BeLessThan(cost * 2, $"level {level}: one Stabilize, not two");
        }
    }

    [Test]
    public void ActionWearRisesWithPeakIntensity_SoTheBudgetStaysTightWhereCpIsPlentiful()
    {
        var wear = Enumerable.Range(1, 5).Select(ReactionRules.ActionStability).ToList();

        wear.Should().BeInAscendingOrder();
        wear.First().Should().BeLessThan(wear.Last());
        ReactionRules.ActionStability(0).Should().Be(wear.First(), "an out-of-range intensity is held to the table");
        ReactionRules.ActionStability(9).Should().Be(wear.Last());
        ReactionRules.HeavyChargeStability.Should().BeGreaterThan(wear.Last() * 2, "a Charge outside Calm costs far more than any other action");
    }

    [Test]
    public void TheXpMultiplierIsOneNamedValuePerBand_AndOnlyTheLowestBandPaysMore()
    {
        ReactionRules.LowLevelXpMultiplier.Should().BeGreaterThan(1.0);
        for (var level = 1; level <= ReactionRules.XpMultiplierLastLevel; level++)
        {
            ReactionRules.XpMultiplier(level).Should().Be(ReactionRules.LowLevelXpMultiplier, $"level {level}");
            CraftMinigameXp.MultiplierFor(CraftMinigameType.Reaction, level).Should().Be(ReactionRules.LowLevelXpMultiplier, $"level {level}");
        }

        for (var level = ReactionRules.XpMultiplierLastLevel + 1; level <= 53; level++)
        {
            ReactionRules.XpMultiplier(level).Should().Be(1.0, $"level {level} already pays at least the old XP per hour");
            CraftMinigameXp.MultiplierFor(CraftMinigameType.Reaction, level).Should().Be(1.0, $"level {level}");
        }
    }

    [Test]
    public void DampenCostsAQuarterOfStabilizesCp_SoACrafterAtTheRecipesLevelHasOneToSpare()
    {
        ReactionRules.DampenRestore.Should().BePositive().And.BeLessThan(ReactionRules.StabilizeRestore);
        ReactionRules.DampenCp(1).Should().Be(ReactionRules.MinimumDampenCp);

        var costs = Enumerable.Range(1, 50).Select(ReactionRules.DampenCp).ToList();
        costs.Should().BeInAscendingOrder();
        for (var level = ReactionRules.StabilizeUnlockRank + 1; level <= 50; level++)
        {
            ReactionRules.DampenCp(level).Should().BeLessThan(ReactionRules.StabilizeCp(level), $"level {level}");
            ReactionRules.MaxCp(level).Should().BeGreaterThanOrEqualTo(
                ReactionRules.StabilizeCp(level) + ReactionRules.DampenCp(level), $"level {level}: one Stabilize and one Dampen");
        }
    }

    [Test]
    public void ProgressFactor_RisesFivePercentPerRankAboveTheRecipe_UpToAQuarter()
    {
        ReactionRules.ProgressFactorPercent(30, 30).Should().Be(100);
        ReactionRules.ProgressFactorPercent(32, 30).Should().Be(110);
        ReactionRules.ProgressFactorPercent(50, 30).Should().Be(ReactionRules.MaximumProgressFactorPercent);
    }

    [Test]
    public void ProgressFactor_FallsFivePercentPerRankBelowTheRecipe_DownToHalf()
    {
        ReactionRules.ProgressFactorPercent(27, 30).Should().Be(85);
        ReactionRules.ProgressFactorPercent(0, 40).Should().Be(ReactionRules.MinimumFactorPercent);
    }

    [Test]
    public void QualityFactor_IsReducedBelowTheRecipeLevelAndNeverRaisedAboveIt()
    {
        ReactionRules.QualityFactorPercent(30, 30).Should().Be(100);
        ReactionRules.QualityFactorPercent(50, 30).Should().Be(100);
        ReactionRules.QualityFactorPercent(27, 30).Should().Be(85);
        ReactionRules.QualityFactorPercent(0, 40).Should().Be(ReactionRules.MinimumFactorPercent);
    }

    [Test]
    public void ScaleGain_NeverFallsBelowOne()
    {
        ReactionRules.ScaleGain(1, 0, ReactionRules.MinimumFactorPercent).Should().Be(1);
        ReactionRules.ScaleGain(0, 0, 100).Should().Be(1);
    }

    [Test]
    public void ScaleGain_RaisesTheBaseByOnePercentPerStatPointAndRoundsDown()
    {
        ReactionRules.ScaleGain(100, 30, 100).Should().Be(130);
        ReactionRules.ScaleGain(14, 18, 100).Should().Be(16);
        ReactionRules.ScaleGain(100, 0, 115).Should().Be(115, "whole-number arithmetic has no float rounding");
    }

    [Test]
    public void PeakChargeStability_RisesWithIntensity()
    {
        var costs = Enumerable.Range(1, 5).Select(ReactionRules.PeakChargeStability).ToList();

        costs.Should().BeInAscendingOrder();
        costs.Distinct().Should().HaveCount(5);
        costs[0].Should().BeGreaterThan(ReactionRules.HeavyChargeStability);
    }

    [Test]
    public void TheSettingsHigherRankGivesNeverWeakenAnAction()
    {
        var previous = Settings(0, 30);
        for (var rank = 1; rank <= 50; rank++)
        {
            var current = Settings(rank, 30);

            current.ChargePotency.Should().BeGreaterThanOrEqualTo(previous.ChargePotency, $"rank {rank}");
            current.MeasurePotency.Should().BeGreaterThanOrEqualTo(previous.MeasurePotency, $"rank {rank}");
            current.RefineSubtlety.Should().BeGreaterThanOrEqualTo(previous.RefineSubtlety, $"rank {rank}");
            current.MaxCp.Should().BeGreaterThanOrEqualTo(previous.MaxCp, $"rank {rank}");
            previous = current;
        }
    }

    [Test]
    public void TheSettingsCarryTheRankAndRecipeInputs()
    {
        var settings = Settings(30, 26, intensity: 4);

        settings.Rank.Should().Be(30);
        settings.RecipeLevel.Should().Be(26);
        settings.Craftsmanship.Should().Be(18);
        settings.Control.Should().Be(18);
        settings.MaxCp.Should().Be(ReactionRules.MaxCp(30));
        settings.PeakIntensity.Should().Be(4);
        settings.PeakChargeStability.Should().Be(ReactionRules.PeakChargeStability(4));
        settings.Cycle.Pattern.Should().Be(ReactionCyclePattern.Brew);
        settings.Cycle.Length.Should().Be(6);
    }

    [Test]
    public void ANegativeRankIsTreatedAsRankZero()
    {
        Settings(-5, 10).MaxCp.Should().Be(0);
        Settings(-5, 10).Rank.Should().Be(0);
    }

    [TestCase(0)]
    [TestCase(6)]
    public void PeakIntensityOutsideOneToFiveIsRefused(int intensity)
    {
        var act = () => Settings(10, 10, intensity);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void SettingsForARecipeWithoutReactionParametersAreRefused()
    {
        var act = () => ReactionSettings.ForRecipe(new RecipeDetail { Level = 10 }, 10);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Reaction parameters*");
    }

    [Test]
    public void SettingsForARecipeReadItsLevelAndParameters()
    {
        var recipe = new RecipeDetail
        {
            Level = 26,
            MinigameParameters = new ReactionParameters(ReactionCyclePattern.Trigger, 5, 3)
        };

        var settings = ReactionSettings.ForRecipe(recipe, 28);

        settings.RecipeLevel.Should().Be(26);
        settings.Rank.Should().Be(28);
        settings.PeakIntensity.Should().Be(3);
        settings.Cycle.Pattern.Should().Be(ReactionCyclePattern.Trigger);
    }
}
