using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

namespace SWLOR.CraftSim.Scenario.Forge
{
    /// <summary>The Plasma Forge played by a crafter who has bought both lines in full and plays Cold Working, the other side of the strongest crafter the perks allow.</summary>
    public sealed class ForgeBothLinesColdScenario : ForgeScenario
    {
        public ForgeBothLinesColdScenario()
            : base("forge-both-cold", "Plasma Forge (both lines, Cold Working)", new ForgeLoadout(ForgeStyle.ColdWorking, true, true))
        {
        }
    }
}
