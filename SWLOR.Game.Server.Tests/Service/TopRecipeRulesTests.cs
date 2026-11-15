using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class TopRecipeRulesTests
{
    [TestCase(50)]
    [TestCase(45)]
    public void ControlCountsInFullAtOrdinaryLevels(int recipeLevel)
    {
        TopRecipeRules.EffectiveControl(189, recipeLevel).Should().Be(189);
    }

    [TestCase(0, 0)]
    [TestCase(119, 119)]
    [TestCase(120, 120)]
    [TestCase(160, 130)]
    [TestCase(200, 140)]
    public void ControlAboveTheSoftCapCountsAQuarterOnTopRecipes(int control, double expected)
    {
        TopRecipeRules.EffectiveControl(control, 52).Should().Be(expected);
    }

    [Test]
    public void TheCircuitIsHarderOnlyOnTopRecipes()
    {
        var ordinary = 1.0 + CircuitRules.CalibrationDifficultyPerLevel * (52 - CircuitRules.DifficultyStartLevel);
        CircuitRules.CalibrationDifficulty(52).Should().BeApproximately(ordinary * CircuitRules.TopRecipeDifficultyFactor, 0.0001);
        CircuitRules.CalibrationDifficulty(50).Should().BeApproximately(1.0 + CircuitRules.CalibrationDifficultyPerLevel * 20, 0.0001);

        var ordinaryIntegrity = 1.0 + CircuitRules.IntegrityDifficultyPerLevel * (52 - CircuitRules.DifficultyStartLevel);
        CircuitRules.IntegrityDifficulty(52).Should().BeApproximately(ordinaryIntegrity * CircuitRules.TopRecipeDifficultyFactor, 0.0001);
        CircuitRules.IntegrityDifficulty(50).Should().BeApproximately(1.0 + CircuitRules.IntegrityDifficultyPerLevel * 20, 0.0001);
    }

    [Test]
    public void BuilderPerkBonusesAreWorthAQuarterOnTopRecipes()
    {
        var perks = new FabricatorPerkStats(RunPowerPercent: 20, ServiceCpReduction: 4, DualFeedUnlocked: true, PrimeWindowBonusActions: 1);

        perks.ForRecipeLevel(50).Should().Be(perks);
        perks.ForRecipeLevel(52).Should().Be(new FabricatorPerkStats(RunPowerPercent: 5, ServiceCpReduction: 1, DualFeedUnlocked: true, PrimeWindowBonusActions: 1));
    }

    [Test]
    public void TheKitchenGivesLessDonenessOnlyOnTopRecipes()
    {
        var stats = new KitchenStats(50, 0, 0, 0);

        stats.Doneness(100, 50).Should().Be(100);
        stats.Doneness(100, 51).Should().Be((int)System.Math.Round(100 * stats.LevelFactor(51) * KitchenTuning.TopRecipeDonenessFactor, System.MidpointRounding.AwayFromZero));
    }
}
