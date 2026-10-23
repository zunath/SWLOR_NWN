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

    /// <summary>
    /// The XP refining pays for units of an ore. Each unit pays the ore's flat XP (25 to 150 by ore, what refining always paid) to a
    /// refiner whose rank equals the ore's level, and the rank scales it on harvesting's curve:
    /// <c>flat XP x GetDeltaXP(ore level - rank) / GetDeltaXP(0)</c>. A refiner below the level earns up to twice the flat XP, one five
    /// ranks past it earns nothing, so cheap ore stops training high ranks while refining at level pays what it always did.
    /// </summary>
    /// <param name="flatXpPerUnit">The ore's XP per unit at its level.</param>
    /// <param name="oreTier">The ore's tier, 1 to 5; its level is <see cref="ForTier"/>.</param>
    /// <param name="gatheringRank">The refiner's Gathering rank.</param>
    /// <param name="units">The units refined.</param>
    public static int RefiningXp(int flatXpPerUnit, int oreTier, int gatheringRank, int units) =>
        (int)Math.Round(
            (double)flatXpPerUnit * Math.Max(0, units) * Skill.GetDeltaXP(ForTier(oreTier) - gatheringRank) / Skill.GetDeltaXP(0),
            MidpointRounding.AwayFromZero);
}
