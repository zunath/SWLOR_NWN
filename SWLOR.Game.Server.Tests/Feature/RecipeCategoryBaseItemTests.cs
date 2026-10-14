using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class RecipeCategoryBaseItemTests
{
    private static readonly Dictionary<RecipeCategoryType, BaseItem[]> AllowedBaseItems = new()
    {
        [RecipeCategoryType.Helmet] = new[] { BaseItem.Helmet },
        [RecipeCategoryType.Cap] = new[] { BaseItem.Helmet },
        [RecipeCategoryType.Tunic] = new[] { BaseItem.Armor },
        [RecipeCategoryType.Glove] = new[] { BaseItem.Gloves },
        [RecipeCategoryType.Bracer] = new[] { BaseItem.Bracer },
        [RecipeCategoryType.Belt] = new[] { BaseItem.Belt },
        [RecipeCategoryType.Cloak] = new[] { BaseItem.Cloak },
        [RecipeCategoryType.Boots] = new[] { BaseItem.Boots },
        [RecipeCategoryType.Necklace] = new[] { BaseItem.Amulet },
        [RecipeCategoryType.Ring] = new[] { BaseItem.Ring },
        [RecipeCategoryType.Shield] = new[] { BaseItem.SmallShield, BaseItem.LargeShield, BaseItem.TowerShield },
        [RecipeCategoryType.CompanionPistol] = new[] { BaseItem.CompanionPistol },
    };

    private static readonly Dictionary<RecipeType, RecipeDetail> Recipes = typeof(IRecipeListDefinition).Assembly
        .GetTypes()
        .Where(type => typeof(IRecipeListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
        .SelectMany(type => ((IRecipeListDefinition)Activator.CreateInstance(type)!).BuildRecipes())
        .GroupBy(pair => pair.Key)
        .ToDictionary(group => group.Key, group => group.First().Value);

    [Test]
    public void EquipmentRecipes_ProduceTheBaseItemOfTheirMenuCategory()
    {
        var root = FindRoot();
        var mismatches = new List<string>();

        foreach (var (type, recipe) in Recipes)
        {
            if (!AllowedBaseItems.TryGetValue(recipe.Category, out var allowed))
                continue;

            var path = Path.Combine(root, "Module", "uti", $"{recipe.Resref}.uti.json");
            if (!File.Exists(path))
            {
                mismatches.Add($"{type} ({recipe.Category}) -> {recipe.Resref} has no blueprint");
                continue;
            }

            using var uti = JsonDocument.Parse(File.ReadAllText(path));
            var baseItem = (BaseItem)uti.RootElement.GetProperty("BaseItem").GetProperty("value").GetInt32();
            if (!allowed.Contains(baseItem))
                mismatches.Add($"{type} ({recipe.Category}) -> {recipe.Resref} is {baseItem}");
        }

        mismatches.Should().BeEmpty();
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
