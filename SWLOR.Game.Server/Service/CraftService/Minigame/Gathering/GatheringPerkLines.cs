using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;

/// <summary>
/// The perks of the Prospector and Treasure Seeker lines, and the stats of a gatherer who owns some of them at a Gathering rank. The
/// simulator and the tests use it to play and check a gatherer without a character.
/// </summary>
public static class GatheringPerkLines
{
    public static IReadOnlyList<PerkType> Prospector { get; } = new[]
    {
        PerkType.EfficientStrikes, PerkType.BulkRefining, PerkType.RichVeins, PerkType.StripMiningStyle, PerkType.CoreSample, PerkType.MotherLode
    };

    public static IReadOnlyList<PerkType> TreasureSeeker { get; } = new[]
    {
        PerkType.FineTrowel, PerkType.SharpChisel, PerkType.SteadyDig, PerkType.CarefulHandsStyle,
        PerkType.KeenEye, PerkType.DeepScan, PerkType.RichPickings, PerkType.Jackpot
    };

    /// <summary>
    /// The stat totals of a gatherer at <paramref name="rank"/> who owns, of <paramref name="owned"/>, the highest level that rank allows
    /// of each perk, with <paramref name="style"/> active from the rank its perk opens at. A style adds its status effect's stats to the
    /// perks' as the stat pipeline does.
    /// </summary>
    /// <param name="perks">The built perk definitions, from <c>GatheringPerkDefinition.BuildPerks</c>.</param>
    /// <param name="owned">The perks the gatherer has bought.</param>
    /// <param name="rank">The gatherer's Gathering rank.</param>
    /// <param name="style">The active style, or <see cref="GatheringStyle.None"/>.</param>
    public static IReadOnlyDictionary<StatType, int> StatsAt(
        IReadOnlyDictionary<PerkType, PerkDetail> perks,
        IEnumerable<PerkType> owned,
        int rank,
        GatheringStyle style = GatheringStyle.None)
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

        if (style != GatheringStyle.None && rank >= GatheringStyles.StyleRank)
        {
            foreach (var (stat, value) in GatheringStyles.StatsOf(style))
                Add(totals, stat, value);
        }

        return totals;
    }

    /// <summary>The Gathering rank a perk level needs.</summary>
    public static int RequiredRank(PerkLevel level) => level.Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank;

    private static void Add(Dictionary<StatType, int> totals, StatType stat, int value)
    {
        totals.TryGetValue(stat, out var current);
        totals[stat] = Stat.AggregateStatAdjustment(stat, current, value);
    }
}
