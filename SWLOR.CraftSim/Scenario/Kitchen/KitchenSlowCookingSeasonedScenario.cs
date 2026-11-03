namespace SWLOR.CraftSim.Scenario.Kitchen
{
    /// <summary>The Kitchen played by a Slow Cooking Chef with the rest of the line its rank allows, Seasoned Palate on for every dish.</summary>
    public sealed class KitchenSlowCookingSeasonedScenario : KitchenChefScenario
    {
        public KitchenSlowCookingSeasonedScenario()
            : base("kitchen-slow-cooking-seasoned", "Kitchen (Slow Cooking Chef, Seasoned Palate on)", KitchenBuild.SlowCookingLineSeasoned)
        {
        }
    }
}
