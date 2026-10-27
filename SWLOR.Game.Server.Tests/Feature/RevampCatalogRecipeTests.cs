using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Guards that every space revamp catalog step and hull has a registered recipe that matches the
/// catalog, and that the Dockhand Store sells the starter modules and store hulls.
/// </summary>
public class RevampCatalogRecipeTests
{
    private static readonly string[] StarterModuleTags =
    {
        "sm_laser_00", "sm_missl_00", "sm_mine_00", "sm_shbst_00", "sm_shrep_00",
        "sm_tcomp_00", "sm_capb_00", "sm_plate_00", "sm_thrust_00",
    };

    [Test]
    public void EveryCatalogModuleStep_HasMatchingRecipe()
    {
        var recipes = new RevampStarshipRecipes().BuildRecipes();
        var problems = new List<string>();

        foreach (var step in RevampModuleCatalog.Steps)
        {
            if (!recipes.TryGetValue(step.RecipeType, out var recipe))
            {
                problems.Add($"{step.Tag}: no recipe {step.RecipeType}");
                continue;
            }

            Compare(problems, step.Tag, recipe, RecipeCategoryType.ShipModule, step.RecipeLevel,
                RecipeEnhancementType.Module, step.Tag, step.Components);
        }

        problems.Should().BeEmpty();
    }

    [Test]
    public void EveryCatalogHull_HasMatchingRecipe()
    {
        var recipes = new RevampStarshipRecipes().BuildRecipes();
        var problems = new List<string>();

        foreach (var hull in RevampShipCatalog.Hulls)
        {
            if (!recipes.TryGetValue(hull.RecipeType, out var recipe))
            {
                problems.Add($"{hull.Tag}: no recipe {hull.RecipeType}");
                continue;
            }

            Compare(problems, hull.Tag, recipe, RecipeCategoryType.Starship, hull.RecipeLevel,
                RecipeEnhancementType.Starship, hull.Tag, hull.Components);
        }

        problems.Should().BeEmpty();
    }

    [Test]
    public void RevampRecipes_CoverTheWholeCatalog()
    {
        var recipes = new RevampStarshipRecipes().BuildRecipes();

        recipes.Count.Should().Be(RevampModuleCatalog.Steps.Count + RevampShipCatalog.Hulls.Count);
        recipes.Values.Select(recipe => recipe.Resref).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void DockhandStore_SellsStarterModulesAndStoreHulls()
    {
        var stocked = ReadDockhandStoreResrefs();

        foreach (var tag in StarterModuleTags)
        {
            RevampModuleCatalog.Steps.Should().Contain(step => step.Tag == tag && step.Rank == 0);
            stocked.Should().Contain(tag);
        }

        foreach (var hull in RevampShipCatalog.Hulls)
        {
            if (hull.Store)
                stocked.Should().Contain(hull.Tag);
            else
                stocked.Should().NotContain(hull.Tag);
        }
    }

    private static void Compare(
        List<string> problems,
        string tag,
        RecipeDetail recipe,
        RecipeCategoryType category,
        int level,
        RecipeEnhancementType enhancement,
        string resref,
        (string Resref, int Quantity)[] components)
    {
        if (recipe.Skill != SkillType.Engineering) problems.Add($"{tag}: skill {recipe.Skill}");
        if (recipe.Category != category) problems.Add($"{tag}: category {recipe.Category}");
        if (recipe.Level != level) problems.Add($"{tag}: level {recipe.Level} vs {level}");
        if (recipe.Resref != resref) problems.Add($"{tag}: resref {recipe.Resref}");
        if (recipe.Quantity != 1) problems.Add($"{tag}: quantity {recipe.Quantity}");
        if (recipe.EnhancementType != enhancement || recipe.EnhancementSlots != 1)
            problems.Add($"{tag}: enhancement {recipe.EnhancementType} x{recipe.EnhancementSlots}");

        var expected = components.ToDictionary(component => component.Resref, component => component.Quantity);
        if (recipe.Components.Count != expected.Count ||
            expected.Any(pair => !recipe.Components.TryGetValue(pair.Key, out var quantity) || quantity != pair.Value))
        {
            problems.Add($"{tag}: components differ from the catalog");
        }
    }

    private static HashSet<string> ReadDockhandStoreResrefs()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");

        var path = Path.Combine(directory!.FullName, "Module", "utm", "dockhand_store.utm.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var resrefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var store in document.RootElement.GetProperty("StoreList").GetProperty("value").EnumerateArray())
        {
            if (!store.TryGetProperty("ItemList", out var itemList))
                continue;

            foreach (var item in itemList.GetProperty("value").EnumerateArray())
            {
                resrefs.Add(item.GetProperty("InventoryRes").GetProperty("value").GetString()!);
            }
        }

        return resrefs;
    }
}
