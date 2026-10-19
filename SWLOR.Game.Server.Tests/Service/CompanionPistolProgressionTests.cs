using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Tests.Feature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class CompanionPistolProgressionTests
{
    private static readonly (string Main, RecipeType MainRecipe)[] Tiers =
    {
        ("b_pistol", RecipeType.BasicPistol),
        ("fld_pistol", RecipeType.FieldPistol),
        ("tit_pistol", RecipeType.TitanPistol),
        ("sith_pistol", RecipeType.SithPistol),
        ("vet_pistol", RecipeType.VeteranPistol),
        ("del_pistol", RecipeType.DeltaPistol),
        ("prm_pistol", RecipeType.PrimePistol),
        ("proto_pistol", RecipeType.ProtoPistol),
        ("asc_pistol", RecipeType.AscendantPistol),
        ("oph_pistol", RecipeType.OphidianPistol),
        ("chi_pistol", RecipeType.ChiroPistol),
    };

    [Test]
    public void EveryPair_OwnsTheWeaponProfileAndRequiresTwinRanksDirectly()
    {
        foreach (var (mainResref, _) in Tiers)
        {
            using var main = ReadItem(mainResref);
            using var pair = ReadItem(mainResref.Replace("_pistol", "_twinpistol"));
            var item = pair.RootElement;
            Int(item, "BaseItem").Should().Be((int)BaseItem.Sling);
            HasLocal(item, "TWIN_PISTOL_PAIR").Should().BeTrue();
            HasLocal(item, "TWIN_PISTOL_SPARE").Should().BeFalse();
            HasLocal(item, "NO_ECONOMY").Should().BeFalse();
            foreach (var field in new[] { "Cost", "AddCost" })
                Int(item, field).Should().Be(Int(main.RootElement, field) * 2);
            foreach (var field in new[] { "ModelPart1", "ModelPart2", "ModelPart3", "StackSize" })
                Int(item, field).Should().Be(Int(main.RootElement, field));
            foreach (var propertyType in new[] { ItemPropertyType.DMG, ItemPropertyType.Delay,
                         ItemPropertyType.Accuracy, ItemPropertyType.DamageStat, ItemPropertyType.AccuracyStat,
                         ItemPropertyType.WeaponDamageType })
                Profiles(item, propertyType).Should().BeEquivalentTo(Profiles(main.RootElement, propertyType));
            var requirement = Properties(item).Single(p => Int(p, "PropertyName") == (int)ItemPropertyType.RequiresSkill);
            Int(requirement, "Subtype").Should().Be((int)SkillType.TwinPistol);
            Int(requirement, "CostValue").Should().Be(Int(Properties(main.RootElement)
                .Single(p => Int(p, "PropertyName") == (int)ItemPropertyType.RequiresSkill), "CostValue"));
        }
    }

    [Test]
    public void PairRecipes_ProduceOneOwnedItemWithOneSlotBudgetAndBothGunsMaterials()
    {
        var mains = new WeaponRecipes().BuildRecipes();
        var pairs = new TwinPistolRecipes().BuildRecipes();
        pairs.Should().HaveCount(Tiers.Length);
        foreach (var (main, mainRecipe) in Tiers)
        {
            var pairRecipe = Enum.Parse<RecipeType>(mainRecipe.ToString().Replace("Pistol", "TwinPistol"));
            var source = mains[mainRecipe];
            var pair = pairs[pairRecipe];
            pair.Resref.Should().Be(main.Replace("_pistol", "_twinpistol"));
            pair.Category.Should().Be(RecipeCategoryType.TwinPistol);
            pair.Quantity.Should().Be(1);
            (pair.Skill, pair.Level, pair.EnhancementType, pair.EnhancementSlots,
                pair.ResearchCostModifier, pair.PracticeRankLimit, pair.IsActive)
                .Should().Be((source.Skill, source.Level, source.EnhancementType, source.EnhancementSlots,
                    source.ResearchCostModifier, source.PracticeRankLimit, source.IsActive));
            pair.Components.Should().BeEquivalentTo(source.Components.ToDictionary(p => p.Key, p => p.Value * 2));
            pair.Requirements.Should().BeEquivalentTo(source.Requirements);
        }
    }

    [Test]
    public void PairItems_AreObtainableWhileIndependentCompanionsHaveNoPlayerSource()
    {
        var root = Root();
        var obtainable = EconomyObtainabilityCoverageTests.ReadObtainableResrefs(root);
        obtainable.Should().Contain(Tiers.Select(t => t.Main.Replace("_pistol", "_twinpistol")));
        var stores = Directory.EnumerateFiles(Path.Combine(root, "Module", "utm"), "*.utm.json")
            .Select(file => (Path: file, Text: File.ReadAllText(file))).ToArray();
        var starterStores = stores.Where(store => store.Text.Contains("\"b_pistol\""));
        starterStores.Should().HaveCount(7);
        foreach (var store in starterStores)
            store.Text.Should().Contain("\"b_twinpistol\"", Path.GetFileName(store.Path));
        stores.Should().NotContain(store => store.Text.Contains("\"twp_companion\""));
    }

    [Test]
    public void PlacedStarterStores_SellOneOwnedPairMatchingTheBlueprintWithoutIndependentCompanions()
    {
        var root = Root();
        var expectedStores = new Dictionary<string, (int Infinite, int X, int Y, int SinglePistols)>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "Module", "utm"), "*.utm.json"))
        {
            var text = File.ReadAllText(file);
            if (!text.Contains("\"b_twinpistol\"")) continue;
            using var blueprint = JsonDocument.Parse(text);
            var stock = StoreItems(blueprint.RootElement).ToArray();
            var pair = stock.Single(item => WrappedString(item, "InventoryRes") == "b_twinpistol");
            expectedStores[Path.GetFileName(file).Replace(".utm.json", "")] = (
                Int(pair, "Infinite"), Int(pair, "Repos_PosX"), Int(pair, "Repos_Posy"),
                stock.Count(item => WrappedString(item, "InventoryRes") == "b_pistol"));
        }
        expectedStores.Should().NotBeEmpty();
        using var ownedPair = ReadItem("b_twinpistol");
        var checkedStores = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "Module", "git"), "*.git.json"))
        {
            var text = File.ReadAllText(file);
            if (!expectedStores.Keys.Any(resref => text.Contains('"' + resref + '"', StringComparison.OrdinalIgnoreCase))) continue;
            using var area = JsonDocument.Parse(text);
            if (!area.RootElement.TryGetProperty("StoreList", out var storeList)) continue;
            foreach (var store in storeList.GetProperty("value").EnumerateArray())
            {
                var resref = WrappedString(store, "ResRef");
                if (!expectedStores.TryGetValue(resref, out var expected)) continue;
                checkedStores.Add(resref);
                var stock = StoreItems(store).ToArray();
                stock.Should().NotContain(item => OptionalBaseItem(item) == (int)BaseItem.CompanionPistol,
                    $"{Path.GetFileName(file)} must not sell an independently owned companion");
                var pairs = stock.Where(item => WrappedString(item, "TemplateResRef") == "b_twinpistol").ToArray();
                pairs.Should().ContainSingle($"{Path.GetFileName(file)} must match its starter store blueprint");
                var pair = pairs.Single();
                HasLocal(pair, "TWIN_PISTOL_PAIR").Should().BeTrue();
                HasLocal(pair, "TWIN_PISTOL_SPARE").Should().BeFalse();
                (Int(pair, "Infinite"), Int(pair, "Repos_PosX"), Int(pair, "Repos_Posy"))
                    .Should().Be((expected.Infinite, expected.X, expected.Y));
                foreach (var field in new[] { "BaseItem", "Cost", "AddCost", "StackSize", "LocalizedName",
                             "DescIdentified", "PropertiesList", "VarTable", "Tag", "TemplateResRef",
                             "ModelPart1", "ModelPart2", "ModelPart3", "xModelPart1", "xModelPart2", "xModelPart3" })
                    JsonElement.DeepEquals(pair.GetProperty(field), ownedPair.RootElement.GetProperty(field))
                        .Should().BeTrue($"{Path.GetFileName(file)} must retain the owned pair's {field}");
                stock.Count(item => WrappedString(item, "TemplateResRef") == "b_pistol")
                    .Should().Be(expected.SinglePistols, "the single-pistol starter remains available");
            }
        }
        checkedStores.Should().BeEquivalentTo(expectedStores.Keys);
    }

    [Test]
    public void TemporaryTemplate_HasNoValueOrProfileUntilRuntimeCreatesItsLinkedCopy()
    {
        using var template = ReadItem("twp_companion");
        var item = template.RootElement;
        Int(item, "BaseItem").Should().Be((int)BaseItem.CompanionPistol);
        HasLocal(item, "TWIN_PISTOL_SPARE").Should().BeTrue();
        HasLocal(item, "NO_ECONOMY").Should().BeTrue();
        HasLocal(item, "TWIN_PISTOL_PAIR").Should().BeFalse();
        Properties(item).Should().BeEmpty();
        Int(item, "Cost").Should().Be(0);
        Int(item, "AddCost").Should().Be(0);
    }

    private static IEnumerable<(int Subtype, int CostValue, int Param1Value)> Profiles(JsonElement item, ItemPropertyType type) =>
        Properties(item).Where(p => Int(p, "PropertyName") == (int)type)
            .Select(p => (Int(p, "Subtype"), Int(p, "CostValue"), Int(p, "Param1Value")));

    private static IEnumerable<JsonElement> Properties(JsonElement item) => item.GetProperty("PropertiesList").GetProperty("value").EnumerateArray();
    private static IEnumerable<JsonElement> StoreItems(JsonElement store)
    {
        foreach (var panel in store.GetProperty("StoreList").GetProperty("value").EnumerateArray())
        {
            if (!panel.TryGetProperty("ItemList", out var items)) continue;
            foreach (var item in items.GetProperty("value").EnumerateArray()) yield return item;
        }
    }
    private static string WrappedString(JsonElement item, string field) => item.TryGetProperty(field, out var value)
        ? value.GetProperty("value").GetString() ?? string.Empty : string.Empty;
    private static int OptionalBaseItem(JsonElement item) => item.TryGetProperty("BaseItem", out var value)
        ? value.GetProperty("value").GetInt32() : -1;
    private static bool HasLocal(JsonElement item, string name) => item.TryGetProperty("VarTable", out var variables) &&
        variables.GetProperty("value").EnumerateArray().Any(variable =>
            variable.GetProperty("Name").GetProperty("value").GetString() == name &&
            Int(variable, "Type") == 1 && Int(variable, "Value") == 1);
    private static int Int(JsonElement item, string field) => item.GetProperty(field).GetProperty("value").GetInt32();
    private static JsonDocument ReadItem(string resref) => JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Module", "uti", resref + ".uti.json")));
    private static string Root()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SWLOR.Game.Server.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException();
    }
}
