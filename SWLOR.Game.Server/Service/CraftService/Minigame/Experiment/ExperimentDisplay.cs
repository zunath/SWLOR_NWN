using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;

/// <summary>
/// The text the Experiment window shows, built from the engine's own previews so the numbers on a button are the ones the click
/// produces. Game text is ASCII punctuation only.
/// </summary>
public static class ExperimentDisplay
{
    public const string EndsTheExperiment = "Ends the experiment";

    /// <summary>The summary and tooltip of an action from its preview.</summary>
    public static ExperimentActionView DescribeAction(MinigameActionPreview preview)
    {
        var tooltip = new StringBuilder();
        tooltip.Append(preview.Action.Name).Append(": ").Append(preview.Action.Description);

        if (!preview.Enabled)
        {
            tooltip.Append('\n').Append(preview.DisabledReason);
            return new ExperimentActionView($"Unavailable: {preview.DisabledReason}", tooltip.ToString(), ExperimentActionKind.Unavailable);
        }

        var cost = Cost(preview);
        tooltip.Append("\nCost: ").Append(cost).Append('.');

        var single = preview.Outcomes.Count == 1;
        var lines = new List<string>();
        foreach (var outcome in preview.Outcomes)
        {
            var result = preview.Action.Id == ExperimentActionIds.Breakthrough
                ? "your next Test succeeds, and Analyze cuts more time per piece of evidence"
                : DescribeOutcome(outcome);
            lines.Add(single ? result : $"{outcome.ChancePercent}% {outcome.Label}: {result}");
            tooltip.Append(single ? "\nResult: " : $"\n{outcome.ChancePercent}% {outcome.Label}: ").Append(result).Append('.');
        }

        var summary = string.Join(" / ", lines);
        var kind = ExperimentActionKind.Normal;
        if (preview.FinishesItem)
        {
            kind = ExperimentActionKind.EndsExperiment;
            summary = $"{EndsTheExperiment}. {summary}";
            tooltip.Append('\n').Append(EndsTheExperiment).Append(": the result is stored and the job runs.");
        }

        return new ExperimentActionView(summary, tooltip.ToString(), kind);
    }

    /// <summary>What an action costs: a trial, CP, or nothing.</summary>
    public static string Cost(MinigameActionPreview preview)
    {
        var uses = preview.Outcomes.Count > 0 && preview.Outcomes[0].Change(MinigameMeterIds.Progress)?.Delta > 0;
        var parts = new List<string>();
        if (uses)
            parts.Add("1 trial");
        if (preview.CpCost > 0)
            parts.Add($"{preview.CpCost} CP");

        return parts.Count == 0 ? "nothing" : string.Join(" and ", parts);
    }

    /// <summary>The change a result makes to the evidence, the time cut and the waiting method bonus.</summary>
    public static string DescribeOutcome(MinigameOutcomePreview outcome)
    {
        var parts = new List<string>();
        foreach (var change in outcome.Changes)
        {
            if (change.Delta == 0)
                continue;

            if (change.Id == ExperimentActionIds.TimeCutMeter)
            {
                parts.Add($"job time cut {Signed(change.Delta)}% (now {change.After}%)");
            }
            else if (change.Id == ExperimentActionIds.MethodMeter)
            {
                parts.Add(change.Delta > 0 ? $"next Test chance +{change.Delta}%" : "method used up");
            }
            else if (change.Id.StartsWith("evidence_", StringComparison.Ordinal))
            {
                parts.Add($"{change.Label} {Signed(change.Delta)}");
            }
        }

        if (parts.Count == 0)
            parts.Add("no change to your evidence");

        return string.Join(", ", parts);
    }

    /// <summary>What a candidate would pay with the evidence on it now.</summary>
    public static string Projection(ResearchCandidate candidate, int evidence)
    {
        if (candidate == ResearchCandidate.FixedGrant)
            return "Fixed grant. Evidence only buys a shorter job.";

        var amount = ResearchOutcomeRules.Magnitude(candidate, evidence);
        if (amount <= 0)
            return "No evidence yet.";

        return $"Pays +{amount}% (most {ResearchGrantRules.ReductionMaximum}%).";
    }

    /// <summary>The line the window shows for the researcher's perks: each number they change, or a note that they have none.</summary>
    public static string PerkSummary(ExperimentTuning tuning)
    {
        var parts = new List<string>();
        if (tuning.Trials != ExperimentRules.Trials)
            parts.Add($"{tuning.Trials} trials");
        if (tuning.TestChanceBonusPercent != 0)
            parts.Add($"Test chance {Signed(tuning.TestChanceBonusPercent)} points");
        if (tuning.RefineCp != ExperimentRules.RefineCp)
            parts.Add($"Refine Method costs {tuning.RefineCp} CP");
        if (tuning.RefineChanceBonus != ExperimentRules.RefineChanceBonusPercent)
            parts.Add($"Refine Method adds {tuning.RefineChanceBonus} points");
        if (tuning.TimeCutPerEvidence != ExperimentRules.TimeCutPercentPerEvidence)
            parts.Add($"Analyze cuts {tuning.TimeCutPerEvidence}% per evidence");
        if (tuning.PeerReview)
            parts.Add("Peer Review");
        if (tuning.Breakthrough)
            parts.Add("Breakthrough");

        return parts.Count == 0 ? "Researcher perks: none." : $"Researcher perks: {string.Join(", ", parts)}.";
    }

    public static string MeterText(string label, MinigameMeter meter) =>
        meter.Max > 0 ? $"{label}: {meter.Value}/{meter.Max}" : $"{label}: {meter.Value}";

    public static string Signed(int value) =>
        value > 0 ? "+" + value.ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
}
