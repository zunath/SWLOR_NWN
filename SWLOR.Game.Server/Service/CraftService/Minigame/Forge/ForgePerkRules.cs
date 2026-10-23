namespace SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

/// <summary>
/// The magnitudes of the Forgemaster and Artisan perk lines and of the two Smithery styles. The perk definitions grant these and
/// print them in their descriptions, so a tuning pass changes this file only. Every value is a starting value for the simulator
/// to tune; the Design Bible script reads this file too.
/// </summary>
public static class ForgePerkRules
{
    /// <summary>The Smithery rank at which each line offers its style.</summary>
    public const int StyleRank = 20;

    // Forgemaster line.
    public const int PlasmaEfficiencyCpReduction = 2;
    public const int HeatRetentionRank1 = 1;
    public const int HeatRetentionRank2 = 2;
    public const int OverdrivePressPercent = 75;
    public const int OverdrivePressCp = 12;
    public const int OverdrivePressIntegrityMultiplier = 2;
    public const int HammerRhythmPercent = 8;
    public const int HammerRhythmMaxStacks = 3;
    public const int ScorchGuardReductionPercent = 25;
    public const int SurgePlasmaThermal = 44;
    public const double SurgePlasmaCpMultiplier = 1.5;
    public const int FastRebondThermalReduction = 6;
    public const int FastRebondCpReduction = 3;
    public const int RedLineCharges = 1;
    public const int RedLineShapeMultiplier = 2;

    // Artisan line.
    public const int SteadyHandsIntegrityReduction = 2;
    public const int CleanContainmentRank1 = 10;
    public const int CleanContainmentRank2 = 20;
    public const int MicroHonePercent = 80;
    public const int MicroHoneCp = 4;
    public const int MicroHoneIntegrity = 6;
    public const int SteadyQuenchEdgePercent = 40;
    public const int CoolHandHotEdgePercent = 20;
    public const int TemperHoldActions = 2;
    public const int TemperHoldCp = 10;
    public const int ContainmentFocusPercent = 25;
    public const int PerfectTemperPercent = 50;
    public const int PerfectTemperWindow = 2;

    // Styles. Hot Forging trades Finish for Shape, and Cold Working trades Shape for Finish.
    public const int HotForgingRangeShift = 10;
    public const int HotForgingPressPercent = 25;
    public const int HotForgingHonePercent = -25;
    public const int ColdWorkingRangeShift = -10;
    public const int ColdWorkingHonePercent = 25;
    public const int ColdWorkingPressPercent = -25;

    /// <summary>"percentage point" for 1 and "percentage points" for any other amount, for the descriptions.</summary>
    public static string PointWord(int amount) => amount == 1 ? "percentage point" : "percentage points";
}
