using SWLOR.CombatSim.Report;
using SWLOR.CombatSim.SpaceCombat.Engine;
using SWLOR.CombatSim.SpaceCombat.Scenario;

namespace SWLOR.CombatSim.SpaceCombat.Report;

/// <summary>Runs a space scenario many times with seeds seed, seed + 1... and aggregates the results.</summary>
public static class SpaceBatchRunner
{
    public const int DefaultRuns = 300;

    public static SpaceBatchSummary Run(SpaceScenarioSpec scenario, int runs, int seed)
    {
        var ships = scenario.Attackers.Ships.Sum(s => (long)s.Count) + scenario.Defenders.Ships.Sum(s => (long)s.Count);
        var stepsPerRun = Math.Ceiling(scenario.DurationSeconds / scenario.TimeStepSeconds);
        if (runs * stepsPerRun * ships > BatchRunner.MaximumCombatantSteps)
            throw new ArgumentException(
                $"{runs} runs of {stepsPerRun} steps with {ships} ships is more than {BatchRunner.MaximumCombatantSteps} " +
                "ship-steps; use fewer runs, a shorter duration or a longer time step.");

        var summary = new SpaceBatchSummary { Runs = runs, Seed = seed };
        for (var run = 0; run < runs; run++)
        {
            var result = new SpaceFight(scenario, seed + run).Run();
            if (result.DefendersDestroyedAt.HasValue)
                summary.TimesToKill.Add(result.DefendersDestroyedAt.Value);
            if (result.AttackersWon)
                summary.AttackerWins++;

            summary.AttackerHullRemaining.Add(result.AttackerHullRemaining);
            summary.AttackerShieldRemaining.Add(result.AttackerShieldRemaining);
            summary.AttackerMaxHull = result.AttackerMaxHull;
            summary.AttackerMaxShield = result.AttackerMaxShield;

            // A fight over in the first step still counts one step of shooting.
            summary.DamagePerSecond.Add(result.DamageDealtByAttackers / Math.Max(result.EndedAt, scenario.TimeStepSeconds));
        }

        return summary;
    }
}
