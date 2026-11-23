using System.Text;
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
/// catalog, and that the Dockhand Store sells the starter modules, store hulls and basic missiles.
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
                RecipeEnhancementType.Module, step.Tag, step.Components, step.Named ? 2 : 1);
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
                RecipeEnhancementType.Starship, hull.Tag, hull.Components, 1);
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

    [Test]
    public void EveryDockhandStore_SellsBasicMissilesBesideTheStarterLauncher()
    {
        // The launcher fires one ship_missile per shot, so a store that sells it must sell the ammunition too
        // (plan section 14: "the lowest-tier missile"). Checked on the blueprint and every placed copy.
        var root = FindRepositoryRoot();
        var stores = new List<(string Name, JsonElement Store)>();
        var documents = new List<JsonDocument>();
        try
        {
            var blueprint = JsonDocument.Parse(File.ReadAllText(
                Path.Combine(root, "Module", "utm", "dockhand_store.utm.json"), Encoding.Latin1));
            documents.Add(blueprint);
            stores.Add(("dockhand_store.utm", blueprint.RootElement));

            foreach (var path in Directory.GetFiles(Path.Combine(root, "Module", "git"), "*.git.json"))
            {
                var text = File.ReadAllText(path, Encoding.Latin1);
                if (!text.Contains("\"dockhand_store\"", StringComparison.Ordinal))
                    continue;

                var area = JsonDocument.Parse(text);
                documents.Add(area);
                foreach (var store in area.RootElement.GetProperty("StoreList").GetProperty("value").EnumerateArray())
                {
                    if (store.TryGetProperty("ResRef", out var resref) &&
                        resref.GetProperty("value").GetString() == "dockhand_store")
                    {
                        stores.Add((Path.GetFileName(path), store));
                    }
                }
            }

            var problems = new List<string>();
            var launcherStores = 0;
            foreach (var (name, store) in stores)
            {
                foreach (var panel in store.GetProperty("StoreList").GetProperty("value").EnumerateArray())
                {
                    if (!panel.TryGetProperty("ItemList", out var itemList))
                        continue;

                    var items = itemList.GetProperty("value").EnumerateArray().Select(item => (
                        Resref: (item.TryGetProperty("TemplateResRef", out var template) ? template : item.GetProperty("InventoryRes"))
                            .GetProperty("value").GetString(),
                        X: item.GetProperty("Repos_PosX").GetProperty("value").GetInt32(),
                        Y: item.GetProperty("Repos_Posy").GetProperty("value").GetInt32(),
                        Infinite: item.TryGetProperty("Infinite", out var infinite) && infinite.GetProperty("value").GetInt32() == 1))
                        .ToList();
                    if (items.All(item => item.Resref != "sm_missl_00"))
                        continue;

                    launcherStores++;
                    var missile = items.Where(item => item.Resref == "ship_missile").ToList();
                    if (missile.Count != 1 || !missile[0].Infinite)
                        problems.Add($"{name}: the starter launcher's panel needs one infinite ship_missile");

                    if (items.GroupBy(item => (item.X, item.Y)).Any(group => group.Count() > 1))
                        problems.Add($"{name}: two items share a store slot");
                }
            }

            problems.Should().BeEmpty();
            launcherStores.Should().BeGreaterThanOrEqualTo(14, "the blueprint and its 13 placed copies sell the starter launcher");
        }
        finally
        {
            foreach (var document in documents)
                document.Dispose();
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!.FullName;
    }

    private static void Compare(
        List<string> problems,
        string tag,
        RecipeDetail recipe,
        RecipeCategoryType category,
        int level,
        RecipeEnhancementType enhancement,
        string resref,
        (string Resref, int Quantity)[] components,
        int enhancementSlots)
    {
        if (recipe.Skill != SkillType.Engineering) problems.Add($"{tag}: skill {recipe.Skill}");
        if (recipe.Category != category) problems.Add($"{tag}: category {recipe.Category}");
        if (recipe.Level != level) problems.Add($"{tag}: level {recipe.Level} vs {level}");
        if (recipe.Resref != resref) problems.Add($"{tag}: resref {recipe.Resref}");
        if (recipe.Quantity != 1) problems.Add($"{tag}: quantity {recipe.Quantity}");
        if (recipe.EnhancementType != enhancement || recipe.EnhancementSlots != enhancementSlots)
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
