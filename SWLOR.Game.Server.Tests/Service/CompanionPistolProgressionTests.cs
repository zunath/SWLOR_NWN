using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Tests.Feature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class CompanionPistolProgressionTests
{
    private static readonly (string Main, string Companion, RecipeType MainRecipe, RecipeType CompanionRecipe)[] Tiers =
    {
        ("b_pistol", "b_companion", RecipeType.BasicPistol, RecipeType.BasicCompanionPistol),
        ("fld_pistol", "fld_companion", RecipeType.FieldPistol, RecipeType.FieldCompanionPistol),
        ("tit_pistol", "tit_companion", RecipeType.TitanPistol, RecipeType.TitanCompanionPistol),
        ("sith_pistol", "sith_companion", RecipeType.SithPistol, RecipeType.SithCompanionPistol),
        ("vet_pistol", "vet_companion", RecipeType.VeteranPistol, RecipeType.VeteranCompanionPistol),
        ("del_pistol", "del_companion", RecipeType.DeltaPistol, RecipeType.DeltaCompanionPistol),
        ("prm_pistol", "prm_companion", RecipeType.PrimePistol, RecipeType.PrimeCompanionPistol),
        ("proto_pistol", "proto_companion", RecipeType.ProtoPistol, RecipeType.ProtoCompanionPistol),
        ("asc_pistol", "asc_companion", RecipeType.AscendantPistol, RecipeType.AscendantCompanionPistol),
        ("oph_pistol", "oph_companion", RecipeType.OphidianPistol, RecipeType.OphidianCompanionPistol),
        ("chi_pistol", "chi_companion", RecipeType.ChiroPistol, RecipeType.ChiroCompanionPistol),
    };

    [Test]
    public void EveryTier_PreservesMatchingWeaponBudgetAndRequiresTwinRanks()
    {
        foreach (var (mainResref, companionResref, _, _) in Tiers)
        {
            using var main = ReadItem(mainResref);
            using var companion = ReadItem(companionResref);
            var item = companion.RootElement;
            Int(item, "BaseItem").Should().Be((int)BaseItem.CompanionPistol, companionResref);
            foreach (var field in new[] { "Cost", "AddCost", "StackSize" })
                Int(item, field).Should().Be(Int(main.RootElement, field), companionResref);
            (Int(item, "ModelPart1"), Int(item, "ModelPart2"), Int(item, "ModelPart3")).Should().Be((11, 201, 11));
            var properties = Properties(item).ToArray();
            properties.Should().OnlyContain(p => TwinPistolEquipment.IsSupportedCompanionProperty((ItemPropertyType)Int(p, "PropertyName")), companionResref);
            foreach (var propertyType in new[] { ItemPropertyType.DMG, ItemPropertyType.Delay, ItemPropertyType.Accuracy,
                         ItemPropertyType.DamageStat, ItemPropertyType.AccuracyStat, ItemPropertyType.WeaponDamageType })
                Profiles(item, propertyType).Should().BeEquivalentTo(Profiles(main.RootElement, propertyType), companionResref);

            var requirements = properties.Where(p => Int(p, "PropertyName") == (int)ItemPropertyType.RequiresSkill).ToArray();
            requirements.Should().ContainSingle(companionResref);
            Int(requirements[0], "Subtype").Should().Be((int)SkillType.TwinPistol);
            var original = Properties(main.RootElement).FirstOrDefault(p => Int(p, "PropertyName") == (int)ItemPropertyType.RequiresSkill);
            Int(requirements[0], "CostValue").Should().Be(original.ValueKind == JsonValueKind.Undefined ? 0 : Int(original, "CostValue"));
            item.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Contain("Companion Pistol");
        }
    }

    [Test]
    public void CompanionRecipes_PreserveMaterialsAndSlotBudgetsWithOnlyProfileEnhancements()
    {
        var mains = new WeaponRecipes().BuildRecipes();
        var companions = new CompanionPistolRecipes().BuildRecipes();
        companions.Should().HaveCount(Tiers.Length);
        foreach (var (_, companion, mainRecipe, companionRecipe) in Tiers)
        {
            var source = mains[mainRecipe];
            var result = companions[companionRecipe];
            result.Resref.Should().Be(companion);
            result.Skill.Should().Be(SkillType.Smithery);
            result.Category.Should().Be(RecipeCategoryType.CompanionPistol);
            (result.Level, result.EnhancementType, result.EnhancementSlots, result.ResearchCostModifier,
                result.Quantity, result.PracticeRankLimit).Should().Be((source.Level, source.EnhancementType,
                source.EnhancementSlots, source.ResearchCostModifier, source.Quantity, source.PracticeRankLimit));
            result.Components.Should().BeEquivalentTo(source.Components);
            result.Requirements.Should().BeEquivalentTo(source.Requirements);
            result.AllowedEnhancements.Should().BeEquivalentTo(new[] { EnhancementSubType.DMG, EnhancementSubType.Accuracy });
            result.AllowsOutputProperty(ItemPropertyType.AbilityBonus).Should().BeFalse();
            result.AllowsOutputProperty(ItemPropertyType.Attack).Should().BeFalse();
            result.AllowsOutputProperty(ItemPropertyType.OnHitCastSpell).Should().BeFalse();
            result.AllowsOutputProperty(ItemPropertyType.WeaponDamageType).Should().BeTrue();
        }
    }

    [Test]
    public void EveryCompanion_IsRecognizedAsObtainableAndStarterStoresCarryIt()
    {
        var root = Root();
        var obtainable = EconomyObtainabilityCoverageTests.ReadObtainableResrefs(root);
        obtainable.Should().Contain(Tiers.Select(t => t.Companion));
        var starterStores = Directory.EnumerateFiles(Path.Combine(root, "Module", "utm"), "*.utm.json")
            .Where(file => File.ReadAllText(file).Contains("\"b_pistol\""));
        foreach (var store in starterStores)
            File.ReadAllText(store).Should().Contain("\"b_companion\"", Path.GetFileName(store));
    }

    private static IEnumerable<(int Subtype, int CostValue, int Param1Value)> Profiles(JsonElement item, ItemPropertyType type) =>
        Properties(item).Where(p => Int(p, "PropertyName") == (int)type)
            .Select(p => (Int(p, "Subtype"), Int(p, "CostValue"), Int(p, "Param1Value")));

    private static IEnumerable<JsonElement> Properties(JsonElement item) => item.GetProperty("PropertiesList").GetProperty("value").EnumerateArray();
    private static int Int(JsonElement item, string field) => item.GetProperty(field).GetProperty("value").GetInt32();
    private static JsonDocument ReadItem(string resref) => JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Module", "uti", resref + ".uti.json")));
    private static string Root()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SWLOR.Game.Server.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException();
    }
}
