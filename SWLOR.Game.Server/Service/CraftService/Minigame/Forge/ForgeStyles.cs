using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

/// <summary>
/// The crafter's Plasma Forge styles: which they own, which is active and how the Plasma Forge window switches between them.
/// Smithery is not split by activity, so a crafter may own both and toggles in the window. A style is a status effect that carries
/// the style's stat adjustments, so at most one is active and the engine reads it like any other stat. The selection is also kept
/// on the player record (<see cref="CraftingStyleSelection"/>), which applies the effect again after a death or a login.
/// </summary>
public static class ForgeStyles
{
    /// <summary>Every style, in the order the window lists them.</summary>
    public static IReadOnlyList<ForgeStyle> All { get; } = new[] { ForgeStyle.HotForging, ForgeStyle.ColdWorking };

    /// <summary>The skill whose craft window offers these styles.</summary>
    public const SkillType Skill = SkillType.Smithery;

    /// <summary>The styles as <see cref="CraftingStyleSelection"/> knows them.</summary>
    public static IReadOnlyList<CraftingStyleDefinition> Definitions =>
        All.Select(style => new CraftingStyleDefinition(Skill, PerkOf(style), StatusEffectOf(style))).ToList();

    private static readonly IReadOnlyDictionary<ForgeStyle, IReadOnlyDictionary<StatType, int>> StyleStats =
        new Dictionary<ForgeStyle, IReadOnlyDictionary<StatType, int>>
        {
            [ForgeStyle.HotForging] = new Dictionary<StatType, int>
            {
                [StatType.ForgeRangeShift] = ForgePerkRules.HotForgingRangeShift,
                [StatType.ForgePressOutputPercent] = ForgePerkRules.HotForgingPressPercent,
                [StatType.ForgeHoneOutputPercent] = ForgePerkRules.HotForgingHonePercent
            },
            [ForgeStyle.ColdWorking] = new Dictionary<StatType, int>
            {
                [StatType.ForgeRangeShift] = ForgePerkRules.ColdWorkingRangeShift,
                [StatType.ForgeHoneOutputPercent] = ForgePerkRules.ColdWorkingHonePercent,
                [StatType.ForgePressOutputPercent] = ForgePerkRules.ColdWorkingPressPercent
            }
        };

    /// <summary>The stat adjustments a style's status effect carries.</summary>
    public static IReadOnlyDictionary<StatType, int> StatsOf(ForgeStyle style) =>
        StyleStats.TryGetValue(style, out var stats) ? stats : new Dictionary<StatType, int>();

    public static string Name(ForgeStyle style) => style switch
    {
        ForgeStyle.HotForging => "Hot Forging",
        ForgeStyle.ColdWorking => "Cold Working",
        _ => "None"
    };

    /// <summary>What the style does, as the window and the perk's description say it.</summary>
    public static string Description(ForgeStyle style) => style switch
    {
        ForgeStyle.HotForging =>
            $"The working range moves up {ForgePerkRules.HotForgingRangeShift} Thermal. Hydro-Press gains {ForgePerkRules.HotForgingPressPercent}% more Shape and Vibro-Hone gains {-ForgePerkRules.HotForgingHonePercent}% less Finish.",
        ForgeStyle.ColdWorking =>
            $"The working range moves down {-ForgePerkRules.ColdWorkingRangeShift} Thermal. Vibro-Hone gains {ForgePerkRules.ColdWorkingHonePercent}% more Finish and Hydro-Press gains {-ForgePerkRules.ColdWorkingPressPercent}% less Shape.",
        _ => "No style: the Plasma Forge plays by its usual rules."
    };

    public static PerkType PerkOf(ForgeStyle style) => style switch
    {
        ForgeStyle.HotForging => PerkType.HotForgingStyle,
        ForgeStyle.ColdWorking => PerkType.ColdWorkingStyle,
        _ => PerkType.Invalid
    };

    public static Type StatusEffectOf(ForgeStyle style) => style switch
    {
        ForgeStyle.HotForging => typeof(HotForgingStatusEffect),
        ForgeStyle.ColdWorking => typeof(ColdWorkingStatusEffect),
        _ => null
    };

    public static bool IsOwned(uint player, ForgeStyle style) =>
        style != ForgeStyle.None && Perk.GetPerkLevel(player, PerkOf(style)) > 0;

    /// <summary>The styles the player has bought.</summary>
    public static IReadOnlyList<ForgeStyle> Owned(uint player) => All.Where(style => IsOwned(player, style)).ToList();

    /// <summary>The active style, or <see cref="ForgeStyle.None"/>.</summary>
    public static ForgeStyle Active(uint player) =>
        All.FirstOrDefault(style => StatusEffect.HasEffect(StatusEffectOf(style), player));

    /// <summary>
    /// Makes <paramref name="style"/> the only active style and records it as the crafter's selection. A style the player does not own,
    /// or <see cref="ForgeStyle.None"/>, clears them all.
    /// </summary>
    public static void Select(uint player, ForgeStyle style)
    {
        if (!IsOwned(player, style))
            style = ForgeStyle.None;

        Remove(player, All.Where(other => other != style));

        if (style != ForgeStyle.None && !StatusEffect.HasEffect(StatusEffectOf(style), player))
            StatusEffect.ApplyPermanentStatusEffect(StatusEffectOf(style), player, player);

        CraftingStyleSelection.Record(player, Skill, PerkOf(style));
    }

    /// <summary>Removes an active style the player no longer owns, such as after refunding its perk, and forgets it as their selection.</summary>
    public static void RemoveUnowned(uint player)
    {
        var unowned = All.Where(style => !IsOwned(player, style)).ToList();
        Remove(player, unowned);

        foreach (var style in unowned)
            CraftingStyleSelection.ClearIfSelected(player, Skill, PerkOf(style));
    }

    private static void Remove(uint player, IEnumerable<ForgeStyle> styles)
    {
        foreach (var style in styles.ToList())
        {
            if (StatusEffect.HasEffect(StatusEffectOf(style), player))
                StatusEffect.RemoveStatusEffect(StatusEffectOf(style), player);
        }
    }
}
