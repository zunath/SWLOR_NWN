namespace SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;

/// <summary>
/// The numbers of the Fabricator minigame. Every value here is a starting value for the simulator to tune; the engine reads them
/// and nothing else, so a tuning pass changes this file only.
/// </summary>
public static class FabricatorRules
{
    /// <summary>
    /// Progress meters count in tenths of the level chart's progress points, so a Run's share of a small recipe is not lost to
    /// whole-number rounding.
    /// </summary>
    public const int ProgressScale = 10;

    /// <summary>The highest quality a sub-assembly, and the piece, can have.</summary>
    public const int MaxQuality = 100;

    public const int MaxMachineCondition = 100;

    // Run: the share of the recipe's progress one Run adds to the sub-assembly its head serves, before gear and rank.
    public const double RunPercent = 28.0;

    /// <summary>A Run on a sub-assembly the active head does not serve gives this share of the effect, with no retooling.</summary>
    public const double OffHeadFactor = 0.5;

    /// <summary>The quality points one Precision Pass adds to the active head's sub-assembly, before gear and rank.</summary>
    public const int PrecisionPoints = 46;

    /// <summary>Retooling wears the machine a little; a Run or Precision Pass wears it by the recipe's wear rate.</summary>
    public const int RetoolWear = 1;

    /// <summary>The Machine Condition one Service restores.</summary>
    public const int ServiceRestore = 40;

    // CP costs rise with the recipe's level, so the budget a rank gives stays meaningful at every level.
    public const int RetoolCpBase = 1;
    public const int RetoolCpLevelDivisor = 6;
    public const int ServiceCpBase = 2;
    public const int ServiceCpLevelDivisor = 3;

    // Prime Feedstock: each turn with none in play, each head has this chance to be the one it names.
    public const int PrimeChancePerHeadPercent = 12;
    public const int PrimeBonusPercent = 50;

    /// <summary>The number of actions, counting the next one, in which the named head can still use its prime.</summary>
    public const int PrimeWindowActions = 2;

    /// <summary>Assemble caps the piece's quality at its weakest sub-assembly plus this many points.</summary>
    public const int AssembleCapPoints = 20;

    /// <summary>Each point of Craftsmanship (Run) or Control (Precision Pass) adds this percent to the output.</summary>
    public const double GearPercentPerPoint = 1.0;

    /// <summary>The output each rank above or below the recipe's level adds, as a fraction.</summary>
    public const double RankStep = 0.05;

    public const int MaxRanksBelowRecipe = 3;
    public const int MaxRanksAboveRecipe = 5;

    // CP: gear CP plus this share of the rank, plus a flat bonus from rank 25.
    public const double CpPerRank = 0.75;
    public const int RankPassiveCpRank = 25;
    public const int RankPassiveCp = 31;

    // The multiplier on the base XP of every Fabricator craft in a recipe-level band (see CraftMinigameXp), each the least that keeps
    // the CraftSim XP-rate gate passing in its band at every gear tier and enhancement count. The Fabricator's 10-20 action loop
    // at 8 seconds an action pays less XP per hour than the old window's 5-28 action loop at 6 seconds, and the old window is fastest
    // at the lowest levels. Starting values until client timing replaces the seconds per action.
    public const double XpMultiplierLevels1To10 = 2.4;
    public const double XpMultiplierLevels11To20 = 1.50;
    public const double XpMultiplierLevels21To30 = 1.25;
    public const double XpMultiplierLevels31To40 = 1.1;
    public const double XpMultiplierLevels41Up = 1.0;

    /// <summary>The multiplier on the base XP of a Fabricator craft at a recipe level. A level of 0 or less takes the lowest band's.</summary>
    public static double XpMultiplierForLevel(int recipeLevel) => recipeLevel switch
    {
        <= 10 => XpMultiplierLevels1To10,
        <= 20 => XpMultiplierLevels11To20,
        <= 30 => XpMultiplierLevels21To30,
        <= 40 => XpMultiplierLevels31To40,
        _ => XpMultiplierLevels41Up
    };

    /// <summary>The Fabrication rank that unlocks Service. Every other action is available from rank 0.</summary>
    public const int ServiceRank = 10;

    /// <summary>The CP one Retool costs at a recipe level.</summary>
    public static int RetoolCp(int recipeLevel) => RetoolCpBase + Math.Max(0, recipeLevel) / RetoolCpLevelDivisor;

    /// <summary>The CP one Service costs at a recipe level.</summary>
    public static int ServiceCp(int recipeLevel) => ServiceCpBase + Math.Max(0, recipeLevel) / ServiceCpLevelDivisor;

    /// <summary>Each of Dual Feed's two sub-assemblies gets this share of what a full Run makes.</summary>
    public const double DualFeedPercent = 60.0;

    /// <summary>The least CP an action with a perk discount costs.</summary>
    public const int MinimumDiscountedCp = 1;

    /// <summary>A CP cost less a percentage, rounded to the nearest CP and never below <see cref="MinimumDiscountedCp"/>.</summary>
    public static int ReduceCpByPercent(int cp, int percent) =>
        Math.Max(MinimumDiscountedCp, (int)Math.Round(cp * (100 - Math.Clamp(percent, 0, 100)) / 100.0, MidpointRounding.AwayFromZero));

    /// <summary>A CP cost less a flat amount, never below <see cref="MinimumDiscountedCp"/>.</summary>
    public static int ReduceCp(int cp, int amount) => Math.Max(MinimumDiscountedCp, cp - Math.Max(0, amount));
}
