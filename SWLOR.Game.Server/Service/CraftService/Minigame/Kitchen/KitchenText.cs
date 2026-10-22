using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>
/// The words the Kitchen window shows, built from engine state so they stay exact and can be tested without the game. Game text
/// uses ASCII punctuation only.
/// </summary>
public static class KitchenText
{
    public const string FinishesWarning = "Finishes the item!";
    public const string RuinsWarning = "Can ruin the dish!";

    private static readonly KitchenAxis[] Axes = { KitchenAxis.Savory, KitchenAxis.Sweet, KitchenAxis.Spice };

    private static readonly (string Id, string Label)[] EffectMeters =
    {
        (MinigameMeterIds.Progress, "Doneness"),
        (KitchenEngine.SavoryMeterId, "Savory"),
        (KitchenEngine.SweetMeterId, "Sweet"),
        (KitchenEngine.SpiceMeterId, "Spice"),
        (MinigameMeterIds.Quality, "Quality")
    };

    public static string Signed(int value) => value > 0 ? $"+{value}" : value.ToString();

    /// <summary>The rule for a failed dish, shown before the ingredients are committed.</summary>
    public static string FailureRule() =>
        $"If the dish is ruined, or you close this window or walk away, each ingredient stack and each enhancement is lost " +
        $"on a roll of {CraftOutcomeRules.ChanceToLoseItemPercent}% or less. A failed dish still earns " +
        $"{(int)(CraftOutcomeRules.FailureXpFraction * 100)}% of the XP.";

    /// <summary>The pantry seasonings every cook may add for free, as one line.</summary>
    public static string PantryLine() =>
        $"Pantry (free, unlimited): Salt {Signed(KitchenTuning.PantryPush)} Savory, " +
        $"Sugar {Signed(KitchenTuning.PantryPush)} Sweet, Spice {Signed(KitchenTuning.PantryPush)} Spice.";

    /// <summary>What one committed ingredient does, as one line for the setup list.</summary>
    public static string IngredientLine(KitchenIngredientLine line)
    {
        var portions = Enumerable.Range(0, line.Portions).ToList();
        if (line.Kind == KitchenIngredientKind.Dilute)
        {
            var percents = string.Join("/", portions.Select(line.DilutePercentAt).Select(percent => percent.ToString()));
            return $"{line.Name} x{line.Quantity}: thins every flavor by {percents}% per portion " +
                   $"({line.Portions} {Portions(line.Portions)})";
        }

        // A stack that does not divide evenly gives its first portions a little more, so list each portion's push.
        var axes = Axes
            .Where(axis => portions.Any(portion => line.PortionVectorAt(portion).Get(axis) != 0))
            .Select(axis =>
            {
                var values = portions.Select(portion => line.PortionVectorAt(portion).Get(axis)).ToList();
                var shown = values.Distinct().Count() == 1
                    ? Signed(values[0])
                    : string.Join("/", values.Select(Signed));
                return $"{axis} {shown}";
            })
            .ToList();
        var push = axes.Count == 0 ? "no flavor" : string.Join(", ", axes);
        return $"{line.Name} x{line.Quantity}: {push} per portion ({line.Portions} {Portions(line.Portions)})";
    }

    /// <summary>A push as words, such as "Savory +13, Spice +2"; axes it does not touch are left out.</summary>
    public static string VectorText(FlavorVector vector)
    {
        var parts = Axes.Where(axis => vector.Get(axis) != 0).Select(axis => $"{axis} {Signed(vector.Get(axis))}").ToList();
        return parts.Count == 0 ? "no flavor" : string.Join(", ", parts);
    }

    /// <summary>The target as words: its center, how close counts as perfect, and where quality reaches nothing.</summary>
    public static string TargetText(KitchenTarget target) =>
        $"Target flavor: Savory {target.Center.Savory}, Sweet {target.Center.Sweet}, Spice {target.Center.Spice}. " +
        $"Perfect within {target.InnerRadius}; no quality at {target.OuterRadius} or more.";

    public static string AxisText(KitchenAxisView axis) =>
        $"{axis.Value} (target {axis.Center}, perfect {axis.PerfectFrom}-{axis.PerfectTo}, nothing outside {axis.ToleranceFrom}-{axis.ToleranceTo})";

    public static string DistanceText(double distance, KitchenTarget target)
    {
        var rounded = (int)Math.Round(distance, MidpointRounding.AwayFromZero);
        var standing = distance <= target.InnerRadius
            ? "Perfect."
            : distance >= target.OuterRadius ? "No quality yet." : "Quality is falling.";
        return $"Off target by {rounded}. Perfect within {target.InnerRadius}; no quality at {target.OuterRadius} or more. {standing}";
    }

    public static string HeatText(KitchenHeat heat, int coverTurns)
    {
        var description = heat switch
        {
            KitchenHeat.Low => "cooks slowly and holds the flavor steady",
            KitchenHeat.Medium => "cooks at a steady pace and drifts the flavor a little",
            _ => "cooks fast and drifts the flavor every turn"
        };
        var lid = coverTurns > 0
            ? $" The lid is on for {coverTurns} more {(coverTurns == 1 ? "turn" : "turns")}."
            : string.Empty;
        return $"Heat {heat}: {description}.{lid}";
    }

    /// <summary>The announced drift and what it will do at the current heat.</summary>
    public static string DriftText(FlavorVector announced, FlavorVector effective, KitchenHeat heat)
    {
        if (announced.IsZero)
            return "No drift is announced.";

        var atHigh = announced.Scaled(KitchenTuning.DriftPercentAtHigh);
        var atMedium = announced.Scaled(KitchenTuning.DriftPercentAtMedium);
        var now = effective.IsZero
            ? $"At {heat} heat it does nothing."
            : $"At {heat} heat: {VectorText(effective)}.";
        return $"Next drift: {VectorText(atHigh)} at High, {VectorText(atMedium)} at Medium, none at Low. {now}";
    }

    public static string AromaText(bool peakAromaNext) => peakAromaNext
        ? "PEAK AROMA: your next Add is twice as strong!"
        : $"Peak Aroma: not announced. Each turn has a {KitchenTuning.PeakAromaChancePercent}% chance of announcing it.";

    /// <summary>Which of the filled enhancement slots (numbered from 1) the current quality already guarantees.</summary>
    public static string ThresholdText(int qualityPercent, IReadOnlyList<int> filledSlots)
    {
        if (filledSlots.Count == 0)
            return "No enhancements in this dish.";

        var slots = filledSlots.Select(slot =>
        {
            var threshold = CraftOutcomeRules.GetEnhancementThreshold(slot);
            return $"slot {slot} needs {threshold}% ({(qualityPercent >= threshold ? "met" : "not met")})";
        });
        return "Enhancements: " + string.Join(", ", slots) + ".";
    }

    /// <summary>One action's row: what it costs and does, the exact range when chance is involved, and the warnings.</summary>
    public static KitchenActionLine ActionLine(MinigameActionPreview preview)
    {
        var tooltip = Tooltip(preview);
        if (!preview.Enabled)
            return new KitchenActionLine(preview.DisabledReason, KitchenActionTone.Unavailable, tooltip);

        var outcomes = preview.Outcomes;
        var typical = outcomes.OrderByDescending(outcome => outcome.ChancePercent).First();
        var parts = new List<string>();

        foreach (var (id, label) in EffectMeters)
        {
            var deltas = outcomes.Select(outcome => outcome.Change(id)?.Delta ?? 0).ToList();
            if (deltas.All(delta => delta == 0))
                continue;

            var text = $"{label} {Signed(typical.Change(id)?.Delta ?? 0)}";
            if (deltas.Min() != deltas.Max())
                text += $" ({Signed(deltas.Min())} to {Signed(deltas.Max())})";
            parts.Add(text);
        }

        var heat = typical.Change(KitchenEngine.HeatMeterId);
        if (heat != null && heat.Delta != 0)
            parts.Add($"Heat becomes {(KitchenHeat)heat.After}");

        var cover = typical.Change(KitchenEngine.CoverMeterId);
        if (cover != null && cover.Delta > 0)
            parts.Add("Lid on");

        if (preview.CpCost > 0)
            parts.Add($"CP -{preview.CpCost}");

        var freshness = typical.Change(MinigameMeterIds.Integrity)?.Delta ?? 0;
        if (freshness != 0)
            parts.Add($"Freshness {Signed(freshness)}");

        if (parts.Count == 0)
            parts.Add(preview.Action.Description);

        var tone = KitchenActionTone.Normal;
        if (outcomes.Count > 1)
            parts.Add("Chances " + string.Join("/", outcomes.Select(outcome => outcome.ChancePercent)) + "%");

        if (preview.FinishesItem)
        {
            tone = KitchenActionTone.Finishes;
            parts.Add(FinishesWarning);
        }
        else if (preview.CanBreakItem)
        {
            tone = KitchenActionTone.Ruins;
            parts.Add(RuinsWarning);
        }

        return new KitchenActionLine(string.Join(" | ", parts), tone, tooltip);
    }

    /// <summary>Everything an action can do, one line per result with its chance.</summary>
    public static string Tooltip(MinigameActionPreview preview)
    {
        var lines = new List<string> { preview.Action.Name, preview.Action.Description };
        if (!preview.Enabled)
        {
            lines.Add("Unavailable: " + preview.DisabledReason);
            return string.Join("\n", lines);
        }

        if (preview.CpCost > 0)
            lines.Add($"Costs {preview.CpCost} CP.");

        foreach (var outcome in preview.Outcomes)
            lines.Add($"{outcome.ChancePercent}% {ResultText(outcome)}");

        return string.Join("\n", lines);
    }

    /// <summary>What a result did, such as "Steady hand: Quality +40, Freshness -4, Savory +13." with any ending warning.</summary>
    public static string ResultText(MinigameOutcomePreview outcome)
    {
        var changes = outcome.Changes
            .Where(change => change.Delta != 0 && !change.Id.StartsWith("stock.", StringComparison.Ordinal) &&
                             change.Id != KitchenEngine.DistanceMeterId && change.Id != MinigameMeterIds.Cp)
            .Select(change => change.Id == KitchenEngine.HeatMeterId
                ? $"Heat becomes {(KitchenHeat)change.After}"
                : $"{change.Label} {Signed(change.Delta)}")
            .ToList();
        var result = changes.Count == 0 ? "no change" : string.Join(", ", changes);
        var ending = outcome.FinishesItem ? $" {FinishesWarning}" : outcome.BreaksItem ? " The dish is ruined!" : string.Empty;
        return $"{outcome.Label}: {result}.{ending}";
    }

    private static string Portions(int count) => count == 1 ? "portion" : "portions";
}
