namespace SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

/// <summary>
/// Every tuned value of the Reaction minigame, in one place. Espionage has no crafting gear, so its Craftsmanship, Control and CP
/// equivalents come from Espionage rank alone. All arithmetic is in whole numbers, so a gain rounds down the same way on every
/// machine. The values were tuned with the CraftSim gates (design/testing/crafting-revamp/reaction-gates.md).
/// </summary>
public static class ReactionRules
{
    /// <summary>The maximum of Potency and Subtlety. Subtlety is the quality percentage.</summary>
    public const int MeterMaximum = 100;

    /// <summary>The maximum of Stability, which every action wears down.</summary>
    public const int StabilityMaximum = 200;

    /// <summary>The Craftsmanship and Control a crafter of this rank is assumed to carry: 3 per 5 ranks, 30 at rank 50 (the best gear tier).</summary>
    public const int EquivalentStatNumerator = 3;
    public const int EquivalentStatDenominator = 5;

    /// <summary>Each point of Craftsmanship or Control raises its actions by this many percent (30 points: +30%).</summary>
    public const int BonusPercentPerStatPoint = 1;

    /// <summary>Each Espionage rank above the recipe's level raises Potency actions by 5% (legacy rule), up to the maximum.</summary>
    public const int FactorPercentPerRankEdge = 5;

    public const int MinimumFactorPercent = 50;
    public const int MaximumProgressFactorPercent = 125;
    public const int MaximumQualityFactorPercent = 100;

    /// <summary>CP is 3 per 4 ranks; Espionage has no gear CP.</summary>
    public const int CpNumerator = 3;
    public const int CpDenominator = 4;

    /// <summary>The rank-25 CP passive. The simulator re-tuned it to nothing: CP is a budget of one Stabilize at the recipe's level.</summary>
    public const int CpPassiveRank = 25;
    public const int CpPassive = 0;

    public const int MeasurePotency = 4;
    public const int ChargePotency = 14;
    public const int RefineSubtlety = 11;

    public const int PeakChargeMultiplier = 2;
    public const int SettlingRefineMultiplier = 2;

    /// <summary>Refine at Peak does its usual Subtlety divided by this, rounded down, and at least 1.</summary>
    public const int PeakRefineDivisor = 2;

    /// <summary>
    /// The Stability every action but Stabilize costs, by Peak intensity 1 to 5. A more intense mixture wears the piece faster, which
    /// keeps the Stability budget as tight in the high tiers, where CP is plentiful, as in the low ones.
    /// </summary>
    private static readonly int[] ActionStabilityByIntensity = { 7, 9, 9, 10, 12 };

    /// <summary>The extra Stability a Charge costs in Rising or Settling.</summary>
    public const int HeavyChargeStability = 36;

    /// <summary>A Charge at Peak costs <see cref="HeavyChargeStability"/> plus this much per point of Peak intensity.</summary>
    public const int PeakStabilityPerIntensity = 8;

    public const int StabilizeRestore = 40;

    /// <summary>Stabilize costs 3 CP for every 5 recipe levels, at least <see cref="MinimumStabilizeCp"/>: one use at the recipe's level.</summary>
    public const int StabilizeCpNumerator = 3;
    public const int StabilizeCpDenominator = 5;
    public const int MinimumStabilizeCp = 4;

    /// <summary>The Espionage rank Stabilize unlocks at. Every other Reaction action is available from rank 0.</summary>
    public const int StabilizeUnlockRank = 10;

    public const int DampenCp = 6;

    /// <summary>The multiplier on a Reaction craft's base XP that keeps its XP per hour at the old loop's in the 1-10 band (gate 6).</summary>
    public const double XpMultiplier = 3.0;

    public static int EquivalentCraftsmanship(int rank) =>
        Math.Max(rank, 0) * EquivalentStatNumerator / EquivalentStatDenominator;

    public static int EquivalentControl(int rank) =>
        Math.Max(rank, 0) * EquivalentStatNumerator / EquivalentStatDenominator;

    /// <summary>Gear CP (none) plus 3 per 4 ranks, and the rank-25 passive from rank 25.</summary>
    public static int MaxCp(int rank)
    {
        rank = Math.Max(rank, 0);
        return rank * CpNumerator / CpDenominator + (rank >= CpPassiveRank ? CpPassive : 0);
    }

    /// <summary>The percent multiplier on Potency actions from rank against recipe level: 5% per level, from 50% up to 125%.</summary>
    public static int ProgressFactorPercent(int rank, int recipeLevel) =>
        Math.Clamp(100 + FactorPercentPerRankEdge * (rank - recipeLevel), MinimumFactorPercent, MaximumProgressFactorPercent);

    /// <summary>The percent multiplier on Subtlety actions: a penalty of 5% per level below the recipe's level, and no bonus above it.</summary>
    public static int QualityFactorPercent(int rank, int recipeLevel) =>
        Math.Clamp(
            100 + FactorPercentPerRankEdge * Math.Min(rank - recipeLevel, 0),
            MinimumFactorPercent,
            MaximumQualityFactorPercent);

    /// <summary>The Stability every action but Stabilize costs for a Peak intensity of 1 to 5.</summary>
    public static int ActionStability(int peakIntensity) =>
        ActionStabilityByIntensity[Math.Clamp(peakIntensity, 1, ActionStabilityByIntensity.Length) - 1];

    /// <summary>The Stability a Charge at Peak costs beyond the action cost.</summary>
    public static int PeakChargeStability(int peakIntensity) =>
        HeavyChargeStability + PeakStabilityPerIntensity * peakIntensity;

    /// <summary>The CP Stabilize costs for a recipe of this level.</summary>
    public static int StabilizeCp(int recipeLevel) =>
        Math.Max(MinimumStabilizeCp, Math.Max(recipeLevel, 0) * StabilizeCpNumerator / StabilizeCpDenominator);

    /// <summary>A base amount raised by the stat and the rank factor and rounded down, never below 1.</summary>
    public static int ScaleGain(int baseAmount, int statPoints, int factorPercent) =>
        Math.Max(1, baseAmount * (100 + BonusPercentPerStatPoint * statPoints) * factorPercent / 10000);
}
