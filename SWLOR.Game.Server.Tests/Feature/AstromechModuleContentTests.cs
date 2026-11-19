using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AstromechService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Astromech modules copy the stats of the humanoid armor-slot item of the same slot and tier. Only the name, the skill the
/// item requires (Chassis instead of Armor) and the astromech-only limitation differ, so a later change to a humanoid item
/// fails here until tools/GenerateAstromechModules.py is rerun.
/// </summary>
public class AstromechModuleContentTests
{
    private const int AstromechRace = 170;
    private const int FirstRecipeId = 5071;

    private static readonly string[] HardenedPrefixes = { "bm", "fld_bul", "tit", "vet_bul", "qk", "prm_bul", "ar", "asc_bul", "ec", "ch" };
    private static readonly string[] TunedPrefixes = { "com", "fld_skm", "val", "vet_skm", "for", "prm_skm", "sur", "asc_skm", "sup", "imm" };
    private static readonly string[] ShortWordTiers = { "fld_", "vet_", "prm_", "asc_" };

    private static readonly (string Code, string Name, string Hardened, string Tuned, string HardenedShort, string TunedShort)[] Slots =
    {
        ("sens", "Sensor Package", "helmet", "cap", "helm", "cap"),
        ("plat", "Chassis Plating", "armor", "tunic", "armor", "tunic"),
        ("serv", "Manipulator Servo", "bracer", "gloves", "bracer", "gloves"),
        ("driv", "Drive Unit", "leggings", "boots", "legs", "boots"),
        ("cell", "Power Cell", "belt", "belt", "belt", "belt"),
        ("logc", "Logic Module", "necklace", "necklace", "neck", "neck"),
        ("sock", "Expansion Socket", "ring", "ring", "ring", "ring"),
        ("defl", "Deflector Emitter", "cloak", "cloak", "cloak", "cloak"),
    };

    private static readonly string[] Roman = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

    public record ModuleSpec(string Resref, string Source, string Name);

    private static readonly List<ModuleSpec> Modules = BuildModules();

    private static readonly Dictionary<RecipeType, RecipeDetail> Recipes = typeof(IRecipeListDefinition).Assembly
        .GetTypes()
        .Where(type => typeof(IRecipeListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
        .SelectMany(type => ((IRecipeListDefinition)Activator.CreateInstance(type)!).BuildRecipes())
        .GroupBy(pair => pair.Key)
        .ToDictionary(group => group.Key, group => group.First().Value);

    private static List<ModuleSpec> BuildModules()
    {
        var modules = new List<ModuleSpec>();
        foreach (var slot in Slots)
        {
            foreach (var (letter, ladder, prefixes) in new[] { ("h", "Hardened", HardenedPrefixes), ("t", "Tuned", TunedPrefixes) })
            {
                for (var tier = 0; tier < 10; tier++)
                {
                    var shortWord = ShortWordTiers.Any(prefixes[tier].StartsWith);
                    var suffix = (letter, shortWord) switch
                    {
                        ("h", false) => slot.Hardened,
                        ("h", true) => slot.HardenedShort,
                        (_, false) => slot.Tuned,
                        _ => slot.TunedShort,
                    };
                    modules.Add(new ModuleSpec($"am_{letter}_{slot.Code}_{tier + 1:00}", $"{prefixes[tier]}_{suffix}",
                        $"{ladder} {slot.Name} Mk {Roman[tier]}"));
                }
            }
        }

        return modules;
    }

    public static IEnumerable<TestCaseData> AllModules() => Modules.Select(module => new TestCaseData(module).SetArgDisplayNames(module.Resref));

    [Test]
    public void ThereAreOneHundredSixtyModules_OnePerSlotLadderAndTier()
    {
        Modules.Should().HaveCount(160);
        Modules.Select(module => module.Resref).Should().OnlyHaveUniqueItems();
        Modules.Select(module => module.Source).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void ResrefsFitTheEngineLimit_AndNamesAreUniqueAscii()
    {
        foreach (var module in Modules)
        {
            module.Resref.Length.Should().BeLessThanOrEqualTo(16, module.Resref);
            module.Resref.Should().MatchRegex("^[a-z0-9_]+$");
        }

        Modules.Select(module => module.Name).Should().OnlyHaveUniqueItems();
        foreach (var module in Modules)
        {
            var name = ReadBlueprint(module.Resref).GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString()!;
            name.Should().Be(module.Name);
            Encoding.UTF8.GetByteCount(name).Should().Be(name.Length, "module names are ASCII");
        }
    }

    [TestCaseSource(nameof(AllModules))]
    public void ModuleCopiesItsSourceExceptNameSkillAndRaceLimit(ModuleSpec module)
    {
        var source = ReadBlueprint(module.Source);
        var item = ReadBlueprint(module.Resref);

        Text(item, "Tag").Should().Be(module.Resref);
        Text(item, "TemplateResRef").Should().Be(module.Resref);

        // Chassis Plating paints the chassis with its dyes, so it wears a paint job in place of its source's dyes.
        var paint = new[] { "Cloth2Color", "Cloth1Color", "Metal2Color" };
        var plating = module.Resref.Contains("_plat_");
        var changed = new[] { "LocalizedName", "Tag", "TemplateResRef", "PropertiesList" }
            .Concat(plating ? paint : Array.Empty<string>())
            .ToArray();
        // Plating also drops the part models of the piece it copies: a leftover number can name a chassis part.
        bool Copied(string field) => !changed.Contains(field) && !(plating && IsPartModel(field));
        var sourceFields = source.EnumerateObject().Where(field => Copied(field.Name)).ToDictionary(field => field.Name, field => field.Value.GetRawText());
        var itemFields = item.EnumerateObject().Where(field => Copied(field.Name)).ToDictionary(field => field.Name, field => field.Value.GetRawText());
        itemFields.Should().BeEquivalentTo(sourceFields);
        if (plating)
            ShouldHaveThePartModelsOfPlating(item);

        var expected = Properties(source).Select(property =>
            property.PropertyName == (int)ItemPropertyType.RequiresSkill && property.Subtype == (int)SkillType.Armor
                ? property with { Subtype = (int)SkillType.Chassis }
                : property).ToList();
        expected.Count(property => property.Subtype == (int)SkillType.Chassis && property.PropertyName == (int)ItemPropertyType.RequiresSkill)
            .Should().Be(1, "the source has exactly one Armor requirement");
        expected.Add(new PropertyShape((int)ItemPropertyType.UseLimitationRacialType, AstromechRace, 0, 0, 255, 0, 100));

        Properties(item).Should().Equal(expected);
    }

    [Test]
    public void EveryChassisPlatingWearsAPaintJob_AndNoTwoOfATierMatch()
    {
        var jobs = new Dictionary<string, (int Main, int Accent, int Trim)>();
        foreach (var module in Modules.Where(module => module.Resref.Contains("_plat_")))
        {
            var item = ReadBlueprint(module.Resref);
            var job = (Number(item, "Cloth2Color"), Number(item, "Cloth1Color"), Number(item, "Metal2Color"));
            job.Item1.Should().NotBe(job.Item2, $"{module.Resref} needs an accent that shows on its main paint");
            jobs[module.Resref] = job;
        }

        jobs.Should().HaveCount(20);
        for (var tier = 1; tier <= 10; tier++)
            jobs[$"am_h_plat_{tier:00}"].Should().NotBe(jobs[$"am_t_plat_{tier:00}"]);
    }

    [Test]
    public void TheStarterPlatingHasNoStats_AndOnlyAnAstromechWearsIt()
    {
        var item = ReadBlueprint(Astromech.StarterPlatingResref);

        Text(item, "Tag").Should().Be(Astromech.StarterPlatingResref);
        Text(item, "TemplateResRef").Should().Be(Astromech.StarterPlatingResref);
        Number(item, "BaseItem").Should().Be((int)BaseItem.Armor);
        ShouldHaveThePartModelsOfPlating(item);
        Properties(item).Should().Equal(
            new PropertyShape((int)ItemPropertyType.UseLimitationRacialType, AstromechRace, 0, 0, 255, 0, 100));
        var name = item.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString();
        name.Should().Be("Basic Chassis Plating");
        var description = item.GetProperty("Description").GetProperty("value").GetProperty("0").GetString();
        description.Should().NotContain("clothes");
        Encoding.UTF8.GetByteCount(description).Should().Be(description.Length, "item text is ASCII");
    }

    [Test]
    public void EveryModuleHasOneEngineeringRecipeMatchingItsSourceSmitheryRecipe()
    {
        var smithery = Recipes.Values.Where(recipe => recipe.Skill == SkillType.Smithery).ToLookup(recipe => recipe.Resref);
        var problems = new List<string>();

        foreach (var module in Modules)
        {
            var recipes = Recipes.Where(pair => pair.Value.Resref == module.Resref).ToList();
            if (recipes.Count != 1)
            {
                problems.Add($"{module.Resref} has {recipes.Count} recipes");
                continue;
            }

            var recipe = recipes[0].Value;
            var original = smithery[module.Source].SingleOrDefault();
            if (original == null)
            {
                problems.Add($"{module.Source} has no single Smithery recipe");
                continue;
            }

            if (recipe.Skill != SkillType.Engineering) problems.Add($"{module.Resref} is not Engineering");
            if (recipe.Category != RecipeCategoryType.AstromechModule) problems.Add($"{module.Resref} category {recipe.Category}");
            if (recipe.Level != original.Level) problems.Add($"{module.Resref} level {recipe.Level} vs {original.Level}");
            if (recipe.Quantity != original.Quantity) problems.Add($"{module.Resref} quantity");
            if (recipe.EnhancementType != original.EnhancementType || recipe.EnhancementSlots != original.EnhancementSlots)
                problems.Add($"{module.Resref} enhancement slots");
            if (!recipe.Components.OrderBy(c => c.Key).SequenceEqual(original.Components.OrderBy(c => c.Key)))
                problems.Add($"{module.Resref} components");
            if (recipe.MinigameParameters == null) problems.Add($"{module.Resref} has no circuit parameters");
        }

        problems.Should().BeEmpty();
    }

    [Test]
    public void RecipeTypesFormOneContiguousBlock()
    {
        var names = Enum.GetNames<RecipeType>().Where(name => name.StartsWith("AstromechHardened") || name.StartsWith("AstromechTuned")).ToList();
        names.Should().HaveCount(160);
        var ids = names.Select(name => (int)Enum.Parse<RecipeType>(name)).OrderBy(id => id).ToList();
        ids.Should().Equal(Enumerable.Range(FirstRecipeId, 160));
        ((int)RecipeCategoryType.AstromechModule).Should().Be(75);
    }

    [Test]
    public void TheStarterMerchantSellsTheMkIModules()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(FindRoot(), "Module", "utm", "cz220_merchant.utm.json")));
        var sold = document.RootElement.GetProperty("StoreList").GetProperty("value").EnumerateArray()
            .SelectMany(tab => tab.GetProperty("ItemList").GetProperty("value").EnumerateArray())
            .Select(item => item.GetProperty("InventoryRes").GetProperty("value").GetString()).ToList();

        var mkI = Modules.Where(module => module.Resref.EndsWith("_01")).Select(module => module.Resref).ToList();
        mkI.Should().HaveCount(16);
        sold.Should().Contain(mkI);
        sold.Should().OnlyHaveUniqueItems();
    }

    private record PropertyShape(int PropertyName, int Subtype, int CostTable, int CostValue, int Param1, int Param1Value, int ChanceAppear);

    private static List<PropertyShape> Properties(JsonElement item) =>
        item.GetProperty("PropertiesList").GetProperty("value").EnumerateArray().Select(property => new PropertyShape(
            Int(property, "PropertyName"), Int(property, "Subtype"), Int(property, "CostTable"), Int(property, "CostValue"),
            Int(property, "Param1"), Int(property, "Param1Value"), Int(property, "ChanceAppear"))).ToList();

    private static int Int(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetInt32();

    private static string Text(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetString()!;

    private static int Number(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetInt32();

    private static bool IsPartModel(string field) => field.StartsWith("ArmorPart_") || field.StartsWith("xArmorPart_");

    // Plating draws nothing: every part is 0 but the torso, whose number has no model and carries the plating icon.
    private static void ShouldHaveThePartModelsOfPlating(JsonElement item)
    {
        var parts = PartModels(item);
        parts.Should().NotBeEmpty();
        foreach (var (field, value) in parts)
            value.Should().Be(field.EndsWith("_Torso") ? AstromechBody.PlatingTorso : 0, field);
    }

    private static Dictionary<string, int> PartModels(JsonElement item) =>
        item.EnumerateObject().Where(field => IsPartModel(field.Name))
            .ToDictionary(field => field.Name, field => field.Value.GetProperty("value").GetInt32());

    private static JsonElement ReadBlueprint(string resref)
    {
        var path = Path.Combine(FindRoot(), "Module", "uti", $"{resref}.uti.json");
        File.Exists(path).Should().BeTrue($"{resref} blueprint should exist");
        return JsonDocument.Parse(File.ReadAllBytes(path)).RootElement.Clone();
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
