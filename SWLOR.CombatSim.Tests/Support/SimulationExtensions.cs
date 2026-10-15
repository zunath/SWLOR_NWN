using SWLOR.CombatSim.Engine;

namespace SWLOR.CombatSim.Tests.Support
{
    internal static class SimulationExtensions
    {
        public static Combatant Get(this Simulation simulation, string name) =>
            simulation.Combatants.Single(c => c.Name == name);
    }
}
