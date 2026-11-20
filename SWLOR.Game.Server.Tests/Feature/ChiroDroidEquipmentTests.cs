using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Guards the Chiro-tier (level 52) droid armor: heavy (DH) pieces mirror the Chaos set and light (DL) pieces mirror the
/// Immortal set, the same way droid tier 5 mirrors Eclipse and Supreme. The recipes were once cut by accident, which
/// left players holding blueprints that no longer taught anything.
/// </summary>
public class ChiroDroidEquipmentTests
{
    private const int DroidOnlyRace = 167;
    private const int UseLimitationRacialType = 64;

    private static readonly Dictionary<RecipeType, RecipeDetail> Recipes = new DroidEquipmentRecipes().BuildRecipes();

    private static readonly Dictionary<string, int> ChiroEngineeringComponents = new()
    {
        ["ref_arkoxit"] = 2,
        ["elec_high"] = 20,
        ["ref_jasioclase"] = 10,
        ["chiro_shard"] = 2,
        ["ref_veldite"] = 5,
        ["ref_scordspar"] = 5,
        ["ref_plagionite"] = 5,
        ["ref_keromber"] = 5,
    };

    private static readonly string[] BlueprintLootTables =
    {
        "DANTOOINE_KINRATH_QUEEN_RECIPES",
        "DATHOMIR_CHIRODACTYL_RECIPES",
        "QIONHIVE_BROODMOTHER_RECIPE",
        "KORRIBAN_MASTER_RECIPE",
    };

    // Recipe ids are pinned: blueprints players already hold store these numbers in their RECIPES local.
    public static IEnumerable<TestCaseData> Pieces()
    {
        (RecipeType Id, int Number, RecipeCategoryType Category, string Mirror)[] pieces =
        {
            (RecipeType.DHCL005C, 3508, RecipeCategoryType.Cloak, "ch_cloak"),
            (RecipeType.DHBE005C, 3509, RecipeCategoryType.Belt, "ch_belt"),
            (RecipeType.DHRG005C, 3510, RecipeCategoryType.Ring, "ch_ring"),
            (RecipeType.DHNK005C, 3511, RecipeCategoryType.Necklace, "ch_necklace"),
            (RecipeType.DHAR005C, 3512, RecipeCategoryType.Breastplate, "ch_armor"),
            (RecipeType.DHHL005C, 3513, RecipeCategoryType.Helmet, "ch_helmet"),
            (RecipeType.DHBR005C, 3514, RecipeCategoryType.Bracer, "ch_bracer"),
            (RecipeType.DHLG005C, 3515, RecipeCategoryType.Legging, "ch_leggings"),
            (RecipeType.DLCL005C, 3516, RecipeCategoryType.Cloak, "imm_cloak"),
            (RecipeType.DLBE005C, 3517, RecipeCategoryType.Belt, "imm_belt"),
            (RecipeType.DLRG005C, 3518, RecipeCategoryType.Ring, "imm_ring"),
            (RecipeType.DLNK005C, 3519, RecipeCategoryType.Necklace, "imm_necklace"),
            (RecipeType.DLAR005C, 3520, RecipeCategoryType.Breastplate, "imm_tunic"),
            (RecipeType.DLHL005C, 3521, RecipeCategoryType.Helmet, "imm_cap"),
            (RecipeType.DLBR005C, 3522, RecipeCategoryType.Glove, "imm_gloves"),
            (RecipeType.DLLG005C, 3523, RecipeCategoryType.Legging, "imm_boots"),
        };

        return pieces.Select(piece => new TestCaseData(piece.Id, piece.Number, piece.Category, piece.Mirror)
            .SetName($"ChiroDroidPiece_{piece.Id}"));
    }

    [TestCaseSource(nameof(Pieces))]
    public void Recipe_IsAnUnlockedChiroEngineeringRecipe(RecipeType id, int number, RecipeCategoryType category, string mirror)
    {
        ((int)id).Should().Be(number, "saved blueprints teach the recipe by this number");
        Recipes.Should().ContainKey(id, "the Chiro droid recipe must be registered so the Engineering menu lists it");

        var recipe = Recipes[id];
        var resref = id.ToString().ToLowerInvariant();
        recipe.IsActive.Should().BeTrue();
        recipe.Skill.Should().Be(SkillType.Engineering);
        recipe.Category.Should().Be(category, "the Chiro piece sits in the same category as its tier 5 base");
        recipe.Category.Should().Be(Recipes[BaseRecipe(id)].Category);
        recipe.Resref.Should().Be(resref);
        recipe.Level.Should().Be(52);
        recipe.Quantity.Should().Be(1);
        recipe.Requirements.Should().ContainSingle(requirement => requirement is RecipeUnlockRequirement);
        recipe.ResearchCostModifier.Should().Be(0.2f);
        recipe.EnhancementType.Should().Be(RecipeEnhancementType.Armor);
        recipe.EnhancementSlots.Should().Be(2);
        recipe.Components.Should().BeEquivalentTo(ChiroEngineeringComponents,
            "Chiro Engineering recipes share one material bill");
        recipe.MinigameParameters.Should().BeOfType<CircuitParameters>();
    }

    [TestCaseSource(nameof(Pieces))]
    public void Item_IsDroidOnlyAndMirrorsItsChiroSetPiece(RecipeType id, int number, RecipeCategoryType category, string mirror)
    {
        var resref = id.ToString().ToLowerInvariant();
        using var actual = ReadItem(resref);
        using var standard = ReadItem(mirror);
        using var tierFive = ReadItem(Recipes[BaseRecipe(id)].Resref);
        var item = actual.RootElement;

        Value(item, "TemplateResRef").GetString().Should().Be(resref);
        Value(item, "Tag").GetString().Should().Be(resref);
        Value(item, "LocalizedName").GetProperty("0").GetString().Should()
            .Be($"{id.ToString()[..4]}-005 C");
        Value(item, "BaseItem").GetInt32().Should().Be(Value(tierFive.RootElement, "BaseItem").GetInt32(),
            "the Chiro piece keeps its tier 5 base's item type and look");

        var properties = Properties(item).ToList();
        properties.Should().ContainSingle(p => p.Kind == UseLimitationRacialType && p.Subtype == DroidOnlyRace,
            "droid armor is droid-only");
        properties.Where(p => p.Kind != UseLimitationRacialType).Should().BeEquivalentTo(Properties(standard.RootElement),
            $"{resref} carries the stats and Armor 50 requirement of {mirror}");
    }

    [TestCaseSource(nameof(Pieces))]
    public void Blueprint_TeachesTheRecipeAndDropsFromChiroBosses(RecipeType id, int number, RecipeCategoryType category, string mirror)
    {
        var book = $"recipe_{id.ToString().ToLowerInvariant()}";
        using var blueprint = ReadItem(book);
        var item = blueprint.RootElement;

        Value(item, "TemplateResRef").GetString().Should().Be(book);
        Value(item, "Tag").GetString().Should().Be("RECIPE");
        var locals = Value(item, "VarTable").EnumerateArray()
            .ToDictionary(v => Value(v, "Name").GetString()!, v => Value(v, "Value"));
        locals["RECIPES"].GetString().Should().Be(number.ToString());

        var tables = new DantooineLootTableDefinition().BuildLootTables()
            .Concat(new DathomirLootTableDefinition().BuildLootTables())
            .Concat(new HutlarLootTableDefinition().BuildLootTables())
            .Concat(new KorribanLootTableDefinition().BuildLootTables())
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        foreach (var table in BlueprintLootTables)
        {
            tables[table].Should().ContainSingle(entry => entry.Resref == book && entry.Weight > 0,
                $"{table} drops the Chiro droid blueprints alongside the Chaos, Magus and Immortal ones");
        }
    }

    private static RecipeType BaseRecipe(RecipeType id) => Enum.Parse<RecipeType>(id.ToString()[..^1]);

    private static JsonElement Value(JsonElement element, string field) => element.GetProperty(field).GetProperty("value");

    private static IEnumerable<(int Kind, int Subtype, int Table, int Value)> Properties(JsonElement item) =>
        Value(item, "PropertiesList").EnumerateArray().Select(p => (
            Value(p, "PropertyName").GetInt32(),
            Value(p, "Subtype").GetInt32(),
            Value(p, "CostTable").GetInt32(),
            Value(p, "CostValue").GetInt32()));

    private static JsonDocument ReadItem(string resref)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        var path = Path.Combine(root!.FullName, "Module", "uti", $"{resref}.uti.json");
        File.Exists(path).Should().BeTrue($"{resref} needs an item blueprint");
        return JsonDocument.Parse(File.ReadAllText(path));
    }
}
