namespace SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

/// <summary>
/// The magnitudes of the Overclocker and Technician perk lines and of the two styles. The perk definitions grant these and print
/// them in their descriptions, so a tuning pass changes this file only.
/// </summary>
public static class CircuitPerkRules
{
    // Overclocker line.
    public const int ConductiveWiringLoadBoostRank1 = 1;
    public const int ConductiveWiringLoadBoostRank2 = 2;
    public const int CapacitorBankDischargePercent = 5;
    public const int HeatSinksLineRank1 = 1;
    public const int HeatSinksLineRank2 = 2;
    public const int OverdriveBoostExtraLoad = 1;
    public const int OverdriveBoostCpReduction = 1;
    public const int ShockAbsorbersReductionPercent = 33;

    // Technician line.
    public const int InsulatedTracesReductionRank1 = 1;
    public const int InsulatedTracesReductionRank2 = 2;
    public const int FineCalibrationPercentRank1 = 10;
    public const int FineCalibrationPercentRank2 = 25;
    public const int DiagnosticsExtraForecastTurns = 1;
    public const int SignalLockChancePercent = 10;
    public const int PrecisionPatchExtraRestore = 10;
    public const int PrecisionPatchCpReduction = 2;

    // Styles. Overclock trades safety for output; Safe Mode trades output for safety.
    public const int OverclockExtraLoad = 1;
    public const int OverclockLoadBoostPercent = 1;
    public const int SafeModeOverloadGrace = 2;
    public const int SafeModeLoadBoostPercent = -3;

    /// <summary>"percentage point" for 1 and "percentage points" for any other amount, for the descriptions.</summary>
    public static string PointWord(int amount) => amount == 1 ? "percentage point" : "percentage points";
}
