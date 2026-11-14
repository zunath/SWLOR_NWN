namespace SWLOR.Game.Server.Domain.Crafting.Minigame.Circuit;

/// <summary>
/// The magnitudes of the Overclocker and Technician perk lines and of the two styles. The perk definitions grant these and print
/// them in their descriptions, so a tuning pass changes this file only.
/// </summary>
public static class CircuitPerkRules
{
    // Overclocker line.
    public const int ConductiveWiringLoadBoost = 1;
    public const int ConductiveWiringDischargePercent = 2;
    public const int CapacitorBankDischargePercent = 1;
    public const int HeatSinksLineRank1 = 1;
    public const int HeatSinksLineRank2 = 2;
    public const int OverdriveBoostExtraLoad = 1;
    public const int OverdriveBoostCpReduction = 1;
    public const int ShockAbsorbersReductionPercent = 15;

    // Technician line.
    public const int InsulatedTracesIntegrityReduction = 1;
    public const int InsulatedTracesPatchRestore = 3;
    public const int FineCalibrationPercentRank1 = 2;
    public const int FineCalibrationPercentRank2 = 4;
    public const int DiagnosticsExtraForecastTurns = 1;
    public const int SignalLockChancePercent = 4;
    public const int PrecisionPatchExtraRestore = 2;
    public const int PrecisionPatchCpReduction = 1;

    // Styles. Overclock trades safety for output; Safe Mode trades output for safety.
    public const int OverclockExtraLoad = 1;
    public const int SafeModeOverloadGrace = 3;
    public const int SafeModeLoadBoostPercent = -1;

    /// <summary>"percentage point" for 1 and "percentage points" for any other amount, for the descriptions.</summary>
    public static string PointWord(int amount) => amount == 1 ? "percentage point" : "percentage points";
}
