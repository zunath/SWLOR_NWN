using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Feature;

public class ArkanianEquipmentTests
{
    [TestCase("bp_arkhelm", RecipeType.ArkanianDragonHelm, "ark_helm", 10)]
    [TestCase("bp_arkward", RecipeType.ArkanianDragonWardRing, "ark_ward_ring", 5)]
    [TestCase("bp_arkforce", RecipeType.ArkanianDragonForceRing, "ark_force_ring", 5)]
    public void DragonLoot_TeachesCraftableEquipment(string book, RecipeType id, string resref, int scales)
    {
        var loot = new NarShaddaaLootTableDefinition().BuildLootTables()["NARSHADDAA_GREAT_ARKANIAN_DRAGON_RARES"];
        loot.IsRare.Should().BeTrue();
        loot.Should().ContainSingle(item => item.Resref == book && item.IsRare && item.Weight > 0);

        using var blueprint = ReadItem(book);
        var item = blueprint.RootElement;
        item.GetProperty("Tag").GetProperty("value").GetString().Should().Be("RECIPE");
        var locals = item.GetProperty("VarTable").GetProperty("value").EnumerateArray()
            .ToDictionary(v => v.GetProperty("Name").GetProperty("value").GetString()!,
                v => v.GetProperty("Value").GetProperty("value"));
        locals["RECIPES"].GetString().Should().Be(((int)id).ToString());
        locals.Should().NotContainKey("NO_ECONOMY", "these books are obtainable player rewards");

        var recipes = new HeadRecipes().BuildRecipes().Concat(new RingRecipes().BuildRecipes())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var recipe = recipes[id];
        recipe.Resref.Should().Be(resref);
        recipe.Skill.Should().Be(SkillType.Smithery);
        recipe.Level.Should().Be(52);
        recipe.Requirements.Should().ContainSingle(r => r is RecipeUnlockRequirement);
        recipe.Components["ark_drg_scales"].Should().Be(scales);
        recipe.Components["ark_dragon_troph"].Should().Be(1);
        recipe.EnhancementType.Should().Be(RecipeEnhancementType.Armor);
        recipe.EnhancementSlots.Should().Be(2);
        foreach (var component in recipe.Components.Keys)
        {
            using var material = ReadItem(component);
            material.RootElement.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(component);
        }
    }

    [TestCase("ark_helm", "ch_helmet", 15)]
    [TestCase("ark_ward_ring", "ch_ring", 10)]
    [TestCase("ark_force_ring", "mag_ring", 10)]
    public void DragonEquipment_MatchesEndgameBudgetsWithIceProtection(string resref, string baseline, int ice)
    {
        using var actual = ReadItem(resref);
        using var standard = ReadItem(baseline);
        var item = actual.RootElement;
        item.GetProperty("BaseItem").GetProperty("value").GetInt32().Should()
            .Be(standard.RootElement.GetProperty("BaseItem").GetProperty("value").GetInt32());

        var properties = Properties(item).ToList();
        properties.Where(p => p.Kind != 133).Should().BeEquivalentTo(Properties(standard.RootElement),
            "the dragon premium is ice protection on an existing Armor-50 stat budget");
        properties.Should().ContainSingle(p => p.Kind == 133 && p.Subtype == 4 && p.Table == 54 && p.Value == ice,
            "ice protection must use the runtime Resistance property, not ignored legacy ice defense");
        properties.Should().Contain((131, 6, 48, 50), "all pieces require Armor 50");

        if (resref == "ark_helm")
        {
            using var armor = ReadItem("ark_armor");
            foreach (var color in armor.RootElement.EnumerateObject().Where(p => p.Name.EndsWith("Color")))
                item.GetProperty(color.Name).GetProperty("value").GetInt32().Should()
                    .Be(color.Value.GetProperty("value").GetInt32(), "the helm shares the armor's palette");
        }
    }

    private static IEnumerable<(int Kind, int Subtype, int Table, int Value)> Properties(JsonElement item) =>
        item.GetProperty("PropertiesList").GetProperty("value").EnumerateArray().Select(p => (
            p.GetProperty("PropertyName").GetProperty("value").GetInt32(),
            p.GetProperty("Subtype").GetProperty("value").GetInt32(),
            p.GetProperty("CostTable").GetProperty("value").GetInt32(),
            p.GetProperty("CostValue").GetProperty("value").GetInt32()));

    private static JsonDocument ReadItem(string resref)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, "Module", "uti", $"{resref}.uti.json")));
    }
}
