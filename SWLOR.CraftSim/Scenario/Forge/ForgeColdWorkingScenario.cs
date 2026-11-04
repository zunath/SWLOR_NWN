using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

namespace SWLOR.CraftSim.Scenario.Forge
{
    /// <summary>The Plasma Forge played by a crafter who has bought Cold Working and no line perk, for the gates a style alone must still pass.</summary>
    public sealed class ForgeColdWorkingScenario : ForgeScenario
    {
        public ForgeColdWorkingScenario()
            : base("forge-cold", "Plasma Forge (Cold Working alone)", new ForgeLoadout(ForgeStyle.ColdWorking, false, false))
        {
        }
    }
}
