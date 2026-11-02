using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

namespace SWLOR.CraftSim.Scenario.Forge
{
    /// <summary>The Plasma Forge played by a crafter who has bought the Forgemaster line in full and plays Hot Forging, for the gates a perked crafter must still pass.</summary>
    public sealed class ForgeForgemasterScenario : ForgeScenario
    {
        public ForgeForgemasterScenario()
            : base("forge-forgemaster", "Plasma Forge (Forgemaster line, Hot Forging)", new ForgeLoadout(ForgeStyle.HotForging, true, false))
        {
        }
    }
}
