using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// What a recipe's parameters look like in the Bible recipe tabs: one column per parameter definition,
    /// then an override column that holds the reason when the recipe's values were overridden.
    /// </summary>
    public static class RecipeParameterBibleColumns
    {
        public static string OverrideHeader(IRecipeParameterProfile profile)
        {
            return profile.Key + " Override";
        }

        /// <summary>Every header the profile owns on a recipe tab, in column order.</summary>
        public static IReadOnlyList<string> Headers(IRecipeParameterProfile profile)
        {
            return profile.Definitions.Select(d => d.Header).Append(OverrideHeader(profile)).ToList();
        }

        /// <summary>The cell text for each header of a recipe that has the profile's parameters.</summary>
        public static IReadOnlyList<RecipeParameterBibleCell> Cells(
            IRecipeParameterProfile profile,
            RecipeMinigameParameters parameters,
            string overrideReason)
        {
            var values = parameters.Values.ToDictionary(v => v.Name, v => v.Value, StringComparer.Ordinal);
            var cells = new List<RecipeParameterBibleCell>();

            foreach (var definition in profile.Definitions)
            {
                if (!values.TryGetValue(definition.Name, out var value))
                {
                    throw new InvalidOperationException(
                        $"{parameters.BuilderMethod} has no value named {definition.Name}.");
                }

                cells.Add(new RecipeParameterBibleCell(
                    definition.Header,
                    definition.FormatDisplay(value),
                    definition.EnumType == null));
            }

            cells.Add(new RecipeParameterBibleCell(OverrideHeader(profile), overrideReason ?? string.Empty, false));
            return cells;
        }
    }
}
