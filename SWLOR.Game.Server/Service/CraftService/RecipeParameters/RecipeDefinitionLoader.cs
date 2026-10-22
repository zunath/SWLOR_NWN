using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// Builds the recipe registry from every <see cref="IRecipeListDefinition"/> in an assembly, without the
    /// game running. As in the live cache, the first registration of a recipe type wins.
    /// </summary>
    public static class RecipeDefinitionLoader
    {
        public static Dictionary<RecipeType, RecipeDetail> LoadGame()
        {
            return Load(typeof(IRecipeListDefinition).Assembly);
        }

        public static Dictionary<RecipeType, RecipeDetail> Load(Assembly assembly)
        {
            var registry = new Dictionary<RecipeType, RecipeDetail>();
            var definitionTypes = assembly.GetTypes()
                .Where(type => typeof(IRecipeListDefinition).IsAssignableFrom(type) &&
                               !type.IsInterface &&
                               !type.IsAbstract)
                .OrderBy(type => type.FullName, StringComparer.Ordinal);

            foreach (var type in definitionTypes)
            {
                var instance = (IRecipeListDefinition)Activator.CreateInstance(type);
                foreach (var (recipeType, detail) in instance.BuildRecipes())
                {
                    if (!registry.ContainsKey(recipeType))
                        registry[recipeType] = detail;
                }
            }

            return registry;
        }
    }
}
