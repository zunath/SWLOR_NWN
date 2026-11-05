using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

namespace SWLOR.CraftSim.Scenario.Forge
{
    /// <summary>The Plasma Forge played by a crafter who has bought both lines in full and plays Hot Forging: the strongest crafter the perks allow, for the gates a perked crafter must still pass.</summary>
    public sealed class ForgeBothLinesScenario : ForgeScenario
    {
        public ForgeBothLinesScenario()
            : base("forge-both", "Plasma Forge (both lines, Hot Forging)", new ForgeLoadout(ForgeStyle.HotForging, true, true))
        {
        }
    }
}
