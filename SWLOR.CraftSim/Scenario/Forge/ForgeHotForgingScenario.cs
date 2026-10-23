using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

namespace SWLOR.CraftSim.Scenario.Forge
{
    /// <summary>The Plasma Forge played by a crafter who has bought Hot Forging and no line perk, for the gates a style alone must still pass.</summary>
    public sealed class ForgeHotForgingScenario : ForgeScenario
    {
        public ForgeHotForgingScenario()
            : base("forge-hot", "Plasma Forge (Hot Forging alone)", new ForgeLoadout(ForgeStyle.HotForging, false, false))
        {
        }
    }
}
