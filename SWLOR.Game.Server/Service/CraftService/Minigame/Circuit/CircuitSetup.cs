namespace SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

/// <summary>
/// Everything the Circuit engine takes as fixed for one craft: the recipe's size and parameters, the crafter's rank and gear, and
/// the progress the selected enhancements add. It holds no state; the engine's state record does.
/// </summary>
public sealed class CircuitSetup
{
    /// <param name="parameters">The recipe's Circuit parameters; null plays with <see cref="CircuitParameters.Fallback"/>.</param>
    /// <param name="recipeLevel">The recipe's level.</param>
    /// <param name="baseProgress">The recipe level's progress size, from the level chart, before the Assembly share.</param>
    /// <param name="baseQuality">The recipe level's quality size, from the level chart, before the Assembly share.</param>
    /// <param name="maxIntegrity">The recipe level's durability, from the level chart.</param>
    /// <param name="rank">The crafter's Engineering rank.</param>
    /// <param name="craftsmanship">The crafter's Engineering Craftsmanship, which powers Assembly.</param>
    /// <param name="control">The crafter's Engineering Control, which powers Calibration.</param>
    /// <param name="gearCp">The CP from the crafter's gear.</param>
    /// <param name="enhancementProgressPenalty">The Assembly the selected enhancements add to what the piece needs.</param>
    public CircuitSetup(
        CircuitParameters parameters,
        int recipeLevel,
        int baseProgress,
        int baseQuality,
        int maxIntegrity,
        int rank,
        int craftsmanship,
        int control,
        int gearCp,
        int enhancementProgressPenalty = 0)
    {
        Parameters = parameters ?? CircuitParameters.Fallback;
        RecipeLevel = recipeLevel;
        Rank = Math.Max(0, rank);
        Craftsmanship = Math.Max(0, craftsmanship);
        Control = Math.Max(0, control);
        GearCp = Math.Max(0, gearCp);
        EnhancementProgressPenalty = Math.Max(0, enhancementProgressPenalty);

        var assemblyShare = Math.Clamp(Parameters.AssemblySharePercent, 1, 99);
        AssemblySize = Math.Max(1, (int)Math.Round(baseProgress * assemblyShare / 50.0, MidpointRounding.AwayFromZero));
        CalibrationSize = Math.Max(1, (int)Math.Round(baseQuality * (100 - assemblyShare) / 50.0, MidpointRounding.AwayFromZero));
        MaxIntegrity = Math.Max(1, maxIntegrity);
        MaxAssembly = AssemblySize + EnhancementProgressPenalty;
        MaxCalibration = CalibrationSize;
        MaxCp = (int)(GearCp + Rank * CircuitRules.CpPerRank) + (Rank >= CircuitRules.RankPassiveCpRank ? CircuitRules.RankPassiveCp : 0);
    }

    public CircuitParameters Parameters { get; }

    public int RecipeLevel { get; }

    public int Rank { get; }

    public int Craftsmanship { get; }

    public int Control { get; }

    public int GearCp { get; }

    public int EnhancementProgressPenalty { get; }

    /// <summary>The Assembly the recipe itself needs. Wire and Discharge output is a share of this, so enhancements do not shrink it.</summary>
    public int AssemblySize { get; }

    /// <summary>The Calibration the recipe needs, which is also the quality meter's maximum.</summary>
    public int CalibrationSize { get; }

    /// <summary>The Assembly that completes the craft: the recipe's own plus what the selected enhancements add.</summary>
    public int MaxAssembly { get; }

    public int MaxCalibration { get; }

    public int MaxIntegrity { get; }

    public int MaxCp { get; }

    public int OverloadLine => Parameters.OverloadLine;

    /// <summary>The Integrity an overload costs.</summary>
    public int OverloadIntegrityCost =>
        Math.Max(1, (int)Math.Round(MaxIntegrity * CircuitRules.OverloadIntegrityPercent / 100.0, MidpointRounding.AwayFromZero));

    public MinigameMeters StartingMeters() => MinigameMeters.Start(MaxAssembly, MaxCalibration, MaxIntegrity, MaxCp);

    /// <summary>The multiplier a Load level gives to Wire, Tune and Discharge output: the recipe's boost per point, per point.</summary>
    public double LoadMultiplier(int load) => 1.0 + Parameters.LoadBoostPercent / 100.0 * Math.Max(0, load);

    /// <summary>The chance, in percent, that an action that leaves Load at the given level overloads.</summary>
    public int OverloadChancePercent(int loadAfterAction)
    {
        var pointsOver = loadAfterAction - OverloadLine;
        return pointsOver <= 0
            ? 0
            : Math.Min(CircuitRules.MaxOverloadChancePercent, pointsOver * CircuitRules.OverloadChancePerPointPercent);
    }

    /// <summary>The Assembly a Wire adds at the given Load.</summary>
    public int WireGain(int load) => Scale(AssemblySize, CircuitRules.WirePercent, load, AssemblyFactor());

    /// <summary>The Calibration a Tune adds at the given Load, before a Clean Signal doubles it.</summary>
    public int TuneGain(int load) => Scale(CalibrationSize, CircuitRules.TunePercent, load, CalibrationFactor());

    /// <summary>The Assembly a Discharge adds when it spends the given Load.</summary>
    public int DischargeAssemblyGain(int load) =>
        load <= 0 ? 0 : Scale(AssemblySize, CircuitRules.DischargePercentPerLoad * load, load, AssemblyFactor());

    /// <summary>The Calibration a Discharge adds when it spends the given Load.</summary>
    public int DischargeCalibrationGain(int load) =>
        load <= 0 ? 0 : Scale(CalibrationSize, CircuitRules.DischargePercentPerLoad * load, load, CalibrationFactor());

    /// <summary>The Engineering rank an action unlocks at: Patch at 10 and everything else at 0.</summary>
    public static int UnlockRank(string actionId) => actionId == CircuitActionIds.Patch ? CircuitRules.PatchRank : 0;

    public bool IsUnlocked(string actionId) => Rank >= UnlockRank(actionId);

    private double AssemblyFactor() =>
        (1.0 + Craftsmanship * CircuitRules.GearPercentPerPoint / 100.0) * RankFactor(CircuitRules.MaxRanksAboveRecipe);

    private double CalibrationFactor() =>
        (1.0 + Control * CircuitRules.GearPercentPerPoint / 100.0) * RankFactor(0);

    /// <summary>A crafter below the recipe's level works at a penalty; a crafter above it works faster, up to <paramref name="maxAbove"/> ranks.</summary>
    private double RankFactor(int maxAbove)
    {
        var difference = Math.Clamp(Rank - RecipeLevel, -CircuitRules.MaxRanksBelowRecipe, maxAbove);
        return 1.0 + (difference < 0 ? CircuitRules.RankBelowStep : CircuitRules.RankStep) * difference;
    }

    private int Scale(int size, double percent, int load, double factor)
    {
        var gain = size * percent / 100.0 * LoadMultiplier(load) * factor;
        return Math.Max(1, (int)Math.Round(gain, MidpointRounding.AwayFromZero));
    }
}
