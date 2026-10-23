using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

namespace SWLOR.CraftSim.Scenario.Circuit
{
    /// <summary>The Circuit played by a crafter who has bought the Technician line in full and plays Safe Mode, for the gates a perked crafter must still pass.</summary>
    public sealed class CircuitTechnicianScenario : CircuitScenario
    {
        public CircuitTechnicianScenario()
            : base("circuit-technician", "Circuit (Technician line, Safe Mode)", new CircuitLoadout(CircuitStyle.SafeMode, false, true))
        {
        }
    }
}
