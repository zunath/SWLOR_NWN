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
        public static BatchSummary Run(ScenarioSpec scenario, RuleSet rules, AbilityCatalog catalog, int runs, int seed)
        {
            var summary = new BatchSummary { RuleSetName = rules.Name, Runs = runs };
            var perCombatant = new Dictionary<string, List<(Combatant Combatant, double Minutes)>>();

            for (var run = 0; run < runs; run++)
            {
                var result = new Simulation(scenario, rules, catalog, seed + run).Run();
                var minutes = Math.Max(result.EndedAt, 1) / 60.0;

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

                summary.AverageDeaths += result.Deaths.Count / (double)runs;
                summary.AverageRevives += result.Revives / (double)runs;

                foreach (var combatant in result.Combatants)
                {
                    summary.TotalDamageToHp += combatant.Stats.DamageTakenToHp / runs;
                    summary.TotalAbsorbed += combatant.Stats.DamageAbsorbedByTemporaryHp / runs;
                    summary.TotalHealing += combatant.Stats.HealingReceived / runs;
                    summary.TotalOverhealing += combatant.Stats.Overhealing / runs;
                    summary.AverageBlockedRevives += combatant.Stats.BlockedRevives / (double)runs;

                    if (!perCombatant.TryGetValue(combatant.Name, out var list))
                        perCombatant[combatant.Name] = list = new List<(Combatant, double)>();
                    list.Add((combatant, minutes));
                }
            }

            foreach (var (name, entries) in perCombatant)
            {
                var starved = entries.Where(e => e.Combatant.Stats.FirstStarvedAt.HasValue)
                    .Select(e => e.Combatant.Stats.FirstStarvedAt.Value)
                    .ToList();

                summary.Combatants.Add(new CombatantSummary
                {
                    Name = name,
                    TeamIndex = entries[0].Combatant.TeamIndex,
                    AverageDeaths = entries.Average(e => e.Combatant.Stats.Deaths),
                    AverageDamageDealt = entries.Average(e => e.Combatant.Stats.DamageDealt),
                    AverageHealingDone = entries.Average(e => e.Combatant.Stats.HealingDone),
                    AverageHealingReceived = entries.Average(e => e.Combatant.Stats.HealingReceived),
                    AverageOverhealing = entries.Average(e => e.Combatant.Stats.Overhealing),
                    AverageDamageTakenToHp = entries.Average(e => e.Combatant.Stats.DamageTakenToHp),
                    AverageAbsorbed = entries.Average(e => e.Combatant.Stats.DamageAbsorbedByTemporaryHp),
                    AverageFpSpentPerMinute = entries.Average(e => e.Combatant.Stats.FpSpent / e.Minutes),
                    AverageStmSpentPerMinute = entries.Average(e => e.Combatant.Stats.StmSpent / e.Minutes),
                    AverageFpRefunded = entries.Average(e => e.Combatant.Stats.FpRefunded),
                    AverageStmRefunded = entries.Average(e => e.Combatant.Stats.StmRefunded),
                    AverageStarvedSeconds = entries.Average(e => e.Combatant.Stats.StarvedSeconds),
                    MedianFirstStarvedAt = Percentile.Of(starved, 50),
                    ShareOfRunsStarved = starved.Count / (double)entries.Count
                });
            }

            return summary;
        }
    }
}
