using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;
using SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Covers the multiplier a minigame puts on its crafts' base XP, which keeps its XP per hour at the old window's.</summary>
public class CraftMinigameXpTests
{
    [Test]
    public void OnlyAMinigameThatNeedsOneHasAMultiplier_AndOnlyForTheRecipeLevelsThatNeedIt()
    {
        foreach (var minigame in Enum.GetValues<CraftMinigameType>().Where(minigame => minigame is not (CraftMinigameType.Reaction or CraftMinigameType.Fabricator or CraftMinigameType.Circuit or CraftMinigameType.Forge)))
        {
            foreach (var level in new[] { 1, 10, 11, 50 })
                CraftMinigameXp.MultiplierFor(minigame, level).Should().Be(1.0, $"{minigame} level {level}");
        }

        CraftMinigameXp.MultiplierFor(CraftMinigameType.Reaction, 1).Should().BeGreaterThan(1.0);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Reaction, ReactionRules.XpMultiplierLastLevel).Should().BeGreaterThan(1.0);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Reaction, ReactionRules.XpMultiplierLastLevel + 1).Should().Be(1.0);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Reaction, 50).Should().Be(1.0);
    }

    [Test]
    public void TheForgesMultiplierDiffersByRecipeLevelBand_AndOnlyEverLiftsTheXp()
    {
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Forge, 5).Should().Be(ForgeRules.XpMultiplierLevels1To10);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Forge, 10).Should().Be(ForgeRules.XpMultiplierLevels1To10);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Forge, 11).Should().Be(ForgeRules.XpMultiplierLevels11To20);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Forge, 30).Should().Be(ForgeRules.XpMultiplierLevels21To30);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Forge, 40).Should().Be(ForgeRules.XpMultiplierLevels31To40);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Forge, 53).Should().Be(ForgeRules.XpMultiplierLevels41Up);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Forge).Should().Be(ForgeRules.XpMultiplierLevels1To10, "no level takes the lowest band");
        new[]
        {
            ForgeRules.XpMultiplierLevels1To10, ForgeRules.XpMultiplierLevels11To20, ForgeRules.XpMultiplierLevels21To30,
            ForgeRules.XpMultiplierLevels31To40, ForgeRules.XpMultiplierLevels41Up
        }.Should().OnlyContain(multiplier => multiplier >= 1.0);
    }

    [TestCase(10, 2.5, 25)]
    [TestCase(5, 1.5, 8)]
    [TestCase(150, 1.0, 150)]
    [TestCase(0, 3.0, 0)]
    public void ScaleBaseXpRoundsToTheNearestWholeXp(int baseXp, double multiplier, int expected)
    {
        CraftMinigameXp.ScaleBaseXp(baseXp, multiplier).Should().Be(expected);
    }

    [Test]
    public void ARecipeWhoseSkillHasNoMinigameKeepsTodaysXp()
    {
        var recipe = new RecipeDetail { Skill = SkillType.Armor, Level = 20 };

        CraftMinigameRouting.GetRoute(SkillType.Armor).IsLegacy.Should().BeTrue();
        CraftMinigameXp.BaseXp(recipe, 15).Should().Be(Craft.GetBaseRecipeXP(recipe, 15));
    }
}
