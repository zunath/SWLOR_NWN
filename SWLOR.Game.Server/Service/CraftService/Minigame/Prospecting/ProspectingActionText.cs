using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>What an action's row in the window is: how it is coloured and what it warns about.</summary>
public enum ProspectingActionKind
{
    Normal = 0,

    /// <summary>The action may end the session by spending the node or the harvester.</summary>
    Ends = 1,

    /// <summary>The action may collapse the node.</summary>
    Collapses = 2,

    Unavailable = 3
}

/// <summary>The row for one action: the result it promises, a tooltip and how it is coloured.</summary>
public sealed record ProspectingActionView(string Summary, string Tooltip, ProspectingActionKind Kind);

/// <summary>
/// The words the Prospecting window shows for the engine's previews. Every number comes from the preview or the setup, so what a button
/// promises is what a click does. Nothing here names how many seams are hidden.
/// </summary>
public static class ProspectingActionText
{
    public const string MinesOut = "MINES OUT THE NODE";
    public const string Collapses = "COLLAPSES THE NODE";
    public const string UsesUp = "USES UP THE HARVESTER";

    /// <summary>The row for an action: its result, a tooltip and its kind.</summary>
    public static ProspectingActionView Describe(ProspectingEngine engine, MinigameActionPreview preview)
    {
        if (!preview.Enabled)
            return new ProspectingActionView(preview.DisabledReason, preview.Action.Description, ProspectingActionKind.Unavailable);

        var kind = ProspectingActionKind.Normal;
        if (preview.Action.Id != ProspectingActionIds.Leave)
        {
            if (preview.Outcomes.Any(outcome => outcome.BreaksItem))
                kind = ProspectingActionKind.Collapses;
            else if (preview.Outcomes.Any(outcome => outcome.FinishesItem))
                kind = ProspectingActionKind.Ends;
        }

        return new ProspectingActionView(SummaryText(engine, preview), TooltipText(preview), kind);
    }

    /// <summary>What the action does and what it costs, such as "Takes 1 unit (50%) or 2 units (50%). Stability 100 -> 80, Charges 12 -> 11".</summary>
    public static string SummaryText(ProspectingEngine engine, MinigameActionPreview preview)
    {
        var what = WhatText(engine, preview);
        var cost = CostText(preview);
        var time = TimeText(engine, preview);
        var ending = EndingText(preview);
        return string.Join(". ", new[] { what, cost, time, ending }.Where(part => !string.IsNullOrEmpty(part)));
    }

    /// <summary>The seconds an action takes, said only when a perk has changed them from what they usually are.</summary>
    private static string TimeText(ProspectingEngine engine, MinigameActionPreview preview)
    {
        var seconds = engine.SecondsFor(preview.Action.Id);
        var usual = ProspectingRules.SecondsFor(preview.Action.Id);
        return usual <= 0 || Math.Abs(seconds - usual) < 0.005
            ? string.Empty
            : $"Takes {seconds.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} seconds instead of {usual.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)}";
    }

    /// <summary>The same text for the action that was performed: what it did and what it cost.</summary>
    public static string OutcomeText(MinigameOutcomePreview outcome)
    {
        var parts = new List<string> { outcome.Label };
        var cost = ChangeText(outcome);
        if (!string.IsNullOrEmpty(cost))
            parts.Add(cost);

        var ending = EndingText(outcome);
        if (ending != null)
            parts.Add(ending);

        return string.Join(". ", parts);
    }

    /// <summary>The ending an outcome causes, or null when the session goes on.</summary>
    public static string EndingText(MinigameOutcomePreview outcome)
    {
        if (outcome.ResultingStatus == MinigameStatus.InProgress)
            return null;

        if (outcome.BreaksItem)
            return Collapses;

        var yield = outcome.Change(MinigameMeterIds.Progress);
        if (yield != null && yield.After <= 0)
            return MinesOut;

        var charges = outcome.Change(MinigameMeterIds.Cp);
        return charges != null && charges.After <= 0 ? UsesUp : null;
    }

    /// <summary>The meter named with its value out of its maximum and what the end of the scale means.</summary>
    public static string MeterText(string name, int value, int max, string note) => $"{name}: {value} of {max} ({note})";

    /// <summary>A seam as the list in the window shows it.</summary>
    public static string SeamText(ProspectingSeam seam) => seam.Kind == ProspectingSeamKind.GemPocket
        ? $"Gem pocket: Extract for {ProspectingRules.GemPocketGems} gem in place of its unit"
        : $"Rich seam: Extract for its unit and a {ProspectingRules.RichSeamBonusPercent}% chance of an extra ore roll";

    private static string WhatText(ProspectingEngine engine, MinigameActionPreview preview)
    {
        switch (preview.Action.Id)
        {
            case ProspectingActionIds.Survey:
                var reveals = engine.Setup.SurveyReveals;
                return reveals == 1 ? "Reveals 1 hidden seam if there is one" : $"Reveals up to {reveals} hidden seams";

            case ProspectingActionIds.Strike:
                return YieldText(preview, shatter: false);

            case ProspectingActionIds.HeavyStrike:
                return YieldText(preview, shatter: true);

            case ProspectingActionIds.Extract:
                return preview.Outcomes[0].Label;

            default:
                return "Leaves the rest of the node for others";
        }
    }

    /// <summary>"Takes 1 unit (50%) or 2 units (50%)", with the chance of shattering a pocket for a Heavy Strike.</summary>
    private static string YieldText(MinigameActionPreview preview, bool shatter)
    {
        var chances = new SortedDictionary<int, int>();
        var shatters = 0;
        foreach (var outcome in preview.Outcomes)
        {
            var units = UnitsOf(outcome);
            chances[units] = chances.GetValueOrDefault(units) + outcome.ChancePercent;
            if (outcome.Label.Contains("shatters", StringComparison.Ordinal))
                shatters += outcome.ChancePercent;
        }

        var text = "Takes " + string.Join(" or ", chances.Select(pair =>
            pair.Value == 100 ? UnitsText(pair.Key) : $"{UnitsText(pair.Key)} ({pair.Value}%)"));
        return shatter ? $"{text}. {shatters}% chance to shatter a gem pocket you have not surveyed" : text;
    }

    private static int UnitsOf(MinigameOutcomePreview outcome)
    {
        var yield = outcome.Change(MinigameMeterIds.Progress);
        return yield == null ? 0 : yield.Before - yield.After;
    }

    private static string UnitsText(int units) => units == 1 ? "1 unit" : $"{units} units";

    private static string CostText(MinigameActionPreview preview)
    {
        var outcome = preview.Outcomes.FirstOrDefault();
        return outcome == null ? string.Empty : ChangeText(outcome);
    }

    /// <summary>"Stability 100 -> 80, Charges 12 -> 11" for the meters that cost something.</summary>
    private static string ChangeText(MinigameOutcomePreview outcome)
    {
        var parts = new List<string>();
        foreach (var id in new[] { MinigameMeterIds.Integrity, MinigameMeterIds.Cp })
        {
            var change = outcome.Change(id);
            if (change != null && change.Before != change.After)
                parts.Add($"{change.Label} {change.Before} -> {change.After}");
        }

        return string.Join(", ", parts);
    }

    private static string EndingText(MinigameActionPreview preview)
    {
        if (preview.Action.Id == ProspectingActionIds.Leave)
            return null;

        var endings = preview.Outcomes
            .Select(outcome => (Outcome: outcome, Ending: EndingText(outcome)))
            .Where(pair => pair.Ending != null)
            .GroupBy(pair => pair.Ending)
            .Select(group => (Ending: group.Key, Chance: group.Sum(pair => pair.Outcome.ChancePercent)))
            .ToList();
        if (endings.Count == 0)
            return null;

        return string.Join(", ", endings.Select(pair => pair.Chance >= 100 ? pair.Ending : $"{pair.Chance}% {pair.Ending}"));
    }

    private static string TooltipText(MinigameActionPreview preview) =>
        preview.Action.Id switch
        {
            ProspectingActionIds.HeavyStrike =>
                $"{preview.Action.Description} The chance shown is the same whether or not a pocket is hidden here.",
            ProspectingActionIds.Survey =>
                $"{preview.Action.Description} It finds nothing once every seam has been found.",
            _ => preview.Action.Description
        };
}
