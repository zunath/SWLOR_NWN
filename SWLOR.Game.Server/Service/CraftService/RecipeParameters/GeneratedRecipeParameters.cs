using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// The generator's result for one recipe.
    /// </summary>
    public sealed class GeneratedRecipeParameters
    {
        public RecipeMinigameParameters Parameters { get; }

        /// <summary>The final value of every definition, by name.</summary>
        public IReadOnlyDictionary<string, double> Values { get; }

        /// <summary>The variation draw that fit the budget; -1 when the recipe uses its unvaried base values.</summary>
        public int Attempt { get; }

        /// <summary>The reasons of the overrides applied to this recipe, or an empty string.</summary>
        public string OverrideReason { get; }

        public double Difficulty { get; }

        public GeneratedRecipeParameters(
            RecipeMinigameParameters parameters,
            IReadOnlyDictionary<string, double> values,
            int attempt,
            string overrideReason,
            double difficulty)
        {
            Parameters = parameters;
            Values = values;
            Attempt = attempt;
            OverrideReason = overrideReason;
            Difficulty = difficulty;
        }
    }
}
