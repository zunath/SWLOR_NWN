using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

/// <summary>
/// What the crafter's Smithery perks and active style change in the Plasma Forge, as the stat totals they grant. The window reads
/// them once when the craft starts and the engine reads only this record, so previews and results stay pure. Zero changes nothing;
/// an action a perk adds is offered only while its stat is above 0.
/// </summary>
public sealed record ForgePerkStats
{
    public static ForgePerkStats None { get; } = new();

    /// <summary>Thermal points the working range moves: positive up, negative down.</summary>
    public int RangeShift { get; init; }

    /// <summary>Percent added to every Hydro-Press Shape gain, the set piece's trim included.</summary>
    public int PressOutputPercent { get; init; }

    /// <summary>Percent added to every Vibro-Hone Finish gain.</summary>
    public int HoneOutputPercent { get; init; }

    /// <summary>CP Ramp Plasma costs less, to a minimum of 1.</summary>
    public int RampCpReduction { get; init; }

    /// <summary>Thermal the piece loses less after every action, to a minimum of 1 lost.</summary>
    public int CoolingReduction { get; init; }

    /// <summary>Above 0 the Forge offers Overdrive Press, whose Shape gain is this many percent more than a Hydro-Press at the same Thermal.</summary>
    public int OverdrivePressPercent { get; init; }

    /// <summary>Percent of Shape each consecutive Hydro-Press before a Hydro-Press adds to it, up to <see cref="ForgePerkRules.HammerRhythmMaxStacks"/> presses.</summary>
    public int PressStreakPercent { get; init; }

    /// <summary>Percent of a Hydro-Press's Integrity cost above the working range that it does not crack.</summary>
    public int PressOverheatIntegrityReductionPercent { get; init; }

    /// <summary>Above 0 the Forge offers Surge Plasma, which raises Thermal by this much.</summary>
    public int SurgePlasmaThermal { get; init; }

    /// <summary>Thermal Rebond costs less.</summary>
    public int RebondThermalReduction { get; init; }

    /// <summary>CP Rebond costs less, to a minimum of 1.</summary>
    public int RebondCpReduction { get; init; }

    /// <summary>The number of Red Line Presses the crafter may make in one craft.</summary>
    public int RedLineCharges { get; init; }

    /// <summary>Integrity Vibro-Hone costs less, to a minimum of 1.</summary>
    public int HoneIntegrityReduction { get; init; }

    /// <summary>Percentage points added to the chance that Stable Containment is announced.</summary>
    public int ContainmentChancePercent { get; init; }

    /// <summary>Above 0 the Forge offers Micro-Hone, which gives this percent of a Vibro-Hone's best Finish at any Thermal.</summary>
    public int MicroHonePercent { get; init; }

    /// <summary>Percentage points of its center bonus that Cryo-Quench keeps at the edge of the working range, beyond the usual.</summary>
    public int QuenchEdgePercent { get; init; }

    /// <summary>Percentage points of its best Finish that Vibro-Hone keeps at the hot edge of the working range, beyond the usual.</summary>
    public int HoneHotEdgePercent { get; init; }

    /// <summary>Above 0 the Forge offers Temper Hold, and this many actions after it do not cool the piece.</summary>
    public int TemperHoldActions { get; init; }

    /// <summary>Percentage points added to the bonus Stable Containment gives a Vibro-Hone.</summary>
    public int ContainmentHonePercent { get; init; }

    /// <summary>Percent added to a Cryo-Quench's Finish when Thermal is within <see cref="ForgePerkRules.PerfectTemperWindow"/> of the working range's center.</summary>
    public int PerfectTemperPercent { get; init; }

    /// <summary>The perk stats for a crafter whose stat totals <paramref name="read"/> returns.</summary>
    public static ForgePerkStats FromStats(Func<StatType, int> read)
    {
        if (read == null)
            throw new ArgumentNullException(nameof(read));

        return new ForgePerkStats
        {
            RangeShift = read(StatType.ForgeRangeShift),
            PressOutputPercent = read(StatType.ForgePressOutputPercent),
            HoneOutputPercent = read(StatType.ForgeHoneOutputPercent),
            RampCpReduction = read(StatType.ForgeRampCpReduction),
            CoolingReduction = read(StatType.ForgeCoolingReduction),
            OverdrivePressPercent = read(StatType.ForgeOverdrivePressPercent),
            PressStreakPercent = read(StatType.ForgePressStreakPercent),
            PressOverheatIntegrityReductionPercent = read(StatType.ForgePressOverheatIntegrityReductionPercent),
            SurgePlasmaThermal = read(StatType.ForgeSurgePlasmaThermal),
            RebondThermalReduction = read(StatType.ForgeRebondThermalReduction),
            RebondCpReduction = read(StatType.ForgeRebondCpReduction),
            RedLineCharges = read(StatType.ForgeRedLineCharges),
            HoneIntegrityReduction = read(StatType.ForgeHoneIntegrityReduction),
            ContainmentChancePercent = read(StatType.ForgeContainmentChancePercent),
            MicroHonePercent = read(StatType.ForgeMicroHonePercent),
            QuenchEdgePercent = read(StatType.ForgeQuenchEdgePercent),
            HoneHotEdgePercent = read(StatType.ForgeHoneHotEdgePercent),
            TemperHoldActions = read(StatType.ForgeTemperHoldActions),
            ContainmentHonePercent = read(StatType.ForgeContainmentHonePercent),
            PerfectTemperPercent = read(StatType.ForgePerfectTemperPercent)
        };
    }
}
