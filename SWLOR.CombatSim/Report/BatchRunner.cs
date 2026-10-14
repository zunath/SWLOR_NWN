using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;

namespace SWLOR.CombatSim.Report
{
    /// <summary>
    /// Runs a scenario many times with different seeds and aggregates the results.
    /// </summary>
    public static class BatchRunner
    {
        /// <summary>Every run's results are kept for the report, so the count stays practical.</summary>
        public const int MaximumRuns = 100_000;

        /// <summary>Runs times combatants per run; each keeps at most one first-starved time for the median.</summary>
        public const long MaximumCombatantRuns = 10_000_000;

        /// <summary>Runs times steps per run times combatants per run, which keeps a batch to minutes.</summary>
        public const long MaximumCombatantSteps = 500_000_000;

        /// <summary>Simulated events across the batch: hit rolls, heal and shield applications and recurring ticks.</summary>
        public const long MaximumEvents = 500_000_000;

        public static BatchSummary Run(ScenarioSpec scenario, RuleSet rules, AbilityCatalog catalog, int runs, int seed)
        {
            var combatantsPerRun = scenario.Teams.Sum(team => team?.Members?.Sum(member => (long)Math.Max(1, member?.Count ?? 1)) ?? 0);
            if (runs * combatantsPerRun > MaximumCombatantRuns)
                throw new ArgumentException(
                    $"{runs} runs of {combatantsPerRun} combatants is more than {MaximumCombatantRuns} combatant-runs; use fewer runs.");

            // A duration or time step out of range is reported by the simulation itself.
            var stepsPerRun = Math.Ceiling(scenario.DurationSeconds / (double)scenario.TimeStepSeconds);
            if (double.IsFinite(stepsPerRun) && stepsPerRun > 0 && runs * stepsPerRun * combatantsPerRun > MaximumCombatantSteps)
                throw new ArgumentException(
                    $"{runs} runs of {stepsPerRun} steps with {combatantsPerRun} combatants is more than {MaximumCombatantSteps} " +
                    "combatant-steps; use fewer runs, a shorter duration or a longer time step.");

            var eventsPerRun = BatchWork.EventsPerRun(scenario, rules, catalog);
            if (runs * eventsPerRun > MaximumEvents)
                throw new ArgumentException(
                    $"{runs} runs of up to {eventsPerRun:0} simulated events each (hit rolls, heal and shield applications, " +
                    $"recurring ticks) is more than {MaximumEvents}; use fewer runs, a shorter duration, or fewer combatants, " +
                    "uses, hits or targets.");

            var summary = new BatchSummary { RuleSetName = rules.Name, Runs = runs };
            var perCombatant = new Dictionary<(int TeamIndex, string Name), CombatantAccumulator>();

            for (var run = 0; run < runs; run++)
            {
                var result = new Simulation(scenario, rules, catalog, seed + run).Run();

                summary.FightEndTimes.Add(result.EndedAt);
                if (result.FirstDeathAt.HasValue)
                {
                    summary.RunsWithDeath++;
                    summary.FirstDeathTimes.Add(result.FirstDeathAt.Value);
                }

                if (result.WinningTeam is >= 0)
                {
                    summary.DecisiveRuns++;
                    summary.WinsByTeam[result.WinningTeam.Value] = summary.WinsByTeam.GetValueOrDefault(result.WinningTeam.Value) + 1;
                }

                summary.AverageDeaths += result.PlayerDeaths.Count() / (double)runs;
                summary.AverageRevives += result.Revives / (double)runs;

                foreach (var combatant in result.Combatants)
                {
                    summary.TotalDamageToHp += combatant.Stats.DamageTakenToHp / runs;
                    summary.TotalHitPointsSpent += combatant.Stats.HitPointsSpent / runs;
                    summary.TotalAbsorbed += combatant.Stats.DamageAbsorbedByTemporaryHp / runs;
                    summary.TotalHealing += combatant.Stats.HealingReceived / runs;
                    summary.TotalOverhealing += combatant.Stats.Overhealing / runs;
                    summary.AverageBlockedRevives += combatant.Stats.BlockedRevives / (double)runs;

                    var key = (combatant.TeamIndex, combatant.Name);
                    if (!perCombatant.TryGetValue(key, out var accumulator))
                        perCombatant[key] = accumulator = new CombatantAccumulator(combatant.Name, combatant.TeamIndex);
                    // Resource rates count only the time a combatant was in the fight.
                    accumulator.Add(combatant.Stats, Math.Max(result.EndedAt - combatant.Spec.JoinAtSeconds, 0) / 60.0);
                }
            }

            summary.Combatants.AddRange(perCombatant.Values.Select(accumulator => accumulator.ToSummary()));
            return summary;
        }
    }
}
