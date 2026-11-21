using System.Globalization;
using System.Text;
using SWLOR.CombatSim.SpaceCombat.Scenario;

namespace SWLOR.CombatSim.SpaceCombat.Report;

/// <summary>Writes the summary of a space scenario in the same markdown style as the ground report.</summary>
public static class SpaceMarkdownReport
{
    public static string Build(SpaceScenarioSpec scenario, SpaceBatchSummary summary)
    {
        var text = new StringBuilder();
        text.AppendLine($"# {scenario.Name}");
        if (!string.IsNullOrWhiteSpace(scenario.Description))
            text.AppendLine().AppendLine(scenario.Description);

        text.AppendLine()
            .AppendLine($"Time limit {Number(scenario.DurationSeconds, 0)}s in {Number(scenario.TimeStepSeconds, 2)}s steps, " +
                        $"{summary.Runs} runs, seed {summary.Seed}. " +
                        $"{scenario.Attackers.Name ?? "Attackers"}{(scenario.Attackers.Passive ? " (passive)" : string.Empty)} attack " +
                        $"{scenario.Defenders.Name ?? "Defenders"}{(scenario.Defenders.Passive ? " (passive)" : string.Empty)}.")
            .AppendLine()
            .AppendLine("| Defenders destroyed | Time to kill: median (p10-p90) | Attacker win rate | Attacker hull left: median | Attacker shield left: median | Attacker damage per second: median |")
            .AppendLine("| --- | --- | --- | --- | --- | --- |");

        var median = summary.MedianTimeToKill;
        var timeToKill = median.HasValue
            ? $"{Seconds(median.Value)} ({Seconds(summary.P10TimeToKill.Value)}-{Seconds(summary.P90TimeToKill.Value)})"
            : "never";

        text.AppendLine(string.Join(" | ",
            $"| {Percent(summary.RunsWithKill, summary.Runs)}",
            timeToKill,
            Percent(summary.AttackerWins, summary.Runs),
            Remaining(summary.MedianAttackerHullRemaining, summary.AttackerMaxHull),
            Remaining(summary.MedianAttackerShieldRemaining, summary.AttackerMaxShield),
            Number(summary.MedianDamagePerSecond ?? 0, 1) + " |"));

        text.AppendLine()
            .AppendLine("Time to kill counts only the runs that destroyed every defender, measured to the shot that did it.");
        return text.ToString();
    }

    private static string Remaining(double? value, double maximum) =>
        !value.HasValue ? "n/a" : maximum > 0 ? $"{Number(value.Value, 0)} of {Number(maximum, 0)}" : Number(value.Value, 0);

    private static string Seconds(double seconds) => Number(seconds, 1) + "s";

    private static string Percent(int count, int total) => total == 0 ? "0%" : $"{count * 100.0 / total:0}%";

    private static string Number(double value, int decimals) => value.ToString("N" + decimals, CultureInfo.InvariantCulture);
}
