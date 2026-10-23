namespace SWLOR.Game.Server.Service.CraftService;

/// <summary>
/// The activity levels Gathering XP is measured at. Harvesting, Prospecting and refining all pay <c>Skill.GetDeltaXP(level - rank)</c>,
/// and they share one ladder of levels so that a unit of ore pays the same wherever it is worked: a vein of tier 1 to 5 is level 5, 15,
/// 25, 35 and 45, which are the ranks at which each harvester tier unlocks (0, 10, 20, 30, 40) plus five.
/// </summary>
public static class GatheringLevels
{
    /// <summary>The ranks between two tiers.</summary>
    public const int LevelsPerTier = 10;

    /// <summary>The level of the first tier.</summary>
    public const int FirstTierLevel = 5;

    /// <summary>
    /// The level of a vein, tree or ore of the tier (1 to 5): <c>10 x (tier - 1) + 5</c>. This is the level harvesting has always paid
    /// XP at (<c>HarvesterItemDefinition</c>), and refining uses it for an ore of the same tier.
    /// </summary>
    public static int ForTier(int tier) => LevelsPerTier * (Math.Max(1, tier) - 1) + FirstTierLevel;

    /// <summary>The XP one unit of a tier pays a Gathering rank: <c>GetDeltaXP(ForTier(tier) - rank)</c>.</summary>
    public static int XpPerUnit(int tier, int gatheringRank) => Skill.GetDeltaXP(ForTier(tier) - gatheringRank);
}
