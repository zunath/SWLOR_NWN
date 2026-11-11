namespace SWLOR.CraftSim.Scenario.Kitchen
{
    /// <summary>The Kitchen played by a Flash Cooking Chef with the rest of the line its rank allows, Seasoned Palate on for every dish.</summary>
    public sealed class KitchenFlashCookingSeasonedScenario : KitchenChefScenario
    {
        public KitchenFlashCookingSeasonedScenario()
            : base("kitchen-flash-cooking-seasoned", "Kitchen (Flash Cooking Chef, Seasoned Palate on)", KitchenBuild.FlashCookingLineSeasoned)
        {
        }
    }
}
