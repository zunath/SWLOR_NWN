using System.Reflection;
using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>
    /// Which recipes the research terminal accepts, asked of the server itself: <c>CraftService.IsResearchableRecipe</c> decides at load which recipes
    /// the research window lists (<c>Craft.GetResearchableRecipes*</c>), and <c>Craft.CanPlayerResearchRecipe</c> then only checks the tier.
    /// </summary>
    public static class ServerResearchRules
    {
        private static readonly Lazy<Func<RecipeDetail, bool>> Predicate = new(() =>
        {
            // The rule is a private method of the crafting service instance that reads no state; the instance is built without
            // neighbours because the simulator has no service container.
            var method = typeof(CraftService).GetMethod("IsResearchableRecipe", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidDataException("CraftService no longer has an IsResearchableRecipe method; update ServerResearchRules.");
            var service = new CraftService(null, null, null, null, null!, null!, null!, null!, null!);
            return detail => (bool)method.Invoke(service, new object[] { detail })!;
        });

        /// <summary>Whether the research window lists the recipe; false for a hand-written entry with no server definition.</summary>
        public static bool IsResearchable(RecipeEntry recipe) => recipe.Detail != null && Predicate.Value(recipe.Detail);
    }
}
