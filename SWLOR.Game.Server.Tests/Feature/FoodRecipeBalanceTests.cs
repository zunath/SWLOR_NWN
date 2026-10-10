using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Feature;

public class FoodRecipeBalanceTests
{
    public static IEnumerable<TestCaseData> FoodRecipes() => typeof(IRecipeListDefinition).Assembly.GetTypes()
        .Where(type => typeof(IRecipeListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
        .SelectMany(type => ((IRecipeListDefinition)Activator.CreateInstance(type)!).BuildRecipes())
        .Where(pair => pair.Value.IsActive && pair.Value.Category == RecipeCategoryType.Food)
        .Select(pair => new TestCaseData(pair.Value).SetName($"FoodBudget_{pair.Key}"));

    [TestCaseSource(nameof(FoodRecipes))]
    public void FoodBonuses_StayWithinAttributeAndRegenerationBudgets(RecipeDetail recipe)
    {
        var bonuses = ReadBonuses(recipe.Resref);
        var attributes = new[] { FoodItemPropertySubType.Might, FoodItemPropertySubType.Vitality,
            FoodItemPropertySubType.Perception, FoodItemPropertySubType.Willpower,
            FoodItemPropertySubType.Agility, FoodItemPropertySubType.Social };
        var attributeTotal = bonuses.Where(pair => attributes.Contains(pair.Key)).Sum(pair => pair.Value);

        attributeTotal.Should().BeLessThanOrEqualTo(2, $"{recipe.Resref}: dedicated attribute foods grant two total points");
        if (bonuses.Keys.Except(attributes).Any())
            attributeTotal.Should().BeLessThanOrEqualTo(1, $"{recipe.Resref}: mixed-purpose food trades attribute potency for other bonuses");

        var tier = Math.Min(5, (recipe.Level + 9) / 10);
        foreach (var regeneration in new[] { FoodItemPropertySubType.HPRegen, FoodItemPropertySubType.FPRegen, FoodItemPropertySubType.STMRegen })
            bonuses.GetValueOrDefault(regeneration).Should().BeLessThanOrEqualTo(tier + 1,
                $"{recipe.Resref}: regeneration follows the food tier, allowing one extra point for specialty food");
        bonuses.GetValueOrDefault(FoodItemPropertySubType.RestRegen).Should().BeLessThanOrEqualTo(tier * 2,
            $"{recipe.Resref}: rest regeneration must not exceed the tier's dedicated meatball food");
    }

    [Test]
    public void CartelCakes_RewardRareIngredientsWithCompetitiveCombatBonuses()
    {
        var cakes = ReadBonuses("cartel_cakes");
        var broth = ReadBonuses("zoni_broth");
        var hybridFood = ReadBonuses("cooked_yayin");

        cakes.GetValueOrDefault(FoodItemPropertySubType.HP).Should()
            .BeGreaterThanOrEqualTo(hybridFood[FoodItemPropertySubType.HP],
                "the learned capstone recipe should offer HP comparable to other high-tier mixed foods");
        cakes.GetValueOrDefault(FoodItemPropertySubType.STMRegen).Should()
            .BeGreaterThan(broth[FoodItemPropertySubType.STMRegen],
                "the harder-to-source cakes should improve combat stamina recovery over Zoni Broth");
    }

    private static Dictionary<FoodItemPropertySubType, int> ReadBonuses(string resref)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRoot(), "Module", "uti", $"{resref}.uti.json")));
        return document.RootElement.GetProperty("PropertiesList").GetProperty("value").EnumerateArray()
            .Where(property => Value(property, "PropertyName") == 106)
            .GroupBy(property => (FoodItemPropertySubType)Value(property, "Subtype"))
            .ToDictionary(group => group.Key, group => group.Sum(property => Value(property, "CostValue")));
    }

    private static int Value(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetInt32();

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
