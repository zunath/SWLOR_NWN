namespace SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

/// <summary>
/// Every starting value of the Reaction minigame, in one place for the simulator to tune. Espionage has no crafting gear, so its
/// Craftsmanship, Control and CP equivalents come from Espionage rank alone.
/// </summary>
public static class ReactionRules
{
    /// <summary>The maximum of Potency, Subtlety and Stability. Subtlety is the quality percentage.</summary>
    public const int MeterMaximum = 100;

    /// <summary>The Craftsmanship and Control a crafter of this rank is assumed to carry: 0.6 per rank, 30 at rank 50 (the best gear tier).</summary>
    public const float EquivalentStatPerRank = 0.6f;

    /// <summary>Each point of Craftsmanship or Control raises its actions by this share (30 points: +30%).</summary>
    public const float BonusPerStatPoint = 0.01f;

    /// <summary>Each Espionage rank above the recipe's level raises Potency actions by 5% (legacy rule), up to <see cref="MaximumProgressFactor"/>.</summary>
    public const float FactorPerRankEdge = 0.05f;

    public const float MinimumFactor = 0.5f;
    public const float MaximumProgressFactor = 1.25f;
    public const float MaximumQualityFactor = 1.0f;

    /// <summary>CP is 0.75 per rank; Espionage has no gear CP.</summary>
    public const float CpPerRank = 0.75f;

    /// <summary>The rank-25 CP passive. The simulator re-tunes it.</summary>
    public const int CpPassiveRank = 25;
    public const int CpPassive = 31;

    public const int MeasurePotency = 4;
    public const int ChargePotency = 12;
    public const int RefineSubtlety = 9;

    public const int PeakChargeMultiplier = 2;
    public const int SettlingRefineMultiplier = 2;

    /// <summary>Refine at Peak does this share of its usual Subtlety.</summary>
    public const float PeakRefineFactor = 0.5f;

    /// <summary>The Stability every action costs except Stabilize.</summary>
    public const int ActionStability = 2;

    /// <summary>The extra Stability a Charge costs in Rising or Settling.</summary>
    public const int HeavyChargeStability = 10;

    /// <summary>A Charge at Peak costs <see cref="HeavyChargeStability"/> plus this much per point of Peak intensity.</summary>
    public const int PeakStabilityPerIntensity = 4;

    public const int StabilizeRestore = 30;
    public const int StabilizeCp = 10;
    public const int DampenCp = 6;

    public static int EquivalentCraftsmanship(int rank) => (int)(Math.Max(rank, 0) * EquivalentStatPerRank);

    public static int EquivalentControl(int rank) => (int)(Math.Max(rank, 0) * EquivalentStatPerRank);

    /// <summary>Gear CP (none) plus 0.75 per rank, and the rank-25 passive from rank 25.</summary>
    public static int MaxCp(int rank)
    {
        rank = Math.Max(rank, 0);
        return (int)(rank * CpPerRank) + (rank >= CpPassiveRank ? CpPassive : 0);
    }

    /// <summary>The multiplier on Potency actions from rank against recipe level: 5% per level, from 0.5 up to 1.25.</summary>
    public static float ProgressFactor(int rank, int recipeLevel) =>
        Math.Clamp(1f + FactorPerRankEdge * (rank - recipeLevel), MinimumFactor, MaximumProgressFactor);

    /// <summary>The multiplier on Subtlety actions: a penalty of 5% per level below the recipe's level, and no bonus above it.</summary>
    public static float QualityFactor(int rank, int recipeLevel) =>
        Math.Clamp(1f + FactorPerRankEdge * Math.Min(rank - recipeLevel, 0), MinimumFactor, MaximumQualityFactor);

    /// <summary>The Stability a Charge at Peak costs beyond the action cost.</summary>
    public static int PeakChargeStability(int peakIntensity) =>
        HeavyChargeStability + PeakStabilityPerIntensity * peakIntensity;

    /// <summary>A base amount raised by the stat and the rank factor and rounded down, never below 1.</summary>
    public static int ScaleGain(int baseAmount, int statPoints, float factor) =>
        Math.Max(1, (int)Math.Floor(baseAmount * (1f + BonusPerStatPoint * statPoints) * factor));
}
