using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

/// <summary>The words the Plasma Forge window shows for a meter and for what an action will do, from the same previews the engine resolves with.</summary>
public static class ForgePreviewText
{
    public const string FinishesItem = "FINISHES THE ITEM";
    public const string BreaksItem = "MAY BREAK THE PIECE";

    /// <summary>A Shape or Finish meter as a percentage of its maximum, such as "45.3%".</summary>
    public static string Percent(MinigameMeter meter) =>
        meter.Max <= 0 ? "0.0%" : (meter.Value * 100.0 / meter.Max).ToString("F1", CultureInfo.InvariantCulture) + "%";

    /// <summary>What one result does to every meter that moves, such as "Shape +14.2%, Integrity -5, Thermal 60 -> 55".</summary>
    public static string Summarize(MinigameOutcomePreview outcome, ForgeSetup setup)
    {
        var parts = new List<string>();
        AddGain(parts, outcome.Change(MinigameMeterIds.Progress), "Shape", setup.ShapeMax);
        AddGain(parts, outcome.Change(MinigameMeterIds.Quality), "Finish", setup.FinishMax);
        AddPoints(parts, outcome.Change(MinigameMeterIds.Integrity), "Integrity");
        AddPoints(parts, outcome.Change(MinigameMeterIds.Cp), "CP");

        var thermal = outcome.Change(ForgeEngine.ThermalMeterId);
        if (thermal != null && thermal.Delta != 0)
            parts.Add($"Thermal {thermal.Before} -> {thermal.After}");

        var text = string.Join(", ", parts);
        if (outcome.FinishesItem)
            text += (text.Length == 0 ? string.Empty : ". ") + FinishesItem;
        else if (outcome.BreaksItem)
            text += (text.Length == 0 ? string.Empty : ". ") + BreaksItem;

        return text;
    }

    /// <summary>The line beside an action's button: its result, or why it cannot be used.</summary>
    public static string Describe(MinigameActionPreview preview, ForgeSetup setup)
    {
        if (!preview.Enabled)
            return preview.DisabledReason;

        var results = preview.Outcomes.Select(outcome => Summarize(outcome, setup)).ToList();
        return results.Count == 1
            ? results[0]
            : string.Join(" | ", preview.Outcomes.Zip(results, (outcome, text) => $"{outcome.ChancePercent}% {outcome.Label}: {text}"));
    }

    /// <summary>Which enhancement slots the Finish reached so far would transfer, such as "Enhancements: Slot 1 met (50%), Slot 2 not met (85%)".</summary>
    public static string Thresholds(int slotCount, int qualityPercent)
    {
        if (slotCount <= 0)
            return "No enhancement slots.";

        var slots = CraftOutcomeRules.GetEnhancementThresholds(slotCount)
            .Select((threshold, index) => $"Slot {index + 1} {(qualityPercent >= threshold ? "met" : "not met")} ({threshold}%)");
        return "Enhancements: " + string.Join(", ", slots);
    }

    /// <summary>
    /// The active style and what the crafter's perks are counting right now: Hammer Rhythm's bonus on the next Hydro-Press, the Red
    /// Line Press and the actions Temper Hold has left. An empty string when the crafter has neither a style nor such a perk.
    /// </summary>
    public static string PerkSummary(ForgeEngine engine, ForgeStyle style)
    {
        var parts = new List<string>();
        var perks = engine.Setup.Perks;
        if (style != ForgeStyle.None)
            parts.Add($"Style: {ForgeStyles.Name(style)}");

        if (perks.PressStreakPercent > 0)
        {
            var bonus = Math.Min(engine.PressStreak, ForgePerkRules.HammerRhythmMaxStacks) * perks.PressStreakPercent;
            parts.Add($"Hammer Rhythm: next Hydro-Press +{bonus}%");
        }

        if (perks.RedLineCharges > 0)
            parts.Add(engine.RedLineReady ? "Red Line ready" : "Red Line used");

        if (perks.TemperHoldActions > 0 && engine.TemperHoldLeft > 0)
            parts.Add($"Temper Hold: {engine.TemperHoldLeft} {(engine.TemperHoldLeft == 1 ? "action" : "actions")} left");

        return string.Join("  |  ", parts);
    }

    /// <summary>The action's own description with its costs, for the button's tooltip.</summary>
    public static string Tooltip(MinigameActionPreview preview)
    {
        var costs = new List<string>();
        if (preview.CpCost > 0)
            costs.Add($"{preview.CpCost} CP");
        if (preview.IntegrityCost > 0)
            costs.Add($"{preview.IntegrityCost} Integrity");

        var cost = costs.Count == 0 ? "No cost besides cooling." : "Costs " + string.Join(" and ", costs) + ".";
        return $"{preview.Action.Description} {cost}";
    }

    /// <summary>The button text: the action's name and its CP cost.</summary>
    public static string ButtonText(MinigameActionPreview preview) =>
        preview.CpCost > 0 ? $"{preview.Action.Name} [{preview.CpCost} CP]" : preview.Action.Name;

    private static void AddGain(List<string> parts, MinigameMeterChange change, string label, int max)
    {
        if (change == null || change.Delta == 0 || max <= 0)
            return;

        var percent = change.Delta * 100.0 / max;
        parts.Add($"{label} {(percent > 0 ? "+" : string.Empty)}{percent.ToString("F1", CultureInfo.InvariantCulture)}%");
    }

    private static void AddPoints(List<string> parts, MinigameMeterChange change, string label)
    {
        if (change == null || change.Delta == 0)
            return;

        parts.Add($"{label} {(change.Delta > 0 ? "+" : string.Empty)}{change.Delta}");
    }
}
