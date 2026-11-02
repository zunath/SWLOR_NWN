using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

/// <summary>
/// The perks of the Overclocker and Technician lines, and the stats of a crafter who owns some of them at an Engineering rank. The
/// simulator and the tests use it to play and check a crafter without a character.
/// </summary>
public static class CircuitPerkLines
{
    public static IReadOnlyList<PerkType> Overclocker { get; } = new[]
    {
        PerkType.ConductiveWiring, PerkType.CapacitorBank, PerkType.HeatSinks, PerkType.OverclockStyle,
        PerkType.CascadeDischarge, PerkType.OverdriveBoost, PerkType.ShockAbsorbers, PerkType.CircuitSurge
    };

    public static IReadOnlyList<PerkType> Technician { get; } = new[]
    {
        PerkType.InsulatedTraces, PerkType.FineCalibration, PerkType.CircuitDiagnostics, PerkType.SafeModeStyle,
        PerkType.MicroTune, PerkType.SignalLock, PerkType.PrecisionPatch, PerkType.CircuitFailsafe
    };

    /// <summary>
    /// The stats of a crafter at <paramref name="rank"/> who owns, of <paramref name="owned"/>, the highest level that rank allows
    /// of each perk, with <paramref name="style"/> active from the rank its perk opens at. A style adds its status effect's stats
    /// to the perks' as the stat pipeline does.
    /// </summary>
    /// <param name="perks">The built perk definitions, from <c>EngineeringPerkDefinition.BuildPerks</c>.</param>
    /// <param name="owned">The perks the crafter has bought.</param>
    /// <param name="rank">The crafter's Engineering rank.</param>
    /// <param name="style">The active style, or <see cref="CircuitStyle.None"/>.</param>
    public static CircuitPerkStats StatsAt(
        IReadOnlyDictionary<PerkType, PerkDetail> perks,
        IEnumerable<PerkType> owned,
        int rank,
        CircuitStyle style = CircuitStyle.None)
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

        if (style != CircuitStyle.None && rank >= CircuitRules.StyleRank)
        {
            foreach (var (stat, value) in CircuitStyles.StatsOf(style))
                Add(totals, stat, value);
        }

        return CircuitPerkStats.FromStats(totals.GetValueOrDefault);
    }

    /// <summary>The Engineering rank a perk level needs.</summary>
    public static int RequiredRank(PerkLevel level) => level.Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank;

    private static void Add(Dictionary<StatType, int> totals, StatType stat, int value)
    {
        totals.TryGetValue(stat, out var current);
        totals[stat] = Stat.AggregateStatAdjustment(stat, current, value);
    }
}
