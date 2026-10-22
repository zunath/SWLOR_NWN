using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

/// <summary>
/// The Plasma Forge's numbers and the pure formulas behind them. Meters are whole numbers: Shape and Finish are 1000 at an even
/// split, so one point is a tenth of a percent. Every value here is a starting value for the simulator to tune.
/// </summary>
public static class ForgeRules
{
    public const int MeterScale = 1000;
    public const int EvenShapeShare = 50;
    public const int MaxThermal = 100;
    public const int MaxIntegrity = 100;

    public const float CpPerRank = 0.75f;
    public const int RankPassiveRank = 25;
    public const int RankPassiveCp = 31;

    /// <summary>The Smithery rank that unlocks Hydro-Press.</summary>
    public const int HydroPressRank = 0;

    /// <summary>The Smithery rank that unlocks Vibro-Hone.</summary>
    public const int VibroHoneRank = 0;

    /// <summary>The Smithery rank that unlocks Ramp Plasma.</summary>
    public const int RampPlasmaRank = 0;

    /// <summary>The Smithery rank that unlocks Rebond.</summary>
    public const int RebondRank = 10;

    /// <summary>The Smithery rank that unlocks Cryo-Quench.</summary>
    public const int CryoQuenchRank = 20;

    /// <summary>Output rises or falls this much for each rank above or below the recipe's level.</summary>
    public const double OutputPerRankStep = 0.05;
    public const double MinimumRankFactor = 0.85;
    public const double MaximumRankFactor = 1.25;

    /// <summary>Each point of Craftsmanship adds this much to Shape gains, and each point of Control to Finish gains.</summary>
    public const double OutputPerGearPoint = 0.01;

    public const int RampThermal = 25;
    public const int RampCp = 5;
    public const int RampIntegrity = 2;

    public const int PressBaseShape = 170;
    public const double PressRangeLowMultiplier = 0.70;
    public const double PressRangeHighMultiplier = 1.20;
    public const double PressAboveBonus = 0.40;
    public const int PressAboveSpan = 25;
    public const double PressBelowMultiplier = 0.25;
    public const int PressIntegrityInRange = 5;
    public const int PressIntegrityBelowBase = 7;
    public const int PressIntegrityBelowStep = 4;
    public const int PressIntegrityAboveBase = 10;
    public const int PressIntegrityAboveStep = 3;

    public const int HoneBaseFinish = 140;
    public const double HoneHotEdgeMultiplier = 0.60;
    public const double HoneOutsideMultiplier = 0.35;
    public const int HoneIntegrity = 4;

    public const int RebondIntegrity = 25;
    public const int RebondThermal = 12;
    public const int RebondCp = 8;

    public const int QuenchBaseFinish = 360;
    public const double QuenchEdgeMultiplier = 0.50;
    public const double QuenchOutsideMultiplier = 0.15;
    public const int QuenchIntegrity = 4;

    /// <summary>Cryo-Quench needs the piece at least this far shaped, as a percentage of the Shape meter.</summary>
    public const int QuenchMinimumShapePercent = 80;

    /// <summary>After a quench, Vibro-Hone gives these flat amounts whatever the Thermal: the set piece is trimmed to size.</summary>
    public const int PostQuenchHoneFinish = 84;
    public const int PostQuenchHoneShape = 72;

    /// <summary>
    /// The factor on every Smithery craft's base XP, success and failure alike. The old window's early crafts take 6 actions at 6 seconds,
    /// so the Forge's 10-20 actions would otherwise pay less XP per hour at the lowest ranks (the XP-rate gate).
    /// </summary>
    public const double XpMultiplier = 1.55;

    public const string StableContainmentId = "stable_containment";
    public const int StableContainmentChancePercent = 35;
    public const int StableContainmentBonusPercent = 50;

    /// <summary>Craft points: gear CP plus 0.75 per rank, and the rank-25 passive.</summary>
    public static int MaxCp(int gearCp, int rank) =>
        (int)(gearCp + rank * CpPerRank) + (rank >= RankPassiveRank ? RankPassiveCp : 0);

    /// <summary>The output factor for a crafter above or below the recipe's level.</summary>
    public static double RankFactor(int rank, int recipeLevel) =>
        Math.Clamp(1.0 + OutputPerRankStep * (rank - recipeLevel), MinimumRankFactor, MaximumRankFactor);

    public static int ShapeMax(int shapeShare, int enhancementShapePercent) =>
        (int)Math.Round(MeterScale * (shapeShare / (double)EvenShapeShare) * (1.0 + Math.Max(0, enhancementShapePercent) / 100.0),
            MidpointRounding.AwayFromZero);

    public static int FinishMax(int shapeShare) =>
        (int)Math.Round(MeterScale * ((100 - shapeShare) / (double)EvenShapeShare), MidpointRounding.AwayFromZero);

    /// <summary>
    /// How much more Shape the recipe needs for the enhancements' progress penalty. The penalty is in the original crafting
    /// window's progress units, so it is compared with that window's progress for the recipe's level.
    /// </summary>
    public static int EnhancementShapePercent(int progressPenalty, int recipeLevel)
    {
        if (progressPenalty <= 0)
            return 0;

        var progress = new RecipeLevelChart().GetByLevel(Math.Clamp(recipeLevel, 1, 80)).Progress;
        return (int)Math.Round(100.0 * progressPenalty / progress, MidpointRounding.AwayFromZero);
    }

    public static ForgeThermalZone ZoneOf(int thermal, int low, int high)
    {
        if (thermal < low)
            return ForgeThermalZone.Below;

        return thermal > high ? ForgeThermalZone.Above : ForgeThermalZone.InRange;
    }

    /// <summary>Where Thermal sits inside the range: 0 at its low edge and 1 at its high edge.</summary>
    public static double PositionInRange(int thermal, int low, int high) =>
        high <= low ? 0.5 : Math.Clamp((thermal - low) / (double)(high - low), 0.0, 1.0);

    /// <summary>Hydro-Press: little below the range, rising across it, and more above it.</summary>
    public static double PressMultiplier(int thermal, int low, int high)
    {
        switch (ZoneOf(thermal, low, high))
        {
            case ForgeThermalZone.Below:
                return PressBelowMultiplier;
            case ForgeThermalZone.Above:
                return PressRangeHighMultiplier + PressAboveBonus * Math.Min(1.0, (thermal - high) / (double)PressAboveSpan);
            default:
                return PressRangeLowMultiplier +
                       (PressRangeHighMultiplier - PressRangeLowMultiplier) * PositionInRange(thermal, low, high);
        }
    }

    /// <summary>Integrity Hydro-Press costs: small inside the range, and more the further it is from it.</summary>
    public static int PressIntegrityCost(int thermal, int low, int high)
    {
        switch (ZoneOf(thermal, low, high))
        {
            case ForgeThermalZone.Below:
                return PressIntegrityBelowBase + CeilingDivide(low - thermal, PressIntegrityBelowStep);
            case ForgeThermalZone.Above:
                return PressIntegrityAboveBase + CeilingDivide(thermal - high, PressIntegrityAboveStep);
            default:
                return PressIntegrityInRange;
        }
    }

    /// <summary>Vibro-Hone: best in the cool half of the range, falling toward the hot edge, and weak outside it.</summary>
    public static double HoneMultiplier(int thermal, int low, int high)
    {
        if (ZoneOf(thermal, low, high) != ForgeThermalZone.InRange)
            return HoneOutsideMultiplier;

        var position = PositionInRange(thermal, low, high);
        if (position <= 0.5)
            return 1.0;

        return 1.0 - (1.0 - HoneHotEdgeMultiplier) * ((position - 0.5) / 0.5);
    }

    /// <summary>Cryo-Quench: large inside the range and larger near its center.</summary>
    public static double QuenchMultiplier(int thermal, int low, int high)
    {
        if (ZoneOf(thermal, low, high) != ForgeThermalZone.InRange)
            return QuenchOutsideMultiplier;

        var half = (high - low) / 2.0;
        var distance = Math.Abs(thermal - (low + high) / 2.0);
        return 1.0 - (1.0 - QuenchEdgeMultiplier) * Math.Min(1.0, half <= 0 ? 0.0 : distance / half);
    }

    /// <summary>The Smithery rank an action unlocks at.</summary>
    public static int UnlockRank(string actionId) => actionId switch
    {
        ForgeActionIds.HydroPress => HydroPressRank,
        ForgeActionIds.VibroHone => VibroHoneRank,
        ForgeActionIds.RampPlasma => RampPlasmaRank,
        ForgeActionIds.Rebond => RebondRank,
        ForgeActionIds.CryoQuench => CryoQuenchRank,
        _ => throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId))
    };

    /// <summary>Every action in the order the window lists them.</summary>
    public static IReadOnlyList<string> ActionIds { get; } = new[]
    {
        ForgeActionIds.RampPlasma,
        ForgeActionIds.HydroPress,
        ForgeActionIds.VibroHone,
        ForgeActionIds.Rebond,
        ForgeActionIds.CryoQuench
    };

    private static int CeilingDivide(int value, int step) => (value + step - 1) / step;
}
