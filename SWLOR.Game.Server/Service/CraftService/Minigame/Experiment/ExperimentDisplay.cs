using System.Collections.Generic;
using System.Globalization;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;

/// <summary>
/// The text the Experiment window shows, built from the engine's own previews so the numbers on a button are the ones the click
/// produces. Game text is ASCII punctuation only.
/// </summary>
public static class ExperimentDisplay
{
    public const string EndsTheExperiment = "Ends the experiment";

    private const string NoChange = "no change to your evidence";

    /// <summary>
    /// The line beside an action's button and its tooltip, from its preview. The line names the chance and the outcome that changes
    /// something (the miss is the rest of the chance); the tooltip is the action's own description on one line, because a NUI
    /// tooltip neither wraps nor breaks lines.
    /// </summary>
    public static ExperimentActionView DescribeAction(MinigameActionPreview preview)
    {
        var tooltip = preview.Action.Description;

        if (!preview.Enabled)
        {
            // The greyed button already says it is unavailable, so the line beside it gives only the reason.
            return new ExperimentActionView(preview.DisabledReason, tooltip, ExperimentActionKind.Unavailable);
        }

        var single = preview.Outcomes.Count == 1;
        var lines = new List<string>();
        foreach (var outcome in preview.Outcomes)
        {
            var result = preview.Action.Id == ExperimentActionIds.Breakthrough
                ? "your next Test succeeds"
                : DescribeOutcome(outcome);
            if (single)
                lines.Add(result);
            else if (result != NoChange)
                lines.Add($"{outcome.ChancePercent}% chance: {result}");
        }

        if (lines.Count == 0)
            lines.Add(NoChange);

        var summary = string.Join(" / ", lines);
        var kind = ExperimentActionKind.Normal;
        if (preview.FinishesItem)
        {
            kind = ExperimentActionKind.EndsExperiment;
            summary = summary == NoChange
                ? $"{EndsTheExperiment} with the evidence so far."
                : $"{EndsTheExperiment}: {summary}.";
            tooltip = $"{tooltip} {EndsTheExperiment}: the result is stored and the job runs.";
        }

        return new ExperimentActionView(summary, tooltip, kind);
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
            parts.Add(NoChange);

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
        if (tuning.TimeCutTenthsPerEvidence != ExperimentRules.TimeCutTenthsPerEvidence)
            parts.Add($"Analyze cuts {Tenths(tuning.TimeCutTenthsPerEvidence)}% per evidence");
        if (tuning.PeerReview)
            parts.Add("Peer Review");
        if (tuning.Breakthrough)
            parts.Add("Breakthrough");

        return parts.Count == 0 ? "Researcher perks: none." : $"Researcher perks: {string.Join(", ", parts)}.";
    }

    public static string MeterText(string label, MinigameMeter meter) =>
        meter.Max > 0 ? $"{label}: {meter.Value}/{meter.Max}" : $"{label}: {meter.Value}";

    /// <summary>Tenths of a percent as a percent, with one decimal only when it is not whole: 50 is "5", 57 is "5.7".</summary>
    public static string Tenths(int tenths) =>
        tenths % 10 == 0
            ? (tenths / 10).ToString(CultureInfo.InvariantCulture)
            : (tenths / 10.0).ToString("0.0", CultureInfo.InvariantCulture);

    public static string Signed(int value) =>
        value > 0 ? "+" + value.ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
}
