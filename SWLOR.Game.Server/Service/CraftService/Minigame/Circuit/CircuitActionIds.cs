namespace SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

/// <summary>The ids of the Circuit actions and of its telegraphed opportunity.</summary>
public static class CircuitActionIds
{
    public const string Wire = "wire";
    public const string Tune = "tune";
    public const string Boost = "boost";
    public const string DischargeAssembly = "discharge_assembly";
    public const string DischargeCalibration = "discharge_calibration";
    public const string Ground = "ground";
    public const string Patch = "patch";

    // Actions the Overclocker and Technician perks add.
    public const string DischargeBoth = "discharge_both";
    public const string MicroTune = "micro_tune";

    public const string CleanSignal = "clean_signal";
}
