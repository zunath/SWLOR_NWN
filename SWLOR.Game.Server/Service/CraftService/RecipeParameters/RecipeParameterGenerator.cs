using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// Turns a recipe into minigame parameters. The profile's base values come from the recipe's category,
    /// level and components. Each varied parameter then moves by a draw seeded by the profile key, the
    /// <see cref="RecipeType"/> name and the parameter name, and the first draw whose difficulty fits the
    /// level's budget wins. Recorded overrides are applied last.
    /// </summary>
    public static class RecipeParameterGenerator
    {
        /// <summary>The number of variation draws tried before falling back to the unvaried base values.</summary>
        public const int MaxAttempts = 32;

        public static GeneratedRecipeParameters Generate(IRecipeParameterProfile profile, RecipeParameterContext recipe)
        {
            var problems = RecipeParameterAudit.ValidateProfile(profile);
            if (problems.Count > 0)
                throw new InvalidOperationException($"Profile {profile.Key} is invalid: {string.Join("; ", problems)}");

            var definitions = profile.Definitions;
            var baseValues = ReadBaseValues(profile, recipe);
            var overrides = ApplicableOverrides(profile, recipe);
            var overridden = new HashSet<string>(overrides.SelectMany(o => o.Values.Keys), StringComparer.Ordinal);
            var varied = definitions.Where(d => d.Variation > 0 && !overridden.Contains(d.Name)).ToList();
            var budget = profile.BudgetFor(recipe.Level);
            var reason = string.Join("; ", overrides.Select(o => o.Reason));
            var recipeName = recipe.Type.ToString();

            var lastDifficulty = 0.0;
            for (var attempt = 0; attempt <= MaxAttempts; attempt++)
            {
                var useVariation = varied.Count > 0 && attempt < MaxAttempts;
                var values = new Dictionary<string, double>(baseValues, StringComparer.Ordinal);
                if (useVariation)
                {
                    foreach (var definition in varied)
                    {
                        var unit = RecipeParameterSeed.Unit(profile.Key, recipeName, definition.Name, attempt);
                        values[definition.Name] = definition.Vary(baseValues[definition.Name], unit);
                    }
                }

                ApplyOverrides(profile, recipe, overrides, values);

                var parameters = profile.Create(new RecipeParameterValues(values));
                lastDifficulty = profile.MeasureDifficulty(parameters);
                if (budget.Contains(lastDifficulty))
                {
                    var resultAttempt = useVariation ? attempt : (varied.Count > 0 ? -1 : 0);
                    return new GeneratedRecipeParameters(parameters, values, resultAttempt, reason, lastDifficulty);
                }

                if (varied.Count == 0)
                    break;
            }

            throw new InvalidOperationException(
                $"Profile {profile.Key} produced no parameters within the difficulty budget {budget} for " +
                $"{recipe.Type} (level {recipe.Level}); the last difficulty was {lastDifficulty}. " +
                "Adjust the profile's base values, bounds or budget, or the recipe's overrides.");
        }

        /// <summary>
        /// The overrides that apply to a recipe in the order they are applied: its category's, then its own.
        /// </summary>
        public static IReadOnlyList<RecipeParameterOverride> ApplicableOverrides(
            IRecipeParameterProfile profile,
            RecipeParameterContext recipe)
        {
            var result = new List<RecipeParameterOverride>();
            if (profile.Overrides.Categories.TryGetValue(recipe.Category, out var category))
                result.Add(category);
            if (profile.Overrides.Recipes.TryGetValue(recipe.Type, out var single))
                result.Add(single);

            return result;
        }

        private static Dictionary<string, double> ReadBaseValues(IRecipeParameterProfile profile, RecipeParameterContext recipe)
        {
            var provided = profile.BaseValues(recipe);
            var values = new Dictionary<string, double>(StringComparer.Ordinal);

            foreach (var definition in profile.Definitions)
            {
                if (!provided.TryGetValue(definition.Name, out var value))
                    throw new InvalidOperationException($"Profile {profile.Key} gave no base value for {definition.Name} on {recipe.Type}.");

                if (!definition.IsInBounds(value))
                {
                    throw new InvalidOperationException(
                        $"Profile {profile.Key} base value {definition.Name}={value} on {recipe.Type} " +
                        $"is outside [{definition.Min}, {definition.Max}].");
                }

                values[definition.Name] = definition.Snap(value);
            }

            var unknown = provided.Keys.Where(name => !values.ContainsKey(name)).ToList();
            if (unknown.Count > 0)
                throw new InvalidOperationException($"Profile {profile.Key} gave base values for undeclared parameters: {string.Join(", ", unknown)}.");

            return values;
        }

        private static void ApplyOverrides(
            IRecipeParameterProfile profile,
            RecipeParameterContext recipe,
            IReadOnlyList<RecipeParameterOverride> overrides,
            Dictionary<string, double> values)
        {
            foreach (var recipeOverride in overrides)
            {
                foreach (var (name, value) in recipeOverride.Values)
                {
                    var problem = RecipeParameterAudit.CheckValue(profile, name, value);
                    if (problem != null)
                        throw new InvalidOperationException($"Override on {recipe.Type} ({recipeOverride.Reason}): {problem}");

                    values[name] = value;
                }
            }
        }
    }
}
