using NUnit.Framework;
using SWLOR.Game.Server.Feature.RecipeDefinition.EspionageRecipeDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

public class CraftTests
{
    [Test]
    public void StarterPoison_IsAvailableAtRankZeroWithoutARecipeUnlock()
    {
        var recipe = new VenomCoatingRecipes().BuildRecipes()[RecipeType.VenomCoating1];

        Assert.That(recipe.IsActive, Is.True);
        Assert.That(recipe.Skill, Is.EqualTo(SkillType.Espionage));
        Assert.That(Craft.GetRequiredSkillRankForRecipe(recipe), Is.Zero);
        Assert.That(recipe.Requirements, Has.Count.EqualTo(1));
        Assert.That(recipe.Requirements[0], Is.TypeOf<RecipePerkRequirement>());
        Assert.That(recipe.Requirements[0].RequirementText, Is.EqualTo("Requires Poisoncraft 1."));
    }

    [Test]
    public void StarterPoison_AwardsXpUntilPoisoncraftTwoUnlocksAtRankFifteen()
    {
        var recipe = new VenomCoatingRecipes().BuildRecipes()[RecipeType.VenomCoating1];

        for (var rank = 0; rank < 15; rank++)
        {
            Assert.That(Craft.GetRequiredSkillRankForRecipe(recipe), Is.LessThanOrEqualTo(rank));
            Assert.That(Craft.GetBaseRecipeXP(recipe, rank), Is.GreaterThan(0),
                $"Poisoncraft must provide a progression path at Espionage rank {rank}.");
        }
        Assert.That(Craft.GetBaseRecipeXP(recipe, 15), Is.Zero);
    }

    [Test]
    public void RecipesWithoutPracticeLimits_KeepTheirExistingXpCurve()
    {
        var recipe = new RecipeDetail { Level = 10 };
        for (var rank = 0; rank <= 50; rank++)
            Assert.That(Craft.GetBaseRecipeXP(recipe, rank), Is.EqualTo(Skill.GetDeltaXP(recipe.Level - rank)));
    }

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
