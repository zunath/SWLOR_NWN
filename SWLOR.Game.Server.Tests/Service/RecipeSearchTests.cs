using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Tests.Service;

public class RecipeSearchTests
{
    private static readonly Dictionary<string, string> ItemNames = new()
    {
        ["dhrg001"] = "DHRG-001",
        ["ref_veldite"] = "Refined Veldite",
        ["elec_ruined"] = "Ruined Electronics"
    };

    private static RecipeDetail CreateRecipe()
    {
        return new RecipeDetail
        {
            Resref = "dhrg001",
            Components = new Dictionary<string, int>
            {
                ["elec_ruined"] = 1,
                ["ref_veldite"] = 1
            }
        };
    }

    private static string GetName(string resref) => ItemNames.TryGetValue(resref, out var name) ? name : string.Empty;

    [TestCase("veldite")]
    [TestCase("REFINED VEL")]
    [TestCase("  electronics ")]
    public void ComponentSearch_MatchesAnyComponentNameIgnoringCase(string searchText)
    {
        Assert.That(RecipeSearch.Matches(CreateRecipe(), searchText, RecipeSearchType.Component, GetName), Is.True);
    }

    [Test]
    public void ComponentSearch_DoesNotMatchTheRecipeName()
    {
        Assert.That(RecipeSearch.Matches(CreateRecipe(), "DHRG", RecipeSearchType.Component, GetName), Is.False);
    }

    [Test]
    public void RecipeNameSearch_DoesNotMatchComponentNames()
    {
        var recipe = CreateRecipe();

        Assert.That(RecipeSearch.Matches(recipe, "dhrg", RecipeSearchType.RecipeName, GetName), Is.True);
        Assert.That(RecipeSearch.Matches(recipe, "veldite", RecipeSearchType.RecipeName, GetName), Is.False);
    }

    [Test]
    public void ComponentSearch_HandlesRecipesWithoutComponents()
    {
        var recipe = new RecipeDetail { Resref = "dhrg001", Components = new Dictionary<string, int>() };

        Assert.That(RecipeSearch.Matches(recipe, "veldite", RecipeSearchType.Component, GetName), Is.False);
    }

    [TestCase(RecipeSearchType.RecipeName)]
    [TestCase(RecipeSearchType.Component)]
    public void BlankSearch_MatchesEveryRecipe(RecipeSearchType searchType)
    {
        Assert.That(RecipeSearch.Matches(CreateRecipe(), "   ", searchType, GetName), Is.True);
    }
}
