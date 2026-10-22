using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

/// <summary>
/// The text the Circuit window shows for each action, built from the engine's own previews so that what a button says is what it
/// does. Game text is ASCII punctuation only.
/// </summary>
public static class CircuitActionText
{
    public const string FinishesItem = "Finishes the item.";
    public const string BreaksPiece = "Breaks the piece.";
    public const string NoRisk = "No overload risk.";

    // The meters an outcome lists, in this order.
    private static readonly string[] ListedMeters =
    {
        MinigameMeterIds.Progress, MinigameMeterIds.Quality, CircuitEngine.LoadId, MinigameMeterIds.Integrity, MinigameMeterIds.Cp
    };

    /// <summary>The button label: the action's name and its CP cost.</summary>
    public static string ButtonText(MinigameActionPreview preview) =>
        preview.CpCost > 0 ? $"{preview.Action.Name} ({preview.CpCost} CP)" : preview.Action.Name;

    /// <summary>What the action does when it holds, or why it cannot be used. Starts with the finish warning when it ends the craft.</summary>
    public static string ResultText(MinigameActionPreview preview)
    {
        if (!preview.Enabled)
            return preview.DisabledReason;

        var normal = preview.Outcomes[0];
        var changes = DescribeChanges(normal);
        return normal.FinishesItem ? $"{FinishesItem} {changes}" : changes;
    }

    /// <summary>
    /// The overload chance and what an overload costs, or a note that the action cannot overload. Warns when an overload would
    /// finish or break the piece.
    /// </summary>
    public static string RiskText(MinigameActionPreview preview)
    {
        if (!preview.Enabled)
            return string.Empty;

        if (preview.Outcomes.Count < 2)
            return NoRisk;

        var normal = preview.Outcomes[0];
        var overload = preview.Outcomes[1];
        var differences = ListedMeters
            .Select(id => (Before: normal.Change(id), After: overload.Change(id)))
            .Where(pair => pair.After != null && pair.Before != null && pair.After.After != pair.Before.After)
            .Select(pair => pair.After.Id == CircuitEngine.LoadId
                ? $"{pair.After.Label} to {pair.After.After}"
                : $"{pair.After.Label} {Signed(pair.After.Delta)}")
            .ToList();

        var text = $"{overload.Label} {overload.ChancePercent}%: {string.Join(", ", differences)}.";
        if (overload.BreaksItem && !normal.BreaksItem)
            text += $" {BreaksPiece}";
        else if (overload.FinishesItem)
            text += $" {FinishesItem}";

        return text;
    }

    /// <summary>The text beside an action's button: its result on the first line and its overload risk on the second.</summary>
    public static string RowText(MinigameActionPreview preview)
    {
        var risk = RiskText(preview);
        return string.IsNullOrEmpty(risk) ? ResultText(preview) : $"{ResultText(preview)}\n{risk}";
    }

    /// <summary>The hover text: what the action is, then its result and its risk.</summary>
    public static string Tooltip(MinigameActionPreview preview)
    {
        var parts = new List<string> { preview.Action.Description };
        var result = ResultText(preview);
        if (!string.IsNullOrEmpty(result))
            parts.Add(result);

        var risk = RiskText(preview);
        if (!string.IsNullOrEmpty(risk))
            parts.Add(risk);

        return string.Join("\n", parts);
    }

    /// <summary>The change an outcome makes to the meters, such as "Calibration +81, Load +1, Integrity -3, CP -3".</summary>
    public static string DescribeChanges(MinigameOutcomePreview outcome)
    {
        var parts = ListedMeters
            .Select(outcome.Change)
            .Where(change => change != null && change.Delta != 0)
            .Select(change => $"{change.Label} {Signed(change.Delta)}")
            .ToList();

        return parts.Count == 0 ? "No change." : string.Join(", ", parts);
    }

    public static string Signed(int value) => value > 0 ? $"+{value}" : value.ToString();
}
