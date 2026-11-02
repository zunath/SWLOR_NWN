using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

/// <summary>
/// What the crafter's Engineering perks and active style change in the Circuit, as the stat totals they grant. The window reads
/// them once when the craft starts and the engine reads only this record, so previews and results stay pure. Zero (or false)
/// changes nothing.
/// </summary>
public sealed record CircuitPerkStats
{
    public static CircuitPerkStats None { get; } = new();

    /// <summary>Percentage points of output each Load point adds, on top of the recipe's. A style may make it negative.</summary>
    public int LoadBoostPercent { get; init; }

    public int DischargePowerPercent { get; init; }

    /// <summary>Points added to the recipe's overload line.</summary>
    public int OverloadLine { get; init; }

    /// <summary>Load a Wire or Tune adds beyond its usual amount.</summary>
    public int ExtraLoadPerBuildAction { get; init; }

    public bool CascadeDischarge { get; init; }

    public int BoostLoad { get; init; }

    public int BoostCpReduction { get; init; }

    public int OverloadIntegrityReductionPercent { get; init; }

    public bool Surge { get; init; }

    /// <summary>Integrity that Wire, Tune, Boost, Discharge and their variants cost less, to a minimum of 1.</summary>
    public int ActionIntegrityReduction { get; init; }

    public int TunePowerPercent { get; init; }

    /// <summary>Actions beyond the next at which a Clean Signal is announced.</summary>
    public int ForecastTurns { get; init; }

    /// <summary>Points past the overload line at which an action still cannot overload.</summary>
    public int OverloadGrace { get; init; }

    public bool MicroTune { get; init; }

    public int SignalChancePercent { get; init; }

    public int PatchRestore { get; init; }

    public int PatchCpReduction { get; init; }

    public bool Failsafe { get; init; }

    /// <summary>The perk stats for a crafter whose stat totals <paramref name="read"/> returns.</summary>
    public static CircuitPerkStats FromStats(Func<StatType, int> read)
    {
        if (read == null)
            throw new ArgumentNullException(nameof(read));

        return new CircuitPerkStats
        {
            LoadBoostPercent = read(StatType.CircuitLoadBoostPercent),
            DischargePowerPercent = read(StatType.CircuitDischargePowerPercent),
            OverloadLine = read(StatType.CircuitOverloadLine),
            ExtraLoadPerBuildAction = read(StatType.CircuitExtraLoadPerBuildAction),
            CascadeDischarge = read(StatType.CircuitCascadeDischargeUnlocked) > 0,
            BoostLoad = read(StatType.CircuitBoostLoad),
            BoostCpReduction = read(StatType.CircuitBoostCpReduction),
            OverloadIntegrityReductionPercent = read(StatType.CircuitOverloadIntegrityReductionPercent),
            Surge = read(StatType.CircuitSurgeUnlocked) > 0,
            ActionIntegrityReduction = read(StatType.CircuitActionIntegrityReduction),
            TunePowerPercent = read(StatType.CircuitTunePowerPercent),
            ForecastTurns = read(StatType.CircuitForecastTurns),
            OverloadGrace = read(StatType.CircuitOverloadGrace),
            MicroTune = read(StatType.CircuitMicroTuneUnlocked) > 0,
            SignalChancePercent = read(StatType.CircuitSignalChancePercent),
            PatchRestore = read(StatType.CircuitPatchRestore),
            PatchCpReduction = read(StatType.CircuitPatchCpReduction),
            Failsafe = read(StatType.CircuitFailsafeUnlocked) > 0
        };
    }
}
