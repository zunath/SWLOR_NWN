using System;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService
{
    public static class RecipeSearch
    {
        /// <summary>
        /// Determines whether a recipe matches the search text by the crafted item's name and,
        /// optionally, by the name of any of its components. Matching is a case-insensitive substring match.
        /// </summary>
        /// <param name="recipe">The recipe to check.</param>
        /// <param name="searchText">The text the player searched for.</param>
        /// <param name="includeComponents">Whether component names also count as a match.</param>
        /// <param name="getItemName">Resolves an item resref to its display name.</param>
        /// <returns>true if the recipe matches or the search text is blank, false otherwise.</returns>
        public static bool Matches(
            RecipeDetail recipe,
            string searchText,
            bool includeComponents,
            Func<string, string> getItemName)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            var text = searchText.Trim();
            if (NameContains(getItemName(recipe.Resref), text))
                return true;

            return includeComponents &&
                   recipe.Components != null &&
                   recipe.Components.Keys.Any(resref => NameContains(getItemName(resref), text));
        }

        private static bool NameContains(string name, string text)
        {
            return name != null && name.Contains(text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
