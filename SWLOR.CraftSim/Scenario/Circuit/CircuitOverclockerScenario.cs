using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

namespace SWLOR.CraftSim.Scenario.Circuit
{
    /// <summary>The Circuit played by a crafter who has bought the Overclocker line in full and plays Overclock, for the gates a perked crafter must still pass.</summary>
    public sealed class CircuitOverclockerScenario : CircuitScenario
    {
        public CircuitOverclockerScenario()
            : base("circuit-overclocker", "Circuit (Overclocker line, Overclock)", new CircuitLoadout(CircuitStyle.Overclock, true, false))
        {
        }
    }
}
