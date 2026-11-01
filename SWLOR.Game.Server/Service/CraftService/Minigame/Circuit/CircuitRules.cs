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

    // The actions the Overclocker and Technician perks add. Their sizes are shares of the ordinary action's.
    public const int CascadeDischargeCp = 5;
    public const int CascadeDischargeSharePercent = 60;
    public const int MicroTuneCp = 2;
    public const int MicroTuneIntegrity = 2;
    public const int MicroTuneSharePercent = 70;

    /// <summary>The Engineering rank at which each perk line offers its style.</summary>
    public const int StyleRank = 20;

    /// <summary>The lowest Integrity an action costs once a perk reduces its cost.</summary>
    public const int MinimumActionIntegrity = 1;

    /// <summary>The most actions beyond the next at which a Clean Signal can be announced.</summary>
    public const int MaxForecastTurns = 1;

    // CP: gear CP plus this share of the rank, plus a flat bonus from rank 25.
    public const double CpPerRank = 0.75;
    public const int RankPassiveCpRank = 25;
    public const int RankPassiveCp = 31;

    // What a Circuit craft's base XP is multiplied by, for an item and for a failure alike, per recipe-level band. CraftSim's XP-rate
    // gate found the Circuit pays less XP per hour than the old window at low levels, where the old window took 6 to 9
    // actions and the Circuit takes 14 to 18. Each is the smallest two-decimal multiplier at which every case of its band (every gear
    // tier, with none to three enhancements) reaches the old window's XP per hour on both of the samples the notes name (6 recipes
    // per band at seed 1, 10 per band at seed 2). Higher bands need little or nothing. The space revamp's Engineering recipes changed the
    // cohort, so 11-20 and 21-30 were raised from 1.54 and 1.15 to keep every case of the committed sample at 1.00x or more.
    public const double XpMultiplierLevels1To10 = 2.29;
    public const double XpMultiplierLevels11To20 = 1.72;
    public const double XpMultiplierLevels21To30 = 1.19;
    public const double XpMultiplierLevels31To40 = 1.0;
    public const double XpMultiplierLevels41AndUp = 1.0;

    /// <summary>The multiplier on a Circuit craft's base XP for a recipe of the given level.</summary>
    public static double XpMultiplierFor(int recipeLevel) => recipeLevel switch
    {
        <= 10 => XpMultiplierLevels1To10,
        <= 20 => XpMultiplierLevels11To20,
        <= 30 => XpMultiplierLevels21To30,
        <= 40 => XpMultiplierLevels31To40,
        _ => XpMultiplierLevels41AndUp
    };

    /// <summary>The Engineering rank that unlocks Patch. Every other action is open from rank 0; rank changes power and CP only.</summary>
    public const int PatchRank = 10;

    /// <summary>How much Assembly the Circuit adds per point of an enhancement's progress penalty. The penalty is in the old window's progress units.</summary>
    public const double EnhancementAssemblyPerPenalty = 1.0;

    /// <summary>
    /// The most the enhancements together add to the Assembly, as a share of the Assembly the recipe itself needs. Low-level recipes
    /// need little Assembly, so an uncapped penalty would more than double it and no craft could finish.
    /// </summary>
    public const double MaxEnhancementAssemblyShare = 0.3;

    /// <summary>The Assembly the selected enhancements add to what a piece needs, from their total progress penalty and the Assembly the recipe itself needs.</summary>
    public static int EnhancementAssembly(int progressPenalty, int assemblySize)
    {
        if (progressPenalty <= 0)
            return 0;

        var scaled = (int)Math.Round(progressPenalty * EnhancementAssemblyPerPenalty, MidpointRounding.AwayFromZero);
        var cap = (int)Math.Round(Math.Max(1, assemblySize) * MaxEnhancementAssemblyShare, MidpointRounding.AwayFromZero);
        return Math.Min(scaled, cap);
    }
}
