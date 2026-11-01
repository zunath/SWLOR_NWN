namespace SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

/// <summary>
/// Everything the Circuit engine takes as fixed for one craft: the recipe's size and parameters, the crafter's rank, gear and perk
/// stats, and the progress the selected enhancements add. It holds no state; the engine's state record does.
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
    /// <param name="perks">What the crafter's Engineering perks and style change; null plays without them.</param>
    /// <param name="stationAssemblyPercent">
    /// The crafting station's bonus to Assembly output, in percent: a droid assigned to a station Shipyard speeds up Capital hull builds
    /// (space revamp). 0 everywhere else.
    /// </param>
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
        int enhancementProgressPenalty = 0,
        CircuitPerkStats perks = null,
        int stationAssemblyPercent = 0)
    {
        Parameters = parameters ?? CircuitParameters.Fallback;
        Perks = perks ?? CircuitPerkStats.None;
        RecipeLevel = recipeLevel;
        Rank = Math.Max(0, rank);
        Craftsmanship = Math.Max(0, craftsmanship);
        Control = Math.Max(0, control);
        GearCp = Math.Max(0, gearCp);
        EnhancementProgressPenalty = Math.Max(0, enhancementProgressPenalty);
        StationAssemblyPercent = Math.Max(0, stationAssemblyPercent);

        var assemblyShare = Math.Clamp(Parameters.AssemblySharePercent, 1, 99);
        AssemblySize = Math.Max(1, (int)Math.Round(baseProgress * assemblyShare / 50.0, MidpointRounding.AwayFromZero));
        CalibrationSize = Math.Max(1, (int)Math.Round(baseQuality * (100 - assemblyShare) / 50.0, MidpointRounding.AwayFromZero));
        MaxIntegrity = Math.Max(1, maxIntegrity);
        EnhancementAssembly = CircuitRules.EnhancementAssembly(EnhancementProgressPenalty, AssemblySize);
        MaxAssembly = AssemblySize + EnhancementAssembly;
        MaxCalibration = CalibrationSize;
        MaxCp = (int)(GearCp + Rank * CircuitRules.CpPerRank) + (Rank >= CircuitRules.RankPassiveCpRank ? CircuitRules.RankPassiveCp : 0);
    }

    public CircuitParameters Parameters { get; }

    /// <summary>What the crafter's perks and style change, captured when the craft starts.</summary>
    public CircuitPerkStats Perks { get; }

    public int RecipeLevel { get; }

    public int Rank { get; }

    public int Craftsmanship { get; }

    public int Control { get; }

    public int GearCp { get; }

    /// <summary>The crafting station's bonus to Assembly output, in percent (a station Shipyard's droid on Capital hulls).</summary>
    public int StationAssemblyPercent { get; }

    /// <summary>The selected enhancements' total progress penalty, in the old window's progress units.</summary>
    public int EnhancementProgressPenalty { get; }

    /// <summary>The Assembly the enhancements add to what the piece needs: the penalty, scaled and capped by <see cref="CircuitRules.EnhancementAssembly"/>.</summary>
    public int EnhancementAssembly { get; }

    /// <summary>The Assembly the recipe itself needs. Wire and Discharge output is a share of this, so enhancements do not shrink it.</summary>
    public int AssemblySize { get; }

    /// <summary>The Calibration the recipe needs, which is also the quality meter's maximum.</summary>
    public int CalibrationSize { get; }

    /// <summary>The Assembly that completes the craft: the recipe's own plus what the selected enhancements add.</summary>
    public int MaxAssembly { get; }

    public int MaxCalibration { get; }

    public int MaxIntegrity { get; }

    public int MaxCp { get; }

    /// <summary>The Load above which an action can overload: the recipe's line plus the perks', never past the highest Load.</summary>
    public int OverloadLine => Math.Min(CircuitRules.MaxLoad, Parameters.OverloadLine + Perks.OverloadLine);

    /// <summary>The percentage points of output each Load point adds: the recipe's plus the perks' and style's.</summary>
    public int LoadBoostPercent => Math.Max(0, Parameters.LoadBoostPercent + Perks.LoadBoostPercent);

    /// <summary>The Integrity an overload costs.</summary>
    public int OverloadIntegrityCost
    {
        get
        {
            var kept = 100 - Math.Clamp(Perks.OverloadIntegrityReductionPercent, 0, 100);
            return Math.Max(1, (int)Math.Round(MaxIntegrity * CircuitRules.OverloadIntegrityPercent * kept / 10000.0, MidpointRounding.AwayFromZero));
        }
    }

    public int WireLoad => CircuitRules.WireLoad + Math.Max(0, Perks.ExtraLoadPerBuildAction);

    public int TuneLoad => CircuitRules.TuneLoad + Math.Max(0, Perks.ExtraLoadPerBuildAction);

    public int BoostLoad => CircuitRules.BoostLoad + Math.Max(0, Perks.BoostLoad);

    public int WireIntegrity => ReduceIntegrity(CircuitRules.WireIntegrity);

    public int TuneIntegrity => ReduceIntegrity(CircuitRules.TuneIntegrity);

    public int BoostIntegrity => ReduceIntegrity(CircuitRules.BoostIntegrity);

    public int DischargeIntegrity => ReduceIntegrity(CircuitRules.DischargeIntegrity);

    public int MicroTuneIntegrity => ReduceIntegrity(CircuitRules.MicroTuneIntegrity);

    public int BoostCp => Math.Max(0, CircuitRules.BoostCp - Math.Max(0, Perks.BoostCpReduction));

    public int PatchCp => Math.Max(0, CircuitRules.PatchCp - Math.Max(0, Perks.PatchCpReduction));

    public int PatchRestore => CircuitRules.PatchRestore + Math.Max(0, Perks.PatchRestore);

    /// <summary>The chance, in percent, that a turn announces a Clean Signal.</summary>
    public int CleanSignalChancePercent => Math.Clamp(CircuitRules.CleanSignalChancePercent + Perks.SignalChancePercent, 0, 100);

    /// <summary>The actions ahead a Clean Signal is announced: 1, or 2 with Diagnostics.</summary>
    public int ForecastDepth => 1 + Math.Clamp(Perks.ForecastTurns, 0, CircuitRules.MaxForecastTurns);

    /// <summary>The highest Load at which no action can overload: the line plus the perks' grace.</summary>
    public int SafeLoad => Math.Min(CircuitRules.MaxLoad, OverloadLine + Math.Max(0, Perks.OverloadGrace));

    public MinigameMeters StartingMeters() => MinigameMeters.Start(MaxAssembly, MaxCalibration, MaxIntegrity, MaxCp);

    /// <summary>The multiplier a Load level gives to Wire, Tune and Discharge output: the recipe's boost per point, per point.</summary>
    public double LoadMultiplier(int load) => 1.0 + LoadBoostPercent / 100.0 * Math.Max(0, load);

    /// <summary>
    /// The chance, in percent, that an action that leaves Load at the given level overloads. It is 0 while Load is no more than
    /// the perks' grace past the line.
    /// </summary>
    public int OverloadChancePercent(int loadAfterAction)
    {
        var pointsOver = loadAfterAction - OverloadLine;
        return pointsOver <= Math.Max(0, Perks.OverloadGrace)
            ? 0
            : Math.Min(CircuitRules.MaxOverloadChancePercent, pointsOver * CircuitRules.OverloadChancePerPointPercent);
    }

    /// <summary>The Assembly a Wire adds at the given Load.</summary>
    public int WireGain(int load) => Scale(AssemblySize, CircuitRules.WirePercent, load, AssemblyFactor());

    /// <summary>The Calibration a Tune adds at the given Load, before a Clean Signal doubles it.</summary>
    public int TuneGain(int load) => Scale(CalibrationSize, CircuitRules.TunePercent, load, CalibrationFactor() * TunePowerFactor());

    /// <summary>The Calibration a Micro-Tune adds at the given Load: a share of a Tune's, and it adds no Load.</summary>
    public int MicroTuneGain(int load) =>
        Scale(CalibrationSize, CircuitRules.TunePercent * CircuitRules.MicroTuneSharePercent / 100.0, load, CalibrationFactor() * TunePowerFactor());

    /// <summary>The Assembly a Discharge adds when it spends the given Load.</summary>
    public int DischargeAssemblyGain(int load) =>
        load <= 0 ? 0 : Scale(AssemblySize, CircuitRules.DischargePercentPerLoad * load, load, AssemblyFactor() * DischargePowerFactor());

    /// <summary>The Calibration a Discharge adds when it spends the given Load.</summary>
    public int DischargeCalibrationGain(int load) =>
        load <= 0 ? 0 : Scale(CalibrationSize, CircuitRules.DischargePercentPerLoad * load, load, CalibrationFactor() * DischargePowerFactor());

    /// <summary>The Assembly a Cascade Discharge adds when it spends the given Load.</summary>
    public int CascadeAssemblyGain(int load) =>
        load <= 0
            ? 0
            : Scale(AssemblySize, CircuitRules.DischargePercentPerLoad * load * CircuitRules.CascadeDischargeSharePercent / 100.0, load,
                AssemblyFactor() * DischargePowerFactor());

    /// <summary>The Calibration a Cascade Discharge adds when it spends the given Load.</summary>
    public int CascadeCalibrationGain(int load) =>
        load <= 0
            ? 0
            : Scale(CalibrationSize, CircuitRules.DischargePercentPerLoad * load * CircuitRules.CascadeDischargeSharePercent / 100.0, load,
                CalibrationFactor() * DischargePowerFactor());

    /// <summary>The Engineering rank an action unlocks at: Patch at 10 and everything else at 0.</summary>
    public static int UnlockRank(string actionId) => actionId == CircuitActionIds.Patch ? CircuitRules.PatchRank : 0;

    public bool IsUnlocked(string actionId) => Rank >= UnlockRank(actionId);

    private int ReduceIntegrity(int cost) =>
        Perks.ActionIntegrityReduction <= 0
            ? cost
            : Math.Max(Math.Min(cost, CircuitRules.MinimumActionIntegrity), cost - Perks.ActionIntegrityReduction);

    private double TunePowerFactor() => 1.0 + Math.Max(0, Perks.TunePowerPercent) / 100.0;

    private double DischargePowerFactor() => 1.0 + Math.Max(0, Perks.DischargePowerPercent) / 100.0;

    private double AssemblyFactor() =>
        (1.0 + Craftsmanship * CircuitRules.GearPercentPerPoint / 100.0) * RankFactor(CircuitRules.MaxRanksAboveRecipe) *
        (1.0 + StationAssemblyPercent / 100.0);

    private double CalibrationFactor() =>
        (1.0 + Control * CircuitRules.GearPercentPerPoint / 100.0) * RankFactor(0);

    /// <summary>A crafter below the recipe's level works at a penalty; a crafter above it works faster, up to <paramref name="maxAbove"/> ranks.</summary>
    private double RankFactor(int maxAbove)
    {
        var difference = Math.Clamp(Rank - RecipeLevel, -CircuitRules.MaxRanksBelowRecipe, maxAbove);
        return 1.0 + CircuitRules.RankStep * difference;
    }

    private int Scale(int size, double percent, int load, double factor)
    {
        var gain = size * percent / 100.0 * LoadMultiplier(load) * factor;
        return Math.Max(1, (int)Math.Round(gain, MidpointRounding.AwayFromZero));
    }
}
