namespace SWLOR.CraftSim.Scenario.Kitchen
{
    /// <summary>The Kitchen played by a Slow Cooking Chef with the rest of the line its rank allows, Seasoned Palate off.</summary>
    public sealed class KitchenSlowCookingScenario : KitchenChefScenario
    {
        public KitchenSlowCookingScenario()
            : base("kitchen-slow-cooking", "Kitchen (Slow Cooking Chef)", KitchenBuild.SlowCookingLine)
        {
        }
    }
}
