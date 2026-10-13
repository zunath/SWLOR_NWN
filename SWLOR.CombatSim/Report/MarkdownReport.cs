using System.Globalization;
using System.Text;
using SWLOR.CombatSim.Scenario;

namespace SWLOR.CombatSim.Report
{
    /// <summary>
    /// Writes a side-by-side comparison of rule sets for one scenario.
    /// </summary>
    public static class MarkdownReport
    {
        public static string Build(ScenarioSpec scenario, IReadOnlyList<BatchSummary> summaries, bool includeCombatants)
        {
            var text = new StringBuilder();
            text.AppendLine($"# {scenario.Name}");
            if (!string.IsNullOrWhiteSpace(scenario.Description))
                text.AppendLine().AppendLine(scenario.Description);

            text.AppendLine()
                .AppendLine($"Time limit {Seconds(scenario.DurationSeconds)}, {(scenario.IsPvp ? "PvP" : "PvE")}, {summaries.FirstOrDefault()?.Runs ?? 0} runs per rule set.")
                .AppendLine()
                .AppendLine("| Rules | Runs with a player death | First player death: median (p10-p90) | Fight decided | Wins by team | Fight length: median | Player deaths per run | Revives per run | (Healing + absorbed) / HP damage |")
                .AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");

            foreach (var s in summaries)
            {
                var median = Percentile.Of(s.FirstDeathTimes, 50);
                var p10 = Percentile.Of(s.FirstDeathTimes, 10);
                var p90 = Percentile.Of(s.FirstDeathTimes, 90);
                var firstDeath = median.HasValue ? $"{Seconds(median.Value)} ({Seconds(p10.Value)}-{Seconds(p90.Value)})" : "none";

                text.AppendLine(string.Join(" | ",
                    $"| {s.RuleSetName}",
                    Percent(s.RunsWithDeath, s.Runs),
                    firstDeath,
                    Percent(s.DecisiveRuns, s.Runs),
                    WinsByTeam(scenario, s),
                    Seconds(Percentile.Of(s.FightEndTimes, 50) ?? 0),
                    Number(s.AverageDeaths, 1),
                    Number(s.AverageRevives, 1),
                    Number(s.RecoveryRatio, 2) + " |"));
            }

            if (!includeCombatants)
                return text.ToString();

            foreach (var s in summaries)
            {
                text.AppendLine()
                    .AppendLine($"## {s.RuleSetName}: per combatant (averages per run)")
                    .AppendLine()
                    .AppendLine("| Combatant | Team | Deaths | Damage dealt | Healing done | Healing received | Overheal | HP damage taken | Absorbed by temp HP | FP/min | STM/min | FP refunded | STM refunded | Couldn't afford a wanted ability | First time: median |")
                    .AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");

                foreach (var c in s.Combatants.OrderBy(c => c.TeamIndex).ThenBy(c => c.Name))
                {
                    text.AppendLine(string.Join(" | ",
                        $"| {c.Name}",
                        scenario.Teams[c.TeamIndex].Name,
                        Number(c.AverageDeaths, 2),
                        Number(c.AverageDamageDealt, 0),
                        Number(c.AverageHealingDone, 0),
                        Number(c.AverageHealingReceived, 0),
                        Number(c.AverageOverhealing, 0),
                        Number(c.AverageDamageTakenToHp, 0),
                        Number(c.AverageAbsorbed, 0),
                        Number(c.AverageFpSpentPerMinute, 0),
                        Number(c.AverageStmSpentPerMinute, 0),
                        Number(c.AverageFpRefunded, 0),
                        Number(c.AverageStmRefunded, 0),
                        $"{c.ShareOfRunsStarved:P0}",
                        (c.MedianFirstStarvedAt.HasValue ? Seconds(c.MedianFirstStarvedAt.Value) : "never") + " |"));
                }
            }

            return text.ToString();
        }

        private static string WinsByTeam(ScenarioSpec scenario, BatchSummary summary) =>
            string.Join(", ", scenario.Teams.Select((team, index) =>
                $"{team.Name} {Percent(summary.WinsByTeam.GetValueOrDefault(index), summary.Runs)}"));

        private static string Seconds(double seconds)
        {
            var span = TimeSpan.FromSeconds(Math.Round(seconds));
            return span.TotalMinutes >= 1 ? $"{(int)span.TotalMinutes}:{span.Seconds:00}" : $"{span.Seconds}s";
        }

        private static string Percent(int count, int total) => total == 0 ? "0%" : $"{count * 100.0 / total:0}%";

        private static string Number(double value, int decimals) =>
            value.ToString("N" + decimals, CultureInfo.InvariantCulture);
    }
}
