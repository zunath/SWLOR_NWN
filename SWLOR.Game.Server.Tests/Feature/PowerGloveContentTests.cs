using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class PowerGloveContentTests
{
    private static readonly string[] TierPrefixes = { "t", "b", "fld", "tit", "vet", "del", "prm", "proto", "asc", "oph", "charm", "chi" };

    public static IEnumerable<TestCaseData> Tiers() => TierPrefixes.Select(prefix => new TestCaseData(prefix));

    [TestCaseSource(nameof(Tiers))]
    public void TierPreservesItsKatarWeaponAndCreditBudgetWithItsOwnSkill(string prefix)
    {
        var source = ReadItem(prefix + "_katar");
        var item = ReadItem(prefix + "_pglove");
        Int(item, "BaseItem").Should().Be((int)BaseItem.PowerGlove);
        Text(item, "TemplateResRef").Should().Be(prefix + "_pglove");
        Text(item, "Tag").Should().Be(prefix + "_pglove");
        item.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Contain("Power Glove");
        Int(item, "Cost").Should().Be(Int(source, "Cost"));
        Int(item, "AddCost").Should().Be(Int(source, "AddCost"));
        Int(item, "StackSize").Should().Be(1);
        foreach (var field in new[] { "ModelPart1", "ModelPart2", "ModelPart3", "xModelPart1", "xModelPart2", "xModelPart3" })
            Int(item, field).Should().Be(11);

        var sourceProperties = List(source, "PropertiesList").ToArray();
        var itemProperties = List(item, "PropertiesList").ToArray();
        itemProperties.Where(property => Int(property, "PropertyName") != (int)ItemPropertyType.RequiresSkill)
            .Select(PropertyBudget).Should().BeEquivalentTo(sourceProperties
                .Where(property => Int(property, "PropertyName") != (int)ItemPropertyType.RequiresSkill).Select(PropertyBudget));
        var requirements = itemProperties.Where(property => Int(property, "PropertyName") == (int)ItemPropertyType.RequiresSkill).ToArray();
        var sourceRequirements = sourceProperties.Where(property => Int(property, "PropertyName") == (int)ItemPropertyType.RequiresSkill).ToArray();
        requirements.Should().HaveSameCount(sourceRequirements, "a tier has a skill requirement only when its Katar does");
        foreach (var (requirement, sourceRequirement) in requirements.Zip(sourceRequirements))
        {
            Int(requirement, "Subtype").Should().Be((int)SkillType.Unarmed);
            Int(requirement, "CostValue").Should().Be(Int(sourceRequirement, "CostValue"));
        }
        List(item, "VarTable").Should().NotContain(local => Text(local, "Name") == "NO_ECONOMY");
    }

    [Test]
    public void SmitheryRecipesMatchTheExistingAcquisitionAndEnhancementCosts()
    {
        var references = new WeaponRecipes().BuildRecipes();
        var recipes = new PowerGloveRecipes().BuildRecipes();
        recipes.Should().HaveCount(10);
        foreach (var (type, recipe) in recipes)
        {
            var referenceType = Enum.Parse<RecipeType>(type.ToString().Replace("PowerGlove", "Katar"));
            var reference = references[referenceType];
            recipe.Skill.Should().Be(SkillType.Smithery);
            recipe.Category.Should().Be(RecipeCategoryType.PowerGlove);
            recipe.Resref.Should().Be(reference.Resref.Replace("katar", "pglove"));
            recipe.Level.Should().Be(reference.Level);
            recipe.Quantity.Should().Be(reference.Quantity);
            recipe.Components.Should().BeEquivalentTo(reference.Components);
            recipe.EnhancementType.Should().Be(RecipeEnhancementType.Weapon);
            recipe.EnhancementSlots.Should().Be(reference.EnhancementSlots);
            recipe.ResearchCostModifier.Should().Be(reference.ResearchCostModifier);
            recipe.Requirements.Select(requirement => requirement.GetType()).Should()
                .BeEquivalentTo(reference.Requirements.Select(requirement => requirement.GetType()));
        }
    }

    [Test]
    public void TrainingAndOrdinaryMerchantsOfferCorrespondingPowerGloveTiers()
    {
        var additions = 0;
        foreach (var path in Directory.EnumerateFiles(Path.Combine(Root(), "Module/utm"), "*.utm.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var store in List(document.RootElement, "StoreList"))
            {
                var items = List(store, "ItemList").ToArray();
                foreach (var prefix in TierPrefixes)
                {
                    var existing = items.FirstOrDefault(item => Text(item, "InventoryRes") == prefix + "_katar");
                    if (existing.ValueKind == JsonValueKind.Undefined) continue;
                    var added = items.Single(item => Text(item, "InventoryRes") == prefix + "_pglove");
                    Int(added, "Infinite").Should().Be(Int(existing, "Infinite"), path);
                    additions++;
                }
            }
        }
        additions.Should().BeGreaterThan(0);
        using var training = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Module/utm/training_gear.utm.json")));
        List(training.RootElement, "StoreList").SelectMany(store => List(store, "ItemList"))
            .Should().Contain(item => Text(item, "InventoryRes") == "t_pglove");
    }

    [Test]
    public void ChiroTeachingBookAndCharmbreakerFollowTheirExistingLootSources()
    {
        var book = ReadItem("recipe_chipglove");
        Text(book, "Tag").Should().Be("RECIPE");
        Text(book, "TemplateResRef").Should().Be("recipe_chipglove");
        Text(List(book, "VarTable").Single(local => Text(local, "Name") == "RECIPES"), "Value")
            .Should().Be(((int)RecipeType.ChiroPowerGlove).ToString());
        List(book, "VarTable").Should().NotContain(local => Text(local, "Name") == "CHARACTER_TYPE" || Text(local, "Name") == "NO_ECONOMY");

        var tables = typeof(ILootTableDefinition).Assembly.GetTypes()
            .Where(type => !type.IsInterface && !type.IsAbstract && typeof(ILootTableDefinition).IsAssignableFrom(type))
            .SelectMany(type => ((ILootTableDefinition)Activator.CreateInstance(type)!).BuildLootTables()).ToArray();
        foreach (var (source, target) in new[] { ("recipe_chikatar", "recipe_chipglove"), ("charm_katar", "charm_pglove") })
        {
            var sources = tables.Where(table => table.Value.Any(entry => entry.Resref == source)).ToArray();
            sources.Should().NotBeEmpty();
            foreach (var table in sources)
            {
                var reference = table.Value.Single(entry => entry.Resref == source);
                var added = table.Value.Single(entry => entry.Resref == target);
                (added.Weight, added.MaxQuantity, added.IsRare).Should()
                    .Be((reference.Weight, reference.MaxQuantity, reference.IsRare), table.Key);
            }
        }
    }

    [Test]
    public void AppearanceCatalogOffersNamedCosmeticsWithValidBlankOuterParts()
    {
        var appearances = new PowerGloveAppearanceDefinition();
        appearances.IsSimple.Should().BeFalse();
        appearances.TopParts.Should().Equal(101);
        appearances.MiddleParts.Should().Equal(Enumerable.Range(101, 21));
        appearances.BottomParts.Should().Equal(101);
        appearances.MiddleParts.Select(partId => appearances.GetPartName(AppearanceWeapon.Middle, partId))
            .Should().Equal("Power Glove", "Vibroknuckler", "Blastknuckle", "Shock Glove", "Power Fist", "Bare Fists (Invisible)",
                "Rivet Knuckler", "Arc Knuckler", "Breacher Cestus", "Fang Claws", "Pulse Cestus", "Talon Knuckler",
                "Aegis Guard", "Cinder Knuckler", "Obsidian Cestus", "Rail Claws", "Thunder Knuckle", "Spine Cestus",
                "Ion Guard", "Impact Knuckler", "Relic Claws");
        appearances.GetPartName(AppearanceWeapon.Top, 101).Should().Be("Part #101");
        appearances.GetPartName(AppearanceWeapon.Bottom, 101).Should().Be("Part #101");
        new KatarAppearanceDefinition().GetPartName(AppearanceWeapon.Middle, 101).Should().Be("Part #101",
            "weapon catalogs without authored names retain their existing numeric picker labels");
    }

    [Test]
    public void EveryAppearanceHasACompiledModelAndNativeCompositeInventoryIcon()
    {
        var folder = Path.Combine(Root(), "SWLOR_Haks/sw_weapon");
        var middleParts = new PowerGloveAppearanceDefinition().MiddleParts
            .Select(id => (part: "m", native: id % 100 * 10 + id / 100, visible: id != 106));
        foreach (var (part, native, visible) in new[] { ("t", 11, false), ("b", 11, false) }.Concat(middleParts))
        foreach (var itemClass in new[] { "wpg", "wpl" })
        {
            var resref = $"{itemClass}_{part}_{native:000}";
            var model = File.ReadAllBytes(Path.Combine(folder, resref + ".mdl"));
            BitConverter.ToUInt32(model, 0).Should().Be(0, $"{resref} must be compiled by the native compiler");
            var modelLength = BitConverter.ToUInt32(model, 4);
            var rawLength = BitConverter.ToUInt32(model, 8);
            ((long)modelLength + rawLength + 12).Should().Be(model.Length);
            modelLength.Should().BeGreaterThan(232, "even an invisible part needs a present model with geometry root");
            if (visible) rawLength.Should().BeGreaterThan(0);
            else rawLength.Should().Be(0, "the bare-fist option and blank outer parts contain no rendered geometry");

            var icon = File.ReadAllBytes(Path.Combine(folder, "i" + resref + ".tga"));
            icon[2].Should().Be(2, "native composite inventory icons use uncompressed TGA, not DDS");
            BitConverter.ToUInt16(icon, 12).Should().Be(32);
            BitConverter.ToUInt16(icon, 14).Should().Be(32);
            icon[16].Should().Be(32);
            icon[17].Should().Be(8, "native inventory TGAs use bottom-left origin with alpha");
        }
        File.ReadAllBytes(Path.Combine(folder, "iwpg_m_061.tga")).Should()
            .Equal(File.ReadAllBytes(Path.Combine(folder, "iwpg_m_011.tga")),
                "the invisible equipped appearance must retain a visible inventory icon");
    }

    [Test]
    public void LeftHandModelsMirrorTheRightHandModelsAcrossTheHookXAxis()
    {
        var folder = Path.Combine(Root(), "SWLOR_Haks/sw_weapon");
        foreach (var id in new PowerGloveAppearanceDefinition().MiddleParts.Where(id => id != 106))
        {
            var native = $"m_{id % 100 * 10 + id / 100:000}";
            var right = File.ReadAllBytes(Path.Combine(folder, $"wpg_{native}.mdl"));
            var left = File.ReadAllBytes(Path.Combine(folder, $"wpl_{native}.mdl"));
            var (rightMin, rightMax) = MeshBounds(right);
            var (leftMin, leftMax) = MeshBounds(left);
            leftMin.X.Should().BeApproximately(-rightMax.X, .0001f, $"wpl_{native} mirrors the right hand across X");
            leftMax.X.Should().BeApproximately(-rightMin.X, .0001f, $"wpl_{native} mirrors the right hand across X");
            leftMin.Y.Should().BeApproximately(rightMin.Y, .0001f, $"wpl_{native} keeps the punching axis");
            leftMax.Y.Should().BeApproximately(rightMax.Y, .0001f, $"wpl_{native} keeps the punching axis");
        }
    }

    private static (System.Numerics.Vector3 Min, System.Numerics.Vector3 Max) MeshBounds(byte[] model)
    {
        // Compiled offsets are relative to the 12-byte file header: geometry root, its first child, then that
        // trimesh's bounding box after the 112-byte node header.
        var root = 12 + (int)BitConverter.ToUInt32(model, 12 + 72);
        var children = 12 + (int)BitConverter.ToUInt32(model, root + 72);
        var mesh = 12 + (int)BitConverter.ToUInt32(model, children) + 112 + 0x14;
        System.Numerics.Vector3 Read(int offset) => new(BitConverter.ToSingle(model, offset),
            BitConverter.ToSingle(model, offset + 4), BitConverter.ToSingle(model, offset + 8));
        return (Read(mesh), Read(mesh + 12));
    }

    private static (int Property, int Subtype, int Cost, int Table) PropertyBudget(JsonElement property) =>
        (Int(property, "PropertyName"), Int(property, "Subtype"), Int(property, "CostValue"), Int(property, "CostTable"));

    private static JsonElement ReadItem(string resref)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Module/uti", resref + ".uti.json")));
        return document.RootElement.Clone();
    }

    private static int Int(JsonElement node, string field) =>
        node.TryGetProperty(field, out var property) ? property.GetProperty("value").GetInt32() : 0;

    private static string Text(JsonElement node, string field) =>
        node.TryGetProperty(field, out var property) ? property.GetProperty("value").GetString() ?? string.Empty : string.Empty;

    private static IEnumerable<JsonElement> List(JsonElement node, string field) =>
        node.TryGetProperty(field, out var property) ? property.GetProperty("value").EnumerateArray() : Enumerable.Empty<JsonElement>();

    private static string Root()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate the SWLOR repository.");
    }
}
