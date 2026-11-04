using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// The overrides a profile records in code. A category override replaces the named values of every
    /// recipe in the category, and a recipe override replaces them for one recipe and wins over its category.
    /// </summary>
    public sealed class RecipeParameterOverrides
    {
        public static RecipeParameterOverrides None => new RecipeParameterOverrides();

        private readonly Dictionary<RecipeCategoryType, RecipeParameterOverride> _categories =
            new Dictionary<RecipeCategoryType, RecipeParameterOverride>();
        private readonly Dictionary<RecipeType, RecipeParameterOverride> _recipes =
            new Dictionary<RecipeType, RecipeParameterOverride>();

        public IReadOnlyDictionary<RecipeCategoryType, RecipeParameterOverride> Categories => _categories;
        public IReadOnlyDictionary<RecipeType, RecipeParameterOverride> Recipes => _recipes;

        public RecipeParameterOverrides ForCategory(
            RecipeCategoryType category,
            string reason,
            params (string Name, double Value)[] values)
        {
            _categories.Add(category, Create(reason, values));
            return this;
        }

        public RecipeParameterOverrides ForRecipe(
            RecipeType recipe,
            string reason,
            params (string Name, double Value)[] values)
        {
            _recipes.Add(recipe, Create(reason, values));
            return this;
        }

        private static RecipeParameterOverride Create(string reason, (string Name, double Value)[] values)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("An override needs a reason.", nameof(reason));
            if (values.Length == 0)
                throw new ArgumentException("An override needs at least one value.", nameof(values));

            var byName = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var (name, value) in values)
            {
                byName.Add(name, value);
            }

            return new RecipeParameterOverride(reason.Trim(), byName);
        }
    }
}
