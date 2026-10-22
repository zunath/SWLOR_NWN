using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Covers the multiplier a minigame puts on its crafts' base XP, which keeps its XP per hour at the old window's.</summary>
public class CraftMinigameXpTests
{
    [Test]
    public void OnlyAMinigameThatNeedsOneHasAMultiplier()
    {
        foreach (var minigame in Enum.GetValues<CraftMinigameType>().Where(minigame => minigame != CraftMinigameType.Reaction))
            CraftMinigameXp.MultiplierFor(minigame).Should().Be(1.0, minigame.ToString());

        CraftMinigameXp.MultiplierFor(CraftMinigameType.Reaction).Should().BeGreaterThan(1.0);
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
