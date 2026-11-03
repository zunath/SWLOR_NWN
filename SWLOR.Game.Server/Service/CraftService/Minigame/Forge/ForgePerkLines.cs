using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

/// <summary>
/// The perks of the Forgemaster and Artisan lines, and the stats of a crafter who owns some of them at a Smithery rank. The
/// simulator and the tests use it to play and check a crafter without a character.
/// </summary>
public static class ForgePerkLines
{
    public static IReadOnlyList<PerkType> Forgemaster { get; } = new[]
    {
        PerkType.SmitheryPlasmaEfficiency, PerkType.SmitheryHeatRetention, PerkType.SmitheryOverdrivePress, PerkType.HotForgingStyle,
        PerkType.SmitheryHammerRhythm, PerkType.SmitheryScorchGuard, PerkType.SmitherySurgePlasma, PerkType.SmitheryFastRebond,
        PerkType.SmitheryRedLine
    };

    public static IReadOnlyList<PerkType> Artisan { get; } = new[]
    {
        PerkType.SmitherySteadyHands, PerkType.SmitheryCleanContainment, PerkType.SmitheryMicroHone, PerkType.ColdWorkingStyle,
        PerkType.SmitherySteadyQuench, PerkType.SmitheryCoolHand, PerkType.SmitheryTemperHold, PerkType.SmitheryContainmentFocus,
        PerkType.SmitheryPerfectTemper
    };

    /// <summary>
    /// The stats of a crafter at <paramref name="rank"/> who owns, of <paramref name="owned"/>, the highest level that rank allows
    /// of each perk, with <paramref name="style"/> active from the rank its perk opens at. A style adds its status effect's stats
    /// to the perks' as the stat pipeline does.
    /// </summary>
    /// <param name="perks">The built perk definitions, from <c>SmitheryPerkDefinition.BuildPerks</c>.</param>
    /// <param name="owned">The perks the crafter has bought.</param>
    /// <param name="rank">The crafter's Smithery rank.</param>
    /// <param name="style">The active style, or <see cref="ForgeStyle.None"/>.</param>
    public static ForgePerkStats StatsAt(
        IReadOnlyDictionary<PerkType, PerkDetail> perks,
        IEnumerable<PerkType> owned,
        int rank,
        ForgeStyle style = ForgeStyle.None)
    {
        var totals = new Dictionary<StatType, int>();
        foreach (var type in owned)
        {
            var level = perks[type].PerkLevels
                .Where(pair => RequiredRank(pair.Value) <= rank)
                .OrderBy(pair => pair.Key)
                .Select(pair => pair.Value)
                .LastOrDefault();
            if (level == null)
                continue;

            foreach (var bonus in level.StatBonuses)
                Add(totals, bonus.Stat, bonus.Calculate(0));
        }

        if (style != ForgeStyle.None && rank >= ForgePerkRules.StyleRank)
        {
            foreach (var (stat, value) in ForgeStyles.StatsOf(style))
                Add(totals, stat, value);
        }

        return ForgePerkStats.FromStats(totals.GetValueOrDefault);
    }

    /// <summary>The Smithery rank a perk level needs.</summary>
    public static int RequiredRank(PerkLevel level) => level.Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank;

    private static void Add(Dictionary<StatType, int> totals, StatType stat, int value)
    {
        totals.TryGetValue(stat, out var current);
        totals[stat] = Stat.AggregateStatAdjustment(stat, current, value);
    }
}
