using System.Reflection;
using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>
    /// Which recipes the research terminal accepts, asked of the server itself: <c>Craft.IsResearchableRecipe</c> decides at load which recipes
    /// the research window lists (<c>Craft.GetResearchableRecipes*</c>), and <c>Craft.CanPlayerResearchRecipe</c> then only checks the tier.
    /// </summary>
    public static class ServerResearchRules
    {
        private static readonly Lazy<Func<RecipeDetail, bool>> Predicate = new(() =>
        {
            // The space revamp made the rule public (its Research Terminal tests ask it directly); either visibility is the same rule.
            var method = typeof(Craft).GetMethod("IsResearchableRecipe", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidDataException("Craft no longer has an IsResearchableRecipe method; update ServerResearchRules.");
            return detail => (bool)method.Invoke(null, new object[] { detail })!;
        });

        /// <summary>Whether the research window lists the recipe; false for a hand-written entry with no server definition.</summary>
        public static bool IsResearchable(RecipeEntry recipe) => recipe.Detail != null && Predicate.Value(recipe.Detail);
    }
}
