using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;

namespace SWLOR.CombatSim.Tests.Support
{
    internal static class TestScenarios
    {
        /// <summary>Two teams of the given members; the first team is index 0.</summary>
        public static ScenarioSpec TwoTeams(bool isPvp, IEnumerable<CombatantSpec> first, IEnumerable<CombatantSpec> second, float durationSeconds = 600f) =>
            new()
            {
                Name = "test",
                IsPvp = isPvp,
                DurationSeconds = durationSeconds,
                Teams = new List<TeamSpec>
                {
                    new() { Name = "First", Members = first.ToList() },
                    new() { Name = "Second", Members = second.ToList() }
                }
            };

        /// <summary>Builds the scenario's simulation and spawns its combatants, ready for focused steps.</summary>
        public static Simulation Spawn(ScenarioSpec scenario, RuleSet rules, AbilityCatalog catalog, int seed = 1)
        {
            var simulation = new Simulation(scenario, rules, catalog, seed);
            simulation.Spawn();
            return simulation;
        }
    }
}
