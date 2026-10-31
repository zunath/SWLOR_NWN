using SWLOR.Game.Server.Service;

namespace SWLOR.CraftSim.Data
{
    /// <summary>The server's XP chart; GetDeltaXP needs the cap that CacheXPChartData sets at module load, so it runs once first.</summary>
    public static class ServerXp
    {
        private static readonly object Gate = new();
        private static volatile bool _loaded;

        /// <summary>XP for an activity whose level is <paramref name="delta"/> above the crafter's rank.</summary>
        public static int ForDelta(int delta)
        {
            EnsureLoaded();
            return Skill.GetDeltaXP(delta);
        }

        /// <summary>The base XP of one craft or activity, as <c>Craft.GetBaseRecipeXP</c> gives it: tiered activities use their practice XP.</summary>
        public static int ForRecipe(RecipeEntry recipe, int rank)
        {
            EnsureLoaded();
            return recipe.PracticeRankLimit.HasValue
                ? Skill.GetPracticeXP(recipe.Level, rank, recipe.PracticeRankLimit.Value)
                : Skill.GetDeltaXP(recipe.Level - rank);
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            lock (Gate)
            {
                if (_loaded)
                    return;

                Skill.CacheXPChartData();
                _loaded = true;
            }
        }
    }
}
