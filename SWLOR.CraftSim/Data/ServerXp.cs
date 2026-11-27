using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.CraftSim.Data
{
    /// <summary>The server's XP chart, read through the pure <see cref="SkillXPChart"/> tables (no container needed).</summary>
    public static class ServerXp
    {
        /// <summary>XP for an activity whose level is <paramref name="delta"/> above the crafter's rank.</summary>
        public static int ForDelta(int delta)
        {
            return SkillXPChart.GetDeltaXP(delta);
        }

        /// <summary>The base XP of one craft or activity, as <c>Craft.GetBaseRecipeXP</c> gives it: tiered activities use their practice XP.</summary>
        public static int ForRecipe(RecipeEntry recipe, int rank)
        {
            return recipe.PracticeRankLimit.HasValue
                ? SkillXPChart.GetPracticeXP(recipe.Level, rank, recipe.PracticeRankLimit.Value)
                : SkillXPChart.GetDeltaXP(recipe.Level - rank);
        }
    }
}
