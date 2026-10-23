using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>The two Fabrication perk lines as the game builds them, and the stat totals a crafter of a rank gets from owning them.</summary>
internal static class FabricationPerkLines
{
    public static readonly PerkType[] BuilderPerks =
    {
        PerkType.QuickChange, PerkType.HardenedHeads, PerkType.FieldService, PerkType.ProductionRun, PerkType.FineTolerance,
        PerkType.DualFeed, PerkType.FeedstockReserve, PerkType.FeedstockContract, PerkType.MasterAssembler
    };

    public static readonly PerkType[] ResearcherPerks =
    {
        PerkType.FieldNotes, PerkType.SteadyHands, PerkType.SecondOpinion, PerkType.Methodical, PerkType.Intuitive,
        PerkType.PeerReview, PerkType.EfficientAnalysis, PerkType.BroaderHypotheses, PerkType.Breakthrough
    };

    public static IReadOnlyDictionary<PerkType, PerkDetail> Builder() => new FabricationBuilderPerkDefinition().BuildPerks();

    public static IReadOnlyDictionary<PerkType, PerkDetail> Researcher() => new FabricationResearcherPerkDefinition().BuildPerks();

    /// <summary>The highest level of a perk a crafter of the rank can own, or 0.</summary>
    public static int OwnedLevel(PerkDetail detail, int rank)
    {
        var owned = 0;
        foreach (var (level, perkLevel) in detail.PerkLevels.OrderBy(pair => pair.Key))
        {
            var required = perkLevel.Requirements.OfType<PerkRequirementSkill>().Single(requirement => requirement.Type == SkillType.Fabrication).RequiredRank;
            if (required > rank)
                break;

            owned = level;
        }

        return owned;
    }

    /// <summary>
    /// The stat totals of a crafter of <paramref name="rank"/> who owns every perk of the line except <paramref name="without"/>,
    /// at the highest level their rank allows: what <c>Stat.GetStatAdjustment</c> returns for them.
    /// </summary>
    public static Func<StatType, int> Totals(
        IReadOnlyDictionary<PerkType, PerkDetail> line, int rank, params PerkType[] without)
    {
        var totals = new Dictionary<StatType, int>();
        foreach (var (type, detail) in line)
        {
            if (without.Contains(type))
                continue;

            var level = OwnedLevel(detail, rank);
            if (level == 0)
                continue;

            foreach (var bonus in detail.PerkLevels[level].StatBonuses)
            {
                totals.TryGetValue(bonus.Stat, out var current);
                totals[bonus.Stat] = Stat.AggregateStatAdjustment(bonus.Stat, current, bonus.Calculate(0));
            }
        }

        return stat => totals.TryGetValue(stat, out var value) ? value : 0;
    }

    /// <summary>The totals of a crafter who owns only the listed perks, at the highest level their rank allows.</summary>
    public static Func<StatType, int> TotalsOf(IReadOnlyDictionary<PerkType, PerkDetail> line, int rank, params PerkType[] owned) =>
        Totals(line, rank, line.Keys.Where(type => !owned.Contains(type)).ToArray());
}
