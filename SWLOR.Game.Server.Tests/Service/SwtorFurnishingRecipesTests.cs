using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Service;

public class SwtorFurnishingRecipesTests
{
    private static readonly HashSet<string> MapScenery = new(StringComparer.OrdinalIgnoreCase)
    {
        "aswtor_090", "aswtor_091", "aswtor_111", "aswtor_112",
        "daf_sw011", "daf_sw012", "daf_sw015", "daf_sw021", "daf_sw022", "daf_sw023", "daf_sw101",
        "daf_sw231", "daf_sw232",
    };

    [Test]
    public void CataloguedSwtorDecorationModels_HaveExactlyOneFabricationRecipe()
    {
        var root = FindRoot();
        var placeables = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da"));
        var appearances = Directory.EnumerateFiles(Path.Combine(root, "Module", "utp"), "*.utp.json")
            .ToDictionary(
                path => Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path)),
                ReadAppearance,
                StringComparer.OrdinalIgnoreCase);
        var expected = appearances.Values
            .Where(row => placeables.GetValue(row, "Label")?.StartsWith("[SWTOR] ", StringComparison.Ordinal) == true)
            .Select(row => placeables.GetValue(row, "ModelName")!)
            .Where(model => !model.StartsWith("daf_planet", StringComparison.OrdinalIgnoreCase) && !MapScenery.Contains(model))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var actual = RecipeDefinitionLoader.LoadGame().Values
            .Where(recipe => recipe.IsActive && recipe.Skill == SkillType.Fabrication &&
                             recipe.Resref.StartsWith("structure_", StringComparison.Ordinal))
            .Select(recipe => GetStructure(recipe.Resref))
            .Where(structure => structure?.IsActive == true && appearances.ContainsKey(structure.Resref))
            .Select(structure => placeables.GetValue(appearances[structure!.Resref], "ModelName"))
            .Where(model => model != null && expected.Contains(model))
            .ToList();

        expected.Should().Contain("daf_sw378");
        actual.Should().BeEquivalentTo(expected, "each model should have one output even when several blueprints share it");
    }

    [Test]
    public void NewRecipes_ProduceUsablePropertyItems()
    {
        var root = FindRoot();
        var recipes = new SwtorFurnishingRecipes().BuildRecipes();
        recipes.Should().NotBeEmpty();
        foreach (var (type, recipe) in recipes)
        {
            var structure = GetStructure(recipe.Resref);
            structure.Should().NotBeNull(type.ToString());
            structure!.IsActive.Should().BeTrue();
            structure.CanBeRetrieved.Should().BeTrue();
            structure.LayoutType.Should().Be(PropertyLayoutType.Invalid);
            recipe.IsActive.Should().BeTrue();
            recipe.Requirements.Should().BeEmpty();
            File.Exists(Path.Combine(root, "Module", "utp", structure.Resref + ".utp.json")).Should().BeTrue();

            using var item = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "uti", recipe.Resref + ".uti.json")));
            var blueprint = item.RootElement;
            blueprint.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(recipe.Resref);
            blueprint.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be(structure.Name);
            blueprint.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(29);
            var property = blueprint.GetProperty("PropertiesList").GetProperty("value").EnumerateArray().Single();
            property.GetProperty("PropertyName").GetProperty("value").GetInt32().Should().Be(15);
            property.GetProperty("Subtype").GetProperty("value").GetInt32().Should().Be(329);
        }
    }

    private static StructureAttribute? GetStructure(string itemResref)
    {
        var type = (StructureType)int.Parse(itemResref["structure_".Length..]);
        return typeof(StructureType).GetField(type.ToString())?.GetCustomAttribute<StructureAttribute>();
    }

    private static int ReadAppearance(string path)
    {
        using var blueprint = JsonDocument.Parse(File.ReadAllText(path));
        return blueprint.RootElement.GetProperty("Appearance").GetProperty("value").GetInt32();
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
