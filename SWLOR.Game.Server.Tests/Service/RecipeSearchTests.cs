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

    [TestCase(false)]
    [TestCase(true)]
    public void RecipeName_MatchesIgnoringCase(bool includeComponents)
    {
        Assert.That(RecipeSearch.Matches(CreateRecipe(), " dhrg ", includeComponents, GetName), Is.True);
    }

    [Test]
    public void ComponentNames_DoNotMatchByDefault()
    {
        Assert.That(RecipeSearch.Matches(CreateRecipe(), "veldite", false, GetName), Is.False);
    }

    [TestCase("veldite")]
    [TestCase("REFINED VEL")]
    [TestCase("  electronics ")]
    public void IncludeComponents_MatchesAnyComponentNameIgnoringCase(string searchText)
    {
        Assert.That(RecipeSearch.Matches(CreateRecipe(), searchText, true, GetName), Is.True);
    }

    [Test]
    public void IncludeComponents_StillRejectsTextFoundNowhere()
    {
        Assert.That(RecipeSearch.Matches(CreateRecipe(), "durasteel", true, GetName), Is.False);
    }

    [Test]
    public void IncludeComponents_HandlesRecipesWithoutComponents()
    {
        var recipe = new RecipeDetail { Resref = "dhrg001", Components = new Dictionary<string, int>() };

        Assert.That(RecipeSearch.Matches(recipe, "veldite", true, GetName), Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BlankSearch_MatchesEveryRecipe(bool includeComponents)
    {
        Assert.That(RecipeSearch.Matches(CreateRecipe(), "   ", includeComponents, GetName), Is.True);
    }
}
