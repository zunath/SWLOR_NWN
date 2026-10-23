using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

/// <summary>
/// The crafter's Circuit styles: which they own, which is active and how the craft window switches between them. A style is a
/// status effect that carries the style's stat adjustments, so at most one is active and the engine reads it like any other stat.
/// </summary>
public static class CircuitStyles
{
    /// <summary>Every style, in the order the window lists them.</summary>
    public static IReadOnlyList<CircuitStyle> All { get; } = new[] { CircuitStyle.Overclock, CircuitStyle.SafeMode };

    private static readonly IReadOnlyDictionary<CircuitStyle, IReadOnlyDictionary<StatType, int>> StyleStats =
        new Dictionary<CircuitStyle, IReadOnlyDictionary<StatType, int>>
        {
            [CircuitStyle.Overclock] = new Dictionary<StatType, int>
            {
                [StatType.CircuitExtraLoadPerBuildAction] = CircuitPerkRules.OverclockExtraLoad,
                [StatType.CircuitLoadBoostPercent] = CircuitPerkRules.OverclockLoadBoostPercent
            },
            [CircuitStyle.SafeMode] = new Dictionary<StatType, int>
            {
                [StatType.CircuitOverloadGrace] = CircuitPerkRules.SafeModeOverloadGrace,
                [StatType.CircuitLoadBoostPercent] = CircuitPerkRules.SafeModeLoadBoostPercent
            }
        };

    /// <summary>The stat adjustments a style's status effect carries.</summary>
    public static IReadOnlyDictionary<StatType, int> StatsOf(CircuitStyle style) =>
        StyleStats.TryGetValue(style, out var stats) ? stats : new Dictionary<StatType, int>();

    public static string Name(CircuitStyle style) => style switch
    {
        CircuitStyle.Overclock => "Overclock",
        CircuitStyle.SafeMode => "Safe Mode",
        _ => "None"
    };

    /// <summary>What the style does, as the window and the perk's description say it.</summary>
    public static string Description(CircuitStyle style) => style switch
    {
        CircuitStyle.Overclock =>
            $"Wire and Tune add {CircuitPerkRules.OverclockExtraLoad} extra Load, and each Load point adds {CircuitPerkRules.OverclockLoadBoostPercent} more {CircuitPerkRules.PointWord(CircuitPerkRules.OverclockLoadBoostPercent)} of output.",
        CircuitStyle.SafeMode =>
            $"Actions cannot overload while Load is no more than {CircuitPerkRules.SafeModeOverloadGrace} over the line, and each Load point adds {-CircuitPerkRules.SafeModeLoadBoostPercent} fewer {CircuitPerkRules.PointWord(-CircuitPerkRules.SafeModeLoadBoostPercent)} of output.",
        _ => "No style: the Circuit plays by its usual rules."
    };

    public static PerkType PerkOf(CircuitStyle style) => style switch
    {
        CircuitStyle.Overclock => PerkType.OverclockStyle,
        CircuitStyle.SafeMode => PerkType.SafeModeStyle,
        _ => PerkType.Invalid
    };

    public static Type StatusEffectOf(CircuitStyle style) => style switch
    {
        CircuitStyle.Overclock => typeof(OverclockStatusEffect),
        CircuitStyle.SafeMode => typeof(SafeModeStatusEffect),
        _ => null
    };

    public static bool IsOwned(uint player, CircuitStyle style) =>
        style != CircuitStyle.None && Perk.GetPerkLevel(player, PerkOf(style)) > 0;

    /// <summary>The styles the player has bought.</summary>
    public static IReadOnlyList<CircuitStyle> Owned(uint player) => All.Where(style => IsOwned(player, style)).ToList();

    /// <summary>The active style, or <see cref="CircuitStyle.None"/>.</summary>
    public static CircuitStyle Active(uint player) =>
        All.FirstOrDefault(style => StatusEffect.HasEffect(StatusEffectOf(style), player));

    /// <summary>Makes <paramref name="style"/> the only active style. A style the player does not own, or <see cref="CircuitStyle.None"/>, clears them all.</summary>
    public static void Select(uint player, CircuitStyle style)
    {
        if (!IsOwned(player, style))
            style = CircuitStyle.None;

        Remove(player, All.Where(other => other != style));

        if (style != CircuitStyle.None && !StatusEffect.HasEffect(StatusEffectOf(style), player))
            StatusEffect.ApplyPermanentStatusEffect(StatusEffectOf(style), player, player);
    }

    /// <summary>Removes an active style the player no longer owns, such as after refunding its perk.</summary>
    public static void RemoveUnowned(uint player)
    {
        Remove(player, All.Where(style => !IsOwned(player, style)));
    }

    private static void Remove(uint player, IEnumerable<CircuitStyle> styles)
    {
        foreach (var style in styles.ToList())
        {
            if (StatusEffect.HasEffect(StatusEffectOf(style), player))
                StatusEffect.RemoveStatusEffect(StatusEffectOf(style), player);
        }
    }
}
