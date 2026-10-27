using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.CharacterizationTests.Definitions;

/// <summary>
/// Builds every recipe the way <c>Craft.CacheData</c> does at module load: each <see cref="IRecipeListDefinition"/>
/// builds its recipes. The engine is not involved (the load-time check of the crafted item's properties is the only
/// engine call and it only sets <see cref="RecipeDetail.IsItemIntendedForCrafting"/>, which is therefore not part of
/// the snapshots).
/// </summary>
internal static class RecipeDefinitionSource
{
    /// <summary>A recipe with the definition class that built it.</summary>
    public readonly record struct Entry(RecipeType Type, RecipeDetail Recipe, string Definition);

    /// <summary>All recipes sorted by recipe id. Fails when two definitions register the same recipe.</summary>
    public static IReadOnlyList<Entry> Load()
    {
        var recipes = new Dictionary<RecipeType, Entry>();

        foreach (var definitionType in DefinitionTypes())
        {
            var definition = (IRecipeListDefinition)Activator.CreateInstance(definitionType, nonPublic: true)!;
            foreach (var (recipeType, recipe) in definition.BuildRecipes())
            {
                if (recipes.TryGetValue(recipeType, out var existing))
                    throw new InvalidOperationException($"{recipeType} is built by both {existing.Definition} and {definitionType.FullName}.");

                recipes[recipeType] = new Entry(recipeType, recipe, definitionType.FullName!);
            }
        }

        return recipes.Values.OrderBy(entry => DefinitionCell.Id(entry.Type)).ToArray();
    }

    /// <summary>Every recipe definition class, in a fixed order.</summary>
    public static IReadOnlyList<Type> DefinitionTypes()
    {
        return typeof(IRecipeListDefinition).Assembly
            .GetTypes()
            .Where(type => typeof(IRecipeListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }
}
