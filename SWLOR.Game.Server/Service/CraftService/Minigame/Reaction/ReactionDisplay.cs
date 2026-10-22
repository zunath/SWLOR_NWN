using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

/// <summary>
/// The text the Reaction window shows, built from the engine's own previews so the numbers on a button are the ones the click
/// produces. Game text is ASCII punctuation only.
/// </summary>
public static class ReactionDisplay
{
    public const string FinishesTheItem = "Finishes the item";
    public const string BreaksThePiece = "May break the piece";

    public static string PhaseName(ReactionPhase phase) => phase.ToString();

    /// <summary>One line on what the phase does to the actions.</summary>
    public static string PhaseEffect(ReactionPhase phase) => phase switch
    {
        ReactionPhase.Calm => "Charge is safe.",
        ReactionPhase.Rising => "Charge costs heavy Stability.",
        ReactionPhase.Peak => "Charge is doubled and costs heavy Stability. Refine is halved.",
        ReactionPhase.Settling => "Refine is doubled. Charge costs heavy Stability.",
        _ => string.Empty
    };

    /// <summary>The whole cycle in one line, with the current position in brackets: <c>C C R [P] S S</c>.</summary>
    public static string CycleStrip(ReactionCycle cycle, int currentIndex)
    {
        var cells = cycle.Phases
            .Select((phase, index) => index == currentIndex ? $"[{phase.ToString()[0]}]" : phase.ToString()[0].ToString());
        return string.Join(" ", cells);
    }

    /// <summary>The window's description of the Peak: how violent it is and what a Charge there costs.</summary>
    public static string PeakText(ReactionSettings settings) =>
        $"Peak intensity {settings.PeakIntensity} of 5: a Charge at Peak costs " +
        $"{ReactionRules.ActionStability + settings.PeakChargeStability} Stability.";

    public static string MeterText(string label, MinigameMeter meter) =>
        meter.Max > 0 ? $"{label}: {meter.Value}/{meter.Max}" : $"{label}: {meter.Value}";

    /// <summary>
    /// The summary and tooltip of an action from its preview. <paramref name="phaseAfter"/> names the phase an outcome leaves the
    /// cycle in, from the cycle meter's position in that outcome.
    /// </summary>
    public static ReactionActionView DescribeAction(
        MinigameActionPreview preview,
        ReactionPhase currentPhase,
        Func<int, ReactionPhase> phaseAfter)
    {
        var tooltip = new StringBuilder();
        tooltip.Append(preview.Action.Name).Append(" in ").Append(PhaseName(currentPhase)).Append(": ")
            .Append(preview.Action.Description);

        if (!preview.Enabled)
        {
            tooltip.Append('\n').Append(preview.DisabledReason);
            return new ReactionActionView($"Unavailable: {preview.DisabledReason}", tooltip.ToString(), ReactionActionKind.Unavailable);
        }

        tooltip.Append("\nCost: ").Append(preview.IntegrityCost).Append(" Stability, ").Append(preview.CpCost).Append(" CP.");

        var single = preview.Outcomes.Count == 1;
        var lines = new List<string>();
        foreach (var outcome in preview.Outcomes)
        {
            var result = DescribeOutcome(outcome, phaseAfter);
            lines.Add(single ? result : $"{outcome.ChancePercent}% {outcome.Label}: {result}");
            tooltip.Append(single ? "\nResult: " : $"\n{outcome.ChancePercent}% {outcome.Label}: ").Append(result);
        }

        var kind = ReactionActionKind.Normal;
        var summary = string.Join(" / ", lines);
        if (preview.FinishesItem)
        {
            kind = ReactionActionKind.FinishesItem;
            summary = $"{FinishesTheItem}. {summary}";
            tooltip.Append('\n').Append(FinishesTheItem).Append(": the item is created at once.");
        }
        else if (preview.CanBreakItem)
        {
            kind = ReactionActionKind.BreaksItem;
            summary = $"{BreaksThePiece}. {summary}";
            tooltip.Append('\n').Append(BreaksThePiece).Append(": Stability would reach 0.");
        }

        return new ReactionActionView(summary, tooltip.ToString(), kind);
    }

    /// <summary>The net change an outcome makes to each shared meter, then the phase the cycle ends in.</summary>
    public static string DescribeOutcome(MinigameOutcomePreview outcome, Func<int, ReactionPhase> phaseAfter)
    {
        var parts = new List<string>();
        foreach (var id in new[] { MinigameMeterIds.Progress, MinigameMeterIds.Quality, MinigameMeterIds.Integrity, MinigameMeterIds.Cp })
        {
            var change = outcome.Change(id);
            if (change != null && change.Delta != 0)
                parts.Add($"{change.Label} {Signed(change.Delta)}");
        }

        if (parts.Count == 0)
            parts.Add("No change to the meters");

        var text = string.Join(", ", parts);
        var cycle = outcome.Change(ReactionEngine.CycleMeterId);
        if (cycle != null && outcome.ResultingStatus == MinigameStatus.InProgress)
            text += $". Next: {PhaseName(phaseAfter(cycle.After))}";

        return text;
    }

    private static string Signed(int value) =>
        value > 0 ? "+" + value.ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
}
