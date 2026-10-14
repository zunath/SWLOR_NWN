using NUnit.Framework;
using SWLOR.CombatSim.Engine;

namespace SWLOR.CombatSim.Tests.Support
{
    /// <summary>
    /// <see cref="Simulation.Run"/> owns the clock; <see cref="Simulation.Step"/> never advances it.
    /// Focused tests drive the same loop themselves, so they can act at exact simulated times.
    /// </summary>
    internal static class SimulationClock
    {
        private const double DefaultStepSeconds = 0.1;

        public static void Set(Simulation simulation, double seconds) => simulation.Now = seconds;

        /// <summary>Steps the simulation the way Run does until the clock reaches <paramref name="seconds"/>.</summary>
        public static void RunUntil(Simulation simulation, double seconds, double stepSeconds = DefaultStepSeconds)
        {
            while (simulation.Now < seconds - 1e-9)
                AdvanceOneStep(simulation, stepSeconds);
        }

        /// <summary>Steps until <paramref name="condition"/> holds; fails the test when it never does within the limit.</summary>
        public static void RunUntil(Simulation simulation, Func<bool> condition, double limitSeconds, string description, double stepSeconds = DefaultStepSeconds)
        {
            while (!condition())
            {
                if (simulation.Now >= limitSeconds)
                    throw new AssertionException($"Condition '{description}' was not met by {limitSeconds:0.0}s of simulated time.");

                AdvanceOneStep(simulation, stepSeconds);
            }
        }

        private static void AdvanceOneStep(Simulation simulation, double stepSeconds)
        {
            simulation.Step(stepSeconds);
            Set(simulation, simulation.Now + stepSeconds);
        }
    }
}
