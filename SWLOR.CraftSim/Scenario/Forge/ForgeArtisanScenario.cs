using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

namespace SWLOR.CraftSim.Scenario.Forge
{
    /// <summary>The Plasma Forge played by a crafter who has bought the Artisan line in full and plays Cold Working, for the gates a perked crafter must still pass.</summary>
    public sealed class ForgeArtisanScenario : ForgeScenario
    {
        public ForgeArtisanScenario()
            : base("forge-artisan", "Plasma Forge (Artisan line, Cold Working)", new ForgeLoadout(ForgeStyle.ColdWorking, false, true))
        {
        }
    }
}
