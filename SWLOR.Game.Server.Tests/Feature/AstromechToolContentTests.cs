using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Astromech tools (Shock Prods and Blaster Mounts) copy the stats of the humanoid quarterstaff or rifle of the same tier. Only
/// the name, the base item, the skill the item requires (Armaments instead of Staff or Rifle) and the astromech-only
/// limitation differ, so a later change to a humanoid weapon fails here until tools/GenerateAstromechTools.py is rerun.
/// </summary>
public class AstromechToolContentTests
{
    private const int AstromechRace = 170;
    private const int FirstRecipeId = 5231;

    private static readonly string[] Prefixes = { "b", "fld", "tit", "vet", "del", "prm", "proto", "asc", "oph", "chi" };
    private static readonly string[] TierNames =
        { "Basic", "Field", "Titan", "Veteran", "Delta", "Prime", "Proto", "Ascendant", "Ophidian", "Chiro" };

    public record ToolSpec(string Resref, string Source, string Name, BaseItem BaseItem, SkillType SourceSkill);

    private static readonly List<ToolSpec> Tools = BuildTools();

    private static readonly Dictionary<RecipeType, RecipeDetail> Recipes = typeof(IRecipeListDefinition).Assembly
        .GetTypes()
        .Where(type => typeof(IRecipeListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
        .SelectMany(type => ((IRecipeListDefinition)Activator.CreateInstance(type)!).BuildRecipes())
        .GroupBy(pair => pair.Key)
        .ToDictionary(group => group.Key, group => group.First().Value);

    private static List<ToolSpec> BuildTools()
    {
        var tools = new List<ToolSpec>();
        for (var tier = 0; tier < 10; tier++)
            tools.Add(new ToolSpec($"am_w_prod_{tier + 1:00}", $"{Prefixes[tier]}_staff", $"{TierNames[tier]} Shock Prod",
                BaseItem.ShockProd, SkillType.Staff));
        for (var tier = 0; tier < 10; tier++)
            tools.Add(new ToolSpec($"am_w_blst_{tier + 1:00}", $"{Prefixes[tier]}_rifle", $"{TierNames[tier]} Blaster Mount",
                BaseItem.BlasterMount, SkillType.Rifle));
        return tools;
    }

    public static IEnumerable<TestCaseData> AllTools() => Tools.Select(tool => new TestCaseData(tool).SetArgDisplayNames(tool.Resref));

    [Test]
    public void ThereAreTwentyTools_TenShockProdsAndTenBlasterMounts()
    {
        Tools.Should().HaveCount(20);
        Tools.Select(tool => tool.Resref).Should().OnlyHaveUniqueItems();
        Tools.Select(tool => tool.Source).Should().OnlyHaveUniqueItems();
        Tools.Count(tool => tool.BaseItem == BaseItem.ShockProd).Should().Be(10);
        Tools.Count(tool => tool.BaseItem == BaseItem.BlasterMount).Should().Be(10);
    }

    [Test]
    public void ResrefsFitTheEngineLimit_AndNamesAreUniqueAscii()
    {
        foreach (var tool in Tools)
        {
            tool.Resref.Length.Should().BeLessThanOrEqualTo(16, tool.Resref);
            tool.Resref.Should().MatchRegex("^[a-z0-9_]+$");
        }

        Tools.Select(tool => tool.Name).Should().OnlyHaveUniqueItems();
        foreach (var tool in Tools)
        {
            var name = ReadBlueprint(tool.Resref).GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString()!;
            name.Should().Be(tool.Name);
            Encoding.UTF8.GetByteCount(name).Should().Be(name.Length, "tool names are ASCII");
        }
    }

    [TestCaseSource(nameof(AllTools))]
    public void ToolCopiesItsSourceExceptNameBaseItemSkillAndRaceLimit(ToolSpec tool)
    {
        var source = ReadBlueprint(tool.Source);
        var item = ReadBlueprint(tool.Resref);

        Text(item, "Tag").Should().Be(tool.Resref);
        Text(item, "TemplateResRef").Should().Be(tool.Resref);
        Int(source, "BaseItem").Should().Be(tool.BaseItem == BaseItem.ShockProd ? (int)BaseItem.QuarterStaff : (int)BaseItem.Rifle);
        Int(item, "BaseItem").Should().Be((int)tool.BaseItem);

        var changed = new[] { "LocalizedName", "Tag", "TemplateResRef", "BaseItem", "PropertiesList" };
        var sourceFields = source.EnumerateObject().Where(field => !changed.Contains(field.Name)).ToDictionary(field => field.Name, field => field.Value.GetRawText());
        var itemFields = item.EnumerateObject().Where(field => !changed.Contains(field.Name)).ToDictionary(field => field.Name, field => field.Value.GetRawText());
        itemFields.Should().BeEquivalentTo(sourceFields);

        var expected = Properties(source).Select(property =>
            property.PropertyName == (int)ItemPropertyType.RequiresSkill && property.Subtype == (int)tool.SourceSkill
                ? property with { Subtype = (int)SkillType.Armaments }
                : property).ToList();
        expected.Count(property => property.Subtype == (int)SkillType.Armaments && property.PropertyName == (int)ItemPropertyType.RequiresSkill)
            .Should().Be(1, "the source has exactly one weapon skill requirement");
        expected.Add(new PropertyShape((int)ItemPropertyType.UseLimitationRacialType, AstromechRace, 0, 0, 255, 0, 100));

        Properties(item).Should().Equal(expected);
    }

    [Test]
    public void EveryToolHasOneEngineeringRecipeMatchingItsSourceSmitheryRecipe()
    {
        var smithery = Recipes.Values.Where(recipe => recipe.Skill == SkillType.Smithery).ToLookup(recipe => recipe.Resref);
        var problems = new List<string>();

        foreach (var tool in Tools)
        {
            var recipes = Recipes.Where(pair => pair.Value.Resref == tool.Resref).ToList();
            if (recipes.Count != 1)
            {
                problems.Add($"{tool.Resref} has {recipes.Count} recipes");
                continue;
            }

            var recipe = recipes[0].Value;
            var original = smithery[tool.Source].SingleOrDefault();
            if (original == null)
            {
                problems.Add($"{tool.Source} has no single Smithery recipe");
                continue;
            }

            if (recipe.Skill != SkillType.Engineering) problems.Add($"{tool.Resref} is not Engineering");
            if (recipe.Category != RecipeCategoryType.AstromechTool) problems.Add($"{tool.Resref} category {recipe.Category}");
            if (recipe.Level != original.Level) problems.Add($"{tool.Resref} level {recipe.Level} vs {original.Level}");
            if (recipe.Quantity != original.Quantity) problems.Add($"{tool.Resref} quantity");
            if (recipe.EnhancementType != original.EnhancementType || recipe.EnhancementSlots != original.EnhancementSlots)
                problems.Add($"{tool.Resref} enhancement slots");
            if (!recipe.Components.OrderBy(c => c.Key).SequenceEqual(original.Components.OrderBy(c => c.Key)))
                problems.Add($"{tool.Resref} components");
            if (recipe.MinigameParameters == null) problems.Add($"{tool.Resref} has no circuit parameters");

            // A weapon taught by a book is learned with its humanoid form: the tool needs that recipe, not a second book.
            var sourceType = Recipes.Single(pair => pair.Value == original).Key;
            var sourceLocked = original.Requirements.OfType<RecipeUnlockRequirement>().Any(r => r.Recipe == sourceType);
            var toolRequirements = recipe.Requirements.OfType<RecipeUnlockRequirement>().Select(r => r.Recipe).ToList();
            if (sourceLocked)
                toolRequirements.Should().Equal(new[] { sourceType }, $"{tool.Resref} is learned with {sourceType}");
            else
                toolRequirements.Should().BeEmpty($"{tool.Resref} copies an unlocked recipe");
        }

        problems.Should().BeEmpty();
    }

    [Test]
    public void RecipeTypesFormOneContiguousBlock()
    {
        var names = Enum.GetNames<RecipeType>().Where(name => name.StartsWith("AstromechShockProd") || name.StartsWith("AstromechBlasterMount")).ToList();
        names.Should().HaveCount(20);
        var ids = names.Select(name => (int)Enum.Parse<RecipeType>(name)).OrderBy(id => id).ToList();
        ids.Should().Equal(Enumerable.Range(FirstRecipeId, 20));
        ((int)RecipeCategoryType.AstromechTool).Should().Be(76);
    }

    [Test]
    public void TheStarterMerchantSellsTheMkITools()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(FindRoot(), "Module", "utm", "cz220_merchant.utm.json")));
        var sold = document.RootElement.GetProperty("StoreList").GetProperty("value").EnumerateArray()
            .SelectMany(tab => tab.GetProperty("ItemList").GetProperty("value").EnumerateArray())
            .Select(item => item.GetProperty("InventoryRes").GetProperty("value").GetString()).ToList();

        sold.Should().Contain(new[] { "am_w_prod_01", "am_w_blst_01" });
        sold.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void GeneratedFilesMatchTheGenerator()
    {
        var python = FindPython();
        if (python == null)
            Assert.Ignore("python is not available; tools/tests/test_generate_astromech_tools.py runs the same check.");

        var start = new ProcessStartInfo(python, $"-B \"{Path.Combine(FindRoot(), "tools", "GenerateAstromechTools.py")}\" --check")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.Should().Be(0, output);
    }

    private static string? FindPython()
    {
        foreach (var name in new[] { "python", "python3" })
        {
            try
            {
                using var probe = Process.Start(new ProcessStartInfo(name, "--version")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                });
                probe!.WaitForExit();
                if (probe.ExitCode == 0)
                    return name;
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }
        }

        return null;
    }

    private record PropertyShape(int PropertyName, int Subtype, int CostTable, int CostValue, int Param1, int Param1Value, int ChanceAppear);

    private static List<PropertyShape> Properties(JsonElement item) =>
        item.GetProperty("PropertiesList").GetProperty("value").EnumerateArray().Select(property => new PropertyShape(
            Int(property, "PropertyName"), Int(property, "Subtype"), Int(property, "CostTable"), Int(property, "CostValue"),
            Int(property, "Param1"), Int(property, "Param1Value"), Int(property, "ChanceAppear"))).ToList();

    private static int Int(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetInt32();

    private static string Text(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetString()!;

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
