namespace SWLOR.CraftSim.Scenario.Kitchen
{
    /// <summary>The Kitchen played by a Flash Cooking Chef with the rest of the line its rank allows, Seasoned Palate off.</summary>
    public sealed class KitchenFlashCookingScenario : KitchenChefScenario
    {
        public KitchenFlashCookingScenario()
            : base("kitchen-flash-cooking", "Kitchen (Flash Cooking Chef)", KitchenBuild.FlashCookingLine)
        {
        }
    }
}
