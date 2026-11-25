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
    [Test]
    public void CataloguedSwtorBeds_HaveExactlyOneFabricationRecipe()
    {
        var root = FindRoot();
        var placeables = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da"));
        var appearances = Directory.EnumerateFiles(Path.Combine(root, "Module", "utp"), "*.utp.json")
            .ToDictionary(
                path => Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path)),
                ReadAppearance,
                StringComparer.OrdinalIgnoreCase);
        var expected = appearances.Values
            .Where(row => placeables.GetValue(row, "Label")?.StartsWith("[SWTOR] Bed -", StringComparison.Ordinal) == true)
            .Select(row => placeables.GetValue(row, "ModelName")!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var actual = RecipeDefinitionLoader.LoadGame().Values
            .Where(recipe => recipe.IsActive && recipe.Skill == SkillType.Fabrication &&
                             recipe.Resref.StartsWith("structure_", StringComparison.Ordinal))
            .Select(recipe => GetStructure(recipe.Resref))
            .Where(structure => structure?.IsActive == true && appearances.ContainsKey(structure.Resref))
            .Select(structure => placeables.GetValue(appearances[structure!.Resref], "ModelName"))
            .Where(model => model != null && expected.Contains(model))
            .ToList();

        expected.Should().HaveCount(14);
        actual.Should().BeEquivalentTo(expected, "each model should have one output even when several blueprints share it");
    }

    [TestCase("Flower")]
    [TestCase("Elevator")]
    [TestCase("Vehicle")]
    public void HouseholdFurnishingRecipes_ExcludeNonFurnishings(string placeableName)
    {
        var root = FindRoot();
        var placeables = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da"));
        var excludedModels = Enumerable.Range(0, placeables.RowCount)
            .Where(row => placeables.GetValue(row, "Label")?.StartsWith($"[SWTOR] {placeableName}", StringComparison.Ordinal) == true)
            .Select(row => placeables.GetValue(row, "ModelName"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        excludedModels.Should().NotBeEmpty();

        var householdModels = GetSwtorFurnitureRecipes()
            .Select(recipe => GetStructure(recipe.Value.Resref)!.Resref)
            .Select(resref => ReadAppearance(Path.Combine(root, "Module", "utp", resref + ".utp.json")))
            .Select(row => placeables.GetValue(row, "ModelName"));
        householdModels.Should().NotIntersectWith(excludedModels);
    }

    [Test]
    public void NewRecipes_ProduceUsablePropertyItems()
    {
        var root = FindRoot();
        var recipes = GetSwtorFurnitureRecipes().ToList();
        recipes.Should().HaveCount(61);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (type, recipe) in recipes)
        {
            var structure = GetStructure(recipe.Resref);
            structure.Should().NotBeNull(type.ToString());
            structure!.IsActive.Should().BeTrue();
            structure.CanBeRetrieved.Should().BeTrue();
            structure.LayoutType.Should().Be(PropertyLayoutType.Invalid);
            recipe.IsActive.Should().BeTrue();
            recipe.Requirements.Should().BeEmpty();
            names.Add(structure.Name).Should().BeTrue($"the furnishing name must be unique for {type}");

            using var placeable = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "utp", structure.Resref + ".utp.json")));
            placeable.RootElement.GetProperty("LocName").GetProperty("value").GetProperty("0").GetString()
                .Should().Be(structure.Name, "the first placement uses the blueprint's name");

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

    private static IEnumerable<KeyValuePair<RecipeType, RecipeDetail>> GetSwtorFurnitureRecipes()
    {
        return new FurnitureRecipes().BuildRecipes()
            .Where(recipe => recipe.Key.ToString().StartsWith("Swtor", StringComparison.Ordinal));
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
