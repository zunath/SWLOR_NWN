using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;

/// <summary>
/// The text the Fabricator window shows for each action, built from the engine's own previews so that what a button says is what it
/// does. Game text is ASCII punctuation only.
/// </summary>
public static class FabricatorActionText
{
    public const string FinishesItem = "Finishes the item";
    public const string BreaksPiece = "Breaks the piece.";

    // The meters an outcome lists, in this order.
    private static readonly string[] ListedMeters =
    {
        FabricatorEngine.FrameProgressId, FabricatorEngine.ShellProgressId, FabricatorEngine.FittingsProgressId,
        FabricatorEngine.FrameQualityId, FabricatorEngine.ShellQualityId, FabricatorEngine.FittingsQualityId,
        MinigameMeterIds.Quality, MinigameMeterIds.Integrity, MinigameMeterIds.Cp
    };

    /// <summary>The button label: the action's name and its CP cost.</summary>
    public static string ButtonText(MinigameActionPreview preview) =>
        preview.CpCost > 0 ? $"{preview.Action.Name} ({preview.CpCost} CP)" : preview.Action.Name;

    /// <summary>
    /// What the action does, or why it cannot be used. An action that finishes the item says so first and names the quality the
    /// item will have; one that would break the piece says that last.
    /// </summary>
    /// <param name="preview">The action's preview.</param>
    /// <param name="currentQualityPercent">The piece's quality now, which Assemble finishes the item at.</param>
    public static string ResultText(MinigameActionPreview preview, int currentQualityPercent)
    {
        if (!preview.Enabled)
            return preview.DisabledReason;

        var outcome = preview.Outcomes[0];
        if (outcome.FinishesItem)
        {
            var quality = outcome.Change(MinigameMeterIds.Quality)?.After ?? currentQualityPercent;
            return $"{FinishesItem} at {quality}% quality.";
        }

        var changes = DescribeChanges(outcome);
        return outcome.BreaksItem ? $"{changes}. {BreaksPiece}" : changes;
    }

    /// <summary>The hover text: what the action is, then its result.</summary>
    public static string Tooltip(MinigameActionPreview preview, int currentQualityPercent)
    {
        var result = ResultText(preview, currentQualityPercent);
        return string.IsNullOrEmpty(result) ? preview.Action.Description : $"{preview.Action.Description}\n{result}";
    }

    /// <summary>The change an outcome makes to the meters, such as "Frame progress +116, Machine Condition -3".</summary>
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

    /// <summary>The line the window shows for the announced or waiting opportunity, or a note that none is.</summary>
    public static string OpportunityText(MinigameOpportunity opportunity) =>
        opportunity == null
            ? "No Prime Feedstock is announced."
            : $"{opportunity.Name}. {opportunity.Description}";
}
