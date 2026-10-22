namespace SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

/// <summary>
/// The numbers of the Circuit minigame. Every value here is a starting value for the simulator to tune; the engine reads them and
/// nothing else, so a tuning pass changes this file only.
/// </summary>
public static class CircuitRules
{
    /// <summary>The highest Load. The overload line is a recipe parameter below it.</summary>
    public const int MaxLoad = 10;

    // Overload: an action that leaves Load above the recipe's line can overload.
    public const int OverloadChancePerPointPercent = 8;
    public const int MaxOverloadChancePercent = 90;
    /// <summary>The Integrity an overload costs, as a share of the piece's maximum Integrity.</summary>
    public const int OverloadIntegrityPercent = 30;

    // Output at Load 0, as a percent of the target meter's maximum, before gear, rank and the Load boost.
    public const double WirePercent = 8.5;
    public const double TunePercent = 5.5;
    public const double DischargePercentPerLoad = 2.5;

    /// <summary>Each point of Craftsmanship (Assembly) or Control (Calibration) adds this percent to the output.</summary>
    public const double GearPercentPerPoint = 1.0;

    /// <summary>The output each rank above or below the recipe's level adds, as a fraction.</summary>
    public const double RankStep = 0.05;
    public const int MaxRanksBelowRecipe = 3;
    public const int MaxRanksAboveRecipe = 5;

    public const int WireLoad = 1;
    public const int TuneLoad = 1;
    public const int BoostLoad = 3;
    public const int GroundLoad = 3;
    public const int PatchRestore = 20;

    // CP costs.
    public const int TuneCp = 3;
    public const int BoostCp = 4;
    public const int GroundCp = 3;
    public const int PatchCp = 8;

    // Integrity costs. Patch restores Integrity instead of spending it.
    public const int WireIntegrity = 3;
    public const int TuneIntegrity = 3;
    public const int BoostIntegrity = 3;
    public const int DischargeIntegrity = 4;
    public const int GroundIntegrity = 2;

    /// <summary>The chance each turn that the next turn announces a Clean Signal, once Tune is unlocked.</summary>
    public const int CleanSignalChancePercent = 25;
    public const int CleanSignalMultiplier = 2;

    // CP: gear CP plus this share of the rank, plus a flat bonus from rank 25.
    public const double CpPerRank = 0.75;
    public const int RankPassiveCpRank = 25;
    public const int RankPassiveCp = 31;

    /// <summary>
    /// What a Circuit craft's base XP is multiplied by, for an item and for a failure alike. CraftSim's XP-rate gate found the Circuit pays
    /// less XP per hour than the old window at low levels, where the old window took 6 to 9 actions and the Circuit takes 10 or more.
    /// </summary>
    public const double XpMultiplier = 2.4;

    /// <summary>The Engineering rank that unlocks Patch. Every other action is open from rank 0; rank changes power and CP only.</summary>
    public const int PatchRank = 10;
}
