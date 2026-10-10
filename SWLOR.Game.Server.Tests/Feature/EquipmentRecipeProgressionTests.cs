using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Feature;

public class EquipmentRecipeProgressionTests
{
    private static readonly Dictionary<RecipeType, RecipeDetail> Recipes = typeof(IRecipeListDefinition).Assembly
        .GetTypes()
        .Where(type => typeof(IRecipeListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
        .SelectMany(type => ((IRecipeListDefinition)Activator.CreateInstance(type)!).BuildRecipes())
        .GroupBy(pair => pair.Key)
        .ToDictionary(group => group.Key, group => group.First().Value);

    public static IEnumerable<TestCaseData> IntermediateRecipes() => Recipes
        .Where(pair => IsIntermediateEquipment(pair.Value))
        .Select(pair => new TestCaseData(pair.Key).SetName($"IntermediateIngredients_{pair.Key}"));

    [Test]
    public void IntermediateEquipment_CoversAllFourProgressionGaps()
    {
        var intermediate = Recipes.Values.Where(IsIntermediateEquipment).ToList();

        intermediate.Count(recipe => recipe.Skill == SkillType.Smithery).Should().Be(156);
        intermediate.Count(recipe => recipe.Skill == SkillType.Engineering).Should().Be(64);
    }

    [TestCaseSource(nameof(IntermediateRecipes))]
    public void IntermediateEquipment_HasDistinctCostsAndRequiresMoreThanItsPredecessor(RecipeType recipeType)
    {
        var recipe = Recipes[recipeType];
        var comparable = Recipes.Where(pair => pair.Key != recipeType && pair.Value.IsActive &&
                                                pair.Value.Skill == recipe.Skill && pair.Value.Category == recipe.Category)
            .ToList();

        var duplicates = comparable.Where(pair => ComponentsEqual(pair.Value.Components, recipe.Components))
            .Select(pair => pair.Key).ToList();
        duplicates.Should().BeEmpty($"{recipeType} must have distinct ingredient requirements within its equipment category");

        var predecessors = comparable.Where(pair => pair.Value.Level == recipe.Level - 5).ToList();
        predecessors.Should().Contain(pair =>
                recipe.Components.Count > pair.Value.Components.Count &&
                pair.Value.Components.All(component => recipe.Components.GetValueOrDefault(component.Key) > component.Value),
            $"{recipeType} should require more of its predecessor's materials plus a specialty ingredient");

        foreach (var (resref, quantity) in recipe.Components)
        {
            quantity.Should().BePositive();
            File.Exists(Path.Combine(FindRoot(), "Module", "uti", $"{resref}.uti.json")).Should().BeTrue(
                $"{recipeType}'s component {resref} must have an existing item blueprint");
        }
    }

    private static bool IsIntermediateEquipment(RecipeDetail recipe) => recipe.IsActive &&
        ((recipe.Skill == SkillType.Smithery && Regex.IsMatch(recipe.Resref, @"^(fld|vet|prm|asc)_")) ||
         (recipe.Skill == SkillType.Engineering && Regex.IsMatch(recipe.Resref, @"^d[hl][a-z]{2}00[1-4]a$")));

    private static bool ComponentsEqual(Dictionary<string, int> left, Dictionary<string, int> right) =>
        left.Count == right.Count && left.All(component => right.GetValueOrDefault(component.Key) == component.Value);

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
