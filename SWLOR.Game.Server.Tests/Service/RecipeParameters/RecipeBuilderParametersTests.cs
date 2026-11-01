using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

public class RecipeBuilderParametersTests
{
    private sealed class OtherTestParameters : RecipeMinigameParameters
    {
        public override IReadOnlyList<RecipeParameterValue> Values => Array.Empty<RecipeParameterValue>();
    }

    private static readonly TestForgeParameters _forge = new(50, 20, 1.5, TestForgeStyle.Warm);

    [Test]
    public void RecipesWithoutParameters_KeepWorkingUnchanged()
    {
        var recipes = new RecipeBuilder()
            .Create(RecipeType.AdventTunic, SkillType.Smithery)
            .Category(RecipeCategoryType.Tunic)
            .Level(10)
            .Component("ref_veldite", 5)
            .Build();

        var recipe = recipes[RecipeType.AdventTunic];

        Assert.That(recipe.MinigameParameters, Is.Null);
        Assert.That(recipe.Level, Is.EqualTo(10));
        Assert.That(recipe.Components["ref_veldite"], Is.EqualTo(5));
        Assert.That(new RecipeDetail().MinigameParameters, Is.Null);
    }

    [Test]
    public void MinigameParameters_AreStoredOnTheActiveRecipeOnly()
    {
        var recipes = new RecipeBuilder()
            .Create(RecipeType.AdventTunic, SkillType.Smithery)
            .MinigameParameters(_forge)
            .Create(RecipeType.AmateurTunic, SkillType.Smithery)
            .Build();

        Assert.That(recipes[RecipeType.AdventTunic].MinigameParameters, Is.SameAs(_forge));
        Assert.That(recipes[RecipeType.AmateurTunic].MinigameParameters, Is.Null);
    }

    [Test]
    public void MinigameParameters_WithNullLeavesTheRecipeWithoutParameters()
    {
        var recipes = new RecipeBuilder()
            .Create(RecipeType.AdventTunic, SkillType.Smithery)
            .MinigameParameters(null)
            .Build();

        Assert.That(recipes[RecipeType.AdventTunic].MinigameParameters, Is.Null);
    }

    [Test]
    public void MinigameParameters_OfTheSameTypeReplaceTheEarlierOnes()
    {
        var replacement = new TestForgeParameters(60, 25, 2.0, TestForgeStyle.Hot);

        var recipes = new RecipeBuilder()
            .Create(RecipeType.AdventTunic, SkillType.Smithery)
            .MinigameParameters(_forge)
            .MinigameParameters(replacement)
            .Build();

        Assert.That(recipes[RecipeType.AdventTunic].MinigameParameters, Is.SameAs(replacement));
    }

    [Test]
    public void MinigameParameters_OfAnotherTypeAreRejectedBecauseARecipeUsesOneMinigame()
    {
        var builder = new RecipeBuilder()
            .Create(RecipeType.AdventTunic, SkillType.Smithery)
            .MinigameParameters(_forge);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.MinigameParameters(new OtherTestParameters()));

        Assert.That(exception!.Message, Does.Contain("AdventTunic").And.Contain("TestForgeParameters"));
    }

    [Test]
    public void BuilderMethod_IsTheParameterTypeName()
    {
        Assert.That(_forge.BuilderMethod, Is.EqualTo("TestForgeParameters"));
    }
}
