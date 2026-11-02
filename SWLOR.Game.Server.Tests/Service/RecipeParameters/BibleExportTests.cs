using System.Text.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

public class BibleExportTests
{
    private readonly TestForgeProfile _profile = new();

    [Test]
    public void Export_ListsEachProfilesHeadersAndTheCellsOfEveryCoveredRecipe()
    {
        var registry = TestRecipeRegistry.Create(_profile);

        var json = BibleExport.Build(new[] { _profile }, registry, out var problems);

        Assert.That(problems, Is.Empty);
        using var document = JsonDocument.Parse(json);
        var profile = document.RootElement.GetProperty("profiles")[0];
        Assert.That(profile.GetProperty("key").GetString(), Is.EqualTo("TestForge"));
        Assert.That(profile.GetProperty("headers").EnumerateArray().Select(h => h.GetString()),
            Is.EqualTo(new[] { "TestForge Center", "TestForge Width", "TestForge Cooling", "TestForge Style", "TestForge Override" }));

        var recipes = document.RootElement.GetProperty("recipes");
        var starter = recipes.GetProperty("BattlemasterBreastplate");
        Assert.That(starter.GetProperty("TestForge Center").GetInt32(), Is.EqualTo(65));
        Assert.That(starter.GetProperty("TestForge Override").GetString(), Is.EqualTo("Breastplates are worked hot; Starter set stays forgiving"));
        Assert.That(starter.GetProperty("TestForge Style").ValueKind, Is.EqualTo(JsonValueKind.String));
        Assert.That(recipes.TryGetProperty(TestRecipeRegistry.OtherSkillRecipe.ToString(), out _), Is.False, "other skills are not exported");
        Assert.That(recipes.TryGetProperty(TestRecipeRegistry.InactiveRecipe.ToString(), out _), Is.False, "inactive recipes are not exported");
    }

    [Test]
    public void Export_ExportsTheValuesInCodeNotTheGeneratorsOutput()
    {
        var registry = TestRecipeRegistry.Create(_profile);
        registry[RecipeType.SpiritmasterTunic].MinigameParameters = new TestForgeParameters(77, 12, 2.5, TestForgeStyle.Hot);

        var json = BibleExport.Build(new[] { _profile }, registry, out _);

        using var document = JsonDocument.Parse(json);
        var row = document.RootElement.GetProperty("recipes").GetProperty("SpiritmasterTunic");
        Assert.That(row.GetProperty("TestForge Center").GetInt32(), Is.EqualTo(77));
        Assert.That(row.GetProperty("TestForge Cooling").GetDecimal(), Is.EqualTo(2.5m));
        Assert.That(row.GetProperty("TestForge Style").GetString(), Is.EqualTo("Hot"));
    }

    [Test]
    public void Export_IsRefusedWhileACoveredRecipeHasNoParametersInCode()
    {
        var registry = TestRecipeRegistry.Create(_profile);
        registry[RecipeType.AdventTunic].MinigameParameters = null;

        BibleExport.Build(new[] { _profile }, registry, out var problems);

        Assert.That(problems, Is.EqualTo(new[] { "AdventTunic has no TestForgeParameters; run the write command first" }));
    }
}
