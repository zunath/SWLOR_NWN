using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;

/// <summary>
/// The gatherer's styles: which they own, which is active and how the Prospecting and Search windows switch between them. A style is a
/// status effect that carries the style's stat adjustments, so at most one is active and the engines read it like any other stat. Gathering
/// is not split by activity, so a gatherer can own both styles and picks one in the window. The selection is also kept on the
/// player record (<see cref="CraftingStyleSelection"/>), which applies the effect again after a death or a login.
/// </summary>
public static class GatheringStyles
{
    /// <summary>The Gathering rank both styles open at.</summary>
    public const int StyleRank = 20;

    /// <summary>The skill whose windows offer these styles. Both styles belong to it, so a gatherer has one selected at a time.</summary>
    public const SkillType Skill = SkillType.Gathering;

    /// <summary>Every style, in the order the windows list them.</summary>
    public static IReadOnlyList<GatheringStyle> All { get; } = new[] { GatheringStyle.StripMining, GatheringStyle.CarefulHands };

    /// <summary>The styles as <see cref="CraftingStyleSelection"/> knows them.</summary>
    public static IReadOnlyList<CraftingStyleDefinition> Definitions =>
        All.Select(style => new CraftingStyleDefinition(Skill, PerkOf(style), StatusEffectOf(style))).ToList();

    private static readonly IReadOnlyDictionary<GatheringStyle, IReadOnlyDictionary<StatType, int>> StyleStats =
        new Dictionary<GatheringStyle, IReadOnlyDictionary<StatType, int>>
        {
            [GatheringStyle.StripMining] = new Dictionary<StatType, int>
            {
                [StatType.ProspectingStrikeTimeReduction] = ProspectingPerkRules.StripMiningStrikeTimeTenths,
                [StatType.ProspectingSeamWorkTimeReduction] = -ProspectingPerkRules.StripMiningSeamWorkTimeTenths,
                [StatType.ProspectingStrikeStabilityReduction] = -ProspectingPerkRules.StripMiningStrikeStability
            },
            [GatheringStyle.CarefulHands] = new Dictionary<StatType, int>
            {
                [StatType.SearchChancePoints] = SearchPerkRules.CarefulHandsSearchChancePoints,
                [StatType.SearchDigRareWeight] = SearchPerkRules.CarefulHandsDigRareWeight
            }
        };

    /// <summary>The stat adjustments a style's status effect carries.</summary>
    public static IReadOnlyDictionary<StatType, int> StatsOf(GatheringStyle style) =>
        StyleStats.TryGetValue(style, out var stats) ? stats : new Dictionary<StatType, int>();

    public static string Name(GatheringStyle style) => style switch
    {
        GatheringStyle.StripMining => "Strip Mining",
        GatheringStyle.CarefulHands => "Careful Hands",
        _ => "None"
    };

    /// <summary>What the style does in Prospecting, as the window and the perk's description say it. Empty when it does nothing there.</summary>
    public static string ProspectingDescription(GatheringStyle style) => style switch
    {
        GatheringStyle.StripMining =>
            $"A Strike takes {ProspectingPerkRules.SecondsText(ProspectingPerkRules.StripMiningStrikeTimeTenths)} {ProspectingPerkRules.SecondsWord(ProspectingPerkRules.StripMiningStrikeTimeTenths)} less and costs {ProspectingPerkRules.StripMiningStrikeStability} more Stability, but a Survey and an Extract each take {ProspectingPerkRules.SecondsText(ProspectingPerkRules.StripMiningSeamWorkTimeTenths)} {ProspectingPerkRules.SecondsWord(ProspectingPerkRules.StripMiningSeamWorkTimeTenths)} more.",
        _ => string.Empty
    };

    /// <summary>What the style does in Search, as the window and the perk's description say it. Empty when it does nothing there.</summary>
    public static string SearchDescription(GatheringStyle style) => style switch
    {
        GatheringStyle.CarefulHands =>
            $"A Search finds {-SearchPerkRules.CarefulHandsSearchChancePoints} {(SearchPerkRules.CarefulHandsSearchChancePoints == -1 ? "point" : "points")} less often, and a Dig Deeper find favours the site's rare entries {SearchPerkRules.CarefulHandsDigRareWeight} more.",
        _ => string.Empty
    };

    /// <summary>What the style does everywhere, for the perk's description.</summary>
    public static string Description(GatheringStyle style)
    {
        var parts = new[] { ProspectingDescription(style), SearchDescription(style) };
        var prospecting = parts[0].Length == 0 ? string.Empty : "In Prospecting: " + parts[0];
        var search = parts[1].Length == 0 ? string.Empty : "In Search: " + parts[1];
        return string.Join(" ", new[] { prospecting, search }.Where(part => part.Length > 0));
    }

    public static PerkType PerkOf(GatheringStyle style) => style switch
    {
        GatheringStyle.StripMining => PerkType.StripMiningStyle,
        GatheringStyle.CarefulHands => PerkType.CarefulHandsStyle,
        _ => PerkType.Invalid
    };

    public static Type StatusEffectOf(GatheringStyle style) => style switch
    {
        GatheringStyle.StripMining => typeof(StripMiningStatusEffect),
        GatheringStyle.CarefulHands => typeof(CarefulHandsStatusEffect),
        _ => null
    };

    public static bool IsOwned(uint player, GatheringStyle style) =>
        style != GatheringStyle.None && Perk.GetPerkLevel(player, PerkOf(style)) > 0;

    /// <summary>The styles the player has bought.</summary>
    public static IReadOnlyList<GatheringStyle> Owned(uint player) => All.Where(style => IsOwned(player, style)).ToList();

    /// <summary>The active style, or <see cref="GatheringStyle.None"/>.</summary>
    public static GatheringStyle Active(uint player) =>
        All.FirstOrDefault(style => StatusEffect.HasEffect(StatusEffectOf(style), player));

    /// <summary>
    /// Makes <paramref name="style"/> the only active style and records it as the gatherer's selection. A style the player does not own,
    /// or <see cref="GatheringStyle.None"/>, clears them all.
    /// </summary>
    public static void Select(uint player, GatheringStyle style)
    {
        if (!IsOwned(player, style))
            style = GatheringStyle.None;

        Remove(player, All.Where(other => other != style));

        if (style != GatheringStyle.None && !StatusEffect.HasEffect(StatusEffectOf(style), player))
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

    private static void Remove(uint player, IEnumerable<GatheringStyle> styles)
    {
        foreach (var style in styles.ToList())
        {
            if (StatusEffect.HasEffect(StatusEffectOf(style), player))
                StatusEffect.RemoveStatusEffect(StatusEffectOf(style), player);
        }
    }
}
