using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class EchaniCookingTests
{
    [Test]
    public void Cuisine_UsesTierFiveFoodRulesAndExistingConsumptionProperties()
    {
        var recipes = new EchaniCookingRecipes().BuildRecipes();
        recipes.Should().HaveCount(12);
        recipes.Values.Select(recipe => recipe.Resref).Should().OnlyHaveUniqueItems();

        foreach (var recipe in recipes.Values)
        {
            recipe.Skill.Should().Be(SkillType.Agriculture);
            recipe.Category.Should().Be(RecipeCategoryType.Food);
            recipe.Level.Should().BeInRange(41, 50);
            recipe.EnhancementType.Should().Be(RecipeEnhancementType.Food);
            recipe.EnhancementSlots.Should().Be(2);
            recipe.Requirements.OfType<RecipeUnlockRequirement>().Should().BeEmpty(
                "these dishes use the ordinary cooking recipe path without a teaching book");
            recipe.Components.Should().OnlyContain(component => component.Value > 0);

            using var food = ReadItem(recipe.Resref);
            var item = food.RootElement;
            item.GetProperty("Charges").GetProperty("value").GetInt32().Should().Be(1);
            item.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(recipe.Resref);
            recipe.Resref.Length.Should().BeLessThanOrEqualTo(16);
            var properties = item.GetProperty("PropertiesList").GetProperty("value").EnumerateArray().ToArray();
            properties.Should().ContainSingle(property =>
                GetInt(property, "PropertyName") == 15 && GetInt(property, "Subtype") == 335,
                "the existing FOOD activation property must dispatch consumption");
            properties.Should().Contain(property => GetInt(property, "PropertyName") == 106);

            foreach (var component in recipe.Components.Keys)
            {
                using var material = ReadItem(component);
                material.RootElement.GetProperty("Plot").GetProperty("value").GetInt32().Should().Be(0);
            }
        }
    }

    [TestCase("esh_dumplings", "wild_sandwich")]
    [TestCase("esh_broth_noodle", "miso_ramen")]
    [TestCase("esh_chili_noodle", "wild_curry")]
    [TestCase("esh_steam_bun", "wild_sandwich")]
    [TestCase("esh_flatbread", "grand_mball")]
    [TestCase("esh_hotpot", "wild_stew")]
    [TestCase("esh_clayrice", "popper_bowl")]
    [TestCase("esh_porridge", "zoni_broth")]
    [TestCase("esh_marble_egg", "crystal_sushi")]
    [TestCase("esh_ginger_fish", "sea_bass_croute")]
    [TestCase("esh_glaze_fowl", "shining_stew")]
    [TestCase("esh_seed_cake", "wizard_cookies")]
    public void FoodBonuses_MatchExistingBibleTierFiveBalance(string resref, string reference)
    {
        using var food = ReadItem(resref);
        using var baseline = ReadItem(reference);
        ReadFoodBonuses(food.RootElement).Should().BeEquivalentTo(ReadFoodBonuses(baseline.RootElement));
    }

    [Test]
    public void AllNewMaterials_DropFromSpawnedEshanMobsAndHaveUsableBlueprints()
    {
        var root = FindRoot();
        var lootTables = new EshanLootTableDefinition().BuildLootTables();
        var spawns = new EshanSpawnDefinition().BuildSpawnTables();
        var placedTables = Directory.EnumerateFiles(Path.Combine(root, "Module", "git"), "*.git.json")
            .SelectMany(path =>
            {
                using var area = JsonDocument.Parse(File.ReadAllText(path));
                return ReadLocals(area.RootElement, "CREATURE_SPAWN_TABLE_ID").ToArray();
            }).ToHashSet();
        var reachableTables = spawns.Where(table => placedTables.Contains(table.Key))
            .SelectMany(table => table.Value.Spawns)
            .Where(spawn => spawn.Type == ObjectType.Creature && spawn.Weight > 0)
            .SelectMany(spawn =>
            {
                using var mob = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "utc", spawn.Resref + ".utc.json")));
                return ReadLocals(mob.RootElement, "LOOT_TABLE_")
                    .Select(value => value.Split(','))
                    .Where(parts => parts.Length == 3 && int.Parse(parts[1]) > 0 && int.Parse(parts[2]) > 0)
                    .Select(parts => parts[0]).ToArray();
            }).ToHashSet();

        var materials = new EchaniCookingRecipes().BuildRecipes().Values
            .SelectMany(recipe => recipe.Components.Keys)
            .Where(resref => resref.StartsWith("esh_", StringComparison.Ordinal)).Distinct().ToArray();
        materials.Should().HaveCount(10);
        foreach (var resref in materials)
        {
            lootTables.Where(table => reachableTables.Contains(table.Key))
                .SelectMany(table => table.Value)
                .Should().Contain(entry => entry.Resref == resref && entry.Weight > 0 && entry.MaxQuantity > 0,
                    $"{resref} must be obtainable by hunting an Eshan mob in a placed spawn table");
            using var material = ReadItem(resref);
            var item = material.RootElement;
            GetInt(item, "Plot").Should().Be(0);
            GetInt(item, "Charges").Should().Be(0);
            item.GetProperty("PropertiesList").GetProperty("value").GetArrayLength().Should().Be(0,
                "raw materials must not inherit consumption or equipment effects");
            item.GetProperty("Description").GetProperty("value").GetProperty("0").GetString().Should().NotBeNullOrWhiteSpace();
        }
    }

    [Test]
    public void SharedHotpot_ProducesFourIndividuallyConsumablePortions()
    {
        var recipe = new EchaniCookingRecipes().BuildRecipes()[RecipeType.EchaniSharedHotpot];
        recipe.Quantity.Should().Be(4);
        recipe.Components["esh_wolf_meat"].Should().Be(3 * recipe.Quantity);
        recipe.Components["esh_greens"].Should().Be(2 * recipe.Quantity);
        recipe.Components["esh_rice"].Should().Be(recipe.Quantity);
        recipe.Components["ginger"].Should().Be(recipe.Quantity);
        recipe.Components["distilled_water"].Should().Be(recipe.Quantity);
    }

    private static (int Subtype, int Amount)[] ReadFoodBonuses(JsonElement item) =>
        item.GetProperty("PropertiesList").GetProperty("value").EnumerateArray()
            .Where(property => GetInt(property, "PropertyName") == 106)
            .Select(property => (GetInt(property, "Subtype"), GetInt(property, "CostValue"))).ToArray();

    private static int GetInt(JsonElement item, string field) => item.GetProperty(field).GetProperty("value").GetInt32();

    private static IEnumerable<string> ReadLocals(JsonElement item, string prefix)
    {
        if (!item.TryGetProperty("VarTable", out var variables)) yield break;
        foreach (var variable in variables.GetProperty("value").EnumerateArray())
        {
            if (variable.GetProperty("Name").GetProperty("value").GetString()!.StartsWith(prefix, StringComparison.Ordinal))
                yield return variable.GetProperty("Value").GetProperty("value").GetString()!;
        }
    }

    private static JsonDocument ReadItem(string resref) => JsonDocument.Parse(
        File.ReadAllText(Path.Combine(FindRoot(), "Module", "uti", resref + ".uti.json")));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
