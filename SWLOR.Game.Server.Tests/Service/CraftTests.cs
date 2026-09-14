using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Tests.Service;

public class CraftTests
{
    [TestCase(40, 37)]
    [TestCase(10, 7)]
    [TestCase(3, 0)]
    [TestCase(2, 0)]
    [TestCase(0, 0)]
    public void GetRequiredSkillRankForRecipe_UnlocksThreeRanksBelowRecipeLevel(
        int recipeLevel,
        int expectedSkillRank)
    {
        var recipe = new RecipeDetail { Level = recipeLevel };

        var requiredSkillRank = Craft.GetRequiredSkillRankForRecipe(recipe);

        Assert.That(requiredSkillRank, Is.EqualTo(expectedSkillRank));
    }
}
