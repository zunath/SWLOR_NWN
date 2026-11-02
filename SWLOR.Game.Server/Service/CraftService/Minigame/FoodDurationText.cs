using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// The line a crafted food's description gets so players can see the extra duration its quality earned. The bonus itself lives in
/// the item local <see cref="CraftOutcomeRules.FoodDurationBonusSecondsVariable"/>, which the item tooltip does not show.
/// </summary>
public static class FoodDurationText
{
    public const string Label = "Bonus Duration";

    /// <summary>The bonus as time, such as "2 min 30 s" or "45 s".</summary>
    public static string Format(int seconds)
    {
        var minutes = seconds / 60;
        var rest = seconds % 60;
        if (minutes == 0)
            return $"{rest} s";

        return rest == 0 ? $"{minutes} min" : $"{minutes} min {rest} s";
    }

    /// <summary>The description line for a bonus, such as "Bonus Duration: +2 min 30 s".</summary>
    public static string Line(int seconds) => $"{Label}: +{Format(seconds)}";

    /// <summary>
    /// The description with the bonus line added after a blank line, or just the line when the item has no description. A
    /// description that already holds a bonus line is returned with that line replaced, so applying twice never doubles it.
    /// </summary>
    public static string AppendTo(string description, int seconds)
    {
        var existing = RemoveLine(description ?? string.Empty);
        if (seconds <= 0)
            return existing;

        return existing.Length == 0 ? Line(seconds) : $"{existing}\n\n{Line(seconds)}";
    }

    private static string RemoveLine(string description)
    {
        var kept = description
            .Replace("\r\n", "\n")
            .Split('\n')
            .Where(line => !line.StartsWith(Label + ":", StringComparison.Ordinal))
            .ToList();
        return string.Join("\n", kept).Trim();
    }
}
