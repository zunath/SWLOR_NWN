using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

namespace SWLOR.CraftSim.Scenario.Circuit
{
    /// <summary>The Circuit played by a crafter who has bought both lines in full and plays Overclock: the strongest crafter the perks allow, for the gates a perked crafter must still pass.</summary>
    public sealed class CircuitBothLinesScenario : CircuitScenario
    {
        public CircuitBothLinesScenario()
            : base("circuit-both", "Circuit (both lines, Overclock)", new CircuitLoadout(CircuitStyle.Overclock, true, true))
        {
        }
    }
}
