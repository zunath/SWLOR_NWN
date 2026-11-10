using SWLOR.Game.Server.Domain.Crafting;

namespace SWLOR.CraftSim.Data
{
    /// <summary>
    /// The server's recipe level chart (<c>RecipeLevelChart.cs:13-99</c>), read from the chart class itself: the crafting service
    /// that serves it in the game needs the server's service container, which the simulator does not build.
    /// </summary>
    public static class ServerRecipeLevels
    {
        private static readonly RecipeLevelChart Chart = new();

        /// <summary>The durability, progress and quality of a recipe level, as <c>Craft.GetRecipeLevelDetail</c> gives them.</summary>
        public static RecipeLevelDetail For(int level) => Chart.GetByLevel(level);
    }
}
