namespace SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

/// <summary>
/// Everything about a craft that is fixed before the first action: the recipe's parameters and level, the crafter's Smithery rank,
/// gear and perk stats, and the Shape the chosen enhancements add. The engine holds it as immutable configuration.
/// </summary>
/// <param name="Parameters">The recipe's Plasma Forge parameters.</param>
/// <param name="RecipeLevel">The recipe's level.</param>
/// <param name="Rank">The crafter's Smithery rank.</param>
/// <param name="Craftsmanship">The crafter's Smithery Craftsmanship, which powers Shape gains.</param>
/// <param name="Control">The crafter's Smithery Control, which powers Finish gains.</param>
/// <param name="GearCp">The CP from the crafter's gear.</param>
/// <param name="EnhancementShapePercent">How much more Shape the selected enhancements require, as a percentage.</param>
/// <param name="Perks">What the crafter's Smithery perks and active style change; null plays without them.</param>
public sealed record ForgeSetup(
    ForgeParameters Parameters,
    int RecipeLevel,
    int Rank,
    int Craftsmanship,
    int Control,
    int GearCp,
    int EnhancementShapePercent = 0,
    ForgePerkStats Perks = null)
{
    /// <summary>What the crafter's perks and style change, captured when the craft starts.</summary>
    public ForgePerkStats Perks { get; init; } = Perks ?? ForgePerkStats.None;

    /// <summary>
    /// How far the style moves the working range, in Thermal points. The whole range moves together and stays inside 0 to 100, so a
    /// range at the edge of the meter moves less rather than narrowing.
    /// </summary>
    public int RangeShift =>
        Math.Clamp(Perks.RangeShift, -Parameters.RangeLow, ForgeRules.MaxThermal - Parameters.RangeHigh);

    /// <summary>The lowest Thermal inside the working range, with the style's shift.</summary>
    public int RangeLow => Parameters.RangeLow + RangeShift;

    /// <summary>The highest Thermal inside the working range, with the style's shift.</summary>
    public int RangeHigh => Parameters.RangeHigh + RangeShift;

    public int ShapeMax => ForgeRules.ShapeMax(Parameters.ShapeShare, EnhancementShapePercent);

    public int FinishMax => ForgeRules.FinishMax(Parameters.ShapeShare);

    public int MaxCp => ForgeRules.MaxCp(GearCp, Rank);

    /// <summary>The factor on every Shape gain: the crafter's rank against the recipe's level and the Craftsmanship of their gear.</summary>
    public double ShapeScale =>
        ForgeRules.RankFactor(Rank, RecipeLevel) * (1.0 + Craftsmanship * ForgeRules.OutputPerGearPoint);

    /// <summary>The factor on every Finish gain: the crafter's rank against the recipe's level and the Control of their gear.</summary>
    public double FinishScale =>
        ForgeRules.RankFactor(Rank, RecipeLevel) * (1.0 + Control * ForgeRules.OutputPerGearPoint);

    /// <summary>The Thermal the piece loses after every action: the recipe's cooling rate less the perks', never below 1 unless the recipe's own is lower.</summary>
    public int CoolingRate =>
        Perks.CoolingReduction <= 0
            ? Parameters.CoolingRate
            : Math.Max(Math.Min(Parameters.CoolingRate, 1), Parameters.CoolingRate - Perks.CoolingReduction);

    /// <summary>The CP Ramp Plasma costs.</summary>
    public int RampCp => Math.Max(1, ForgeRules.RampCp(RecipeLevel) - Math.Max(0, Perks.RampCpReduction));

    /// <summary>The CP Surge Plasma costs: one and a half Ramp Plasmas.</summary>
    public int SurgeCp => Math.Max(1, (int)Math.Round(RampCp * ForgePerkRules.SurgePlasmaCpMultiplier, MidpointRounding.AwayFromZero));

    /// <summary>The Thermal Surge Plasma raises.</summary>
    public int SurgeThermal => Math.Max(0, Perks.SurgePlasmaThermal);

    /// <summary>The CP Rebond costs.</summary>
    public int RebondCp => Math.Max(1, ForgeRules.RebondCp(RecipeLevel) - Math.Max(0, Perks.RebondCpReduction));

    /// <summary>The Thermal Rebond takes from the piece.</summary>
    public int RebondThermal => Math.Max(0, ForgeRules.RebondThermal - Math.Max(0, Perks.RebondThermalReduction));

    /// <summary>The Integrity Vibro-Hone costs.</summary>
    public int HoneIntegrity => Math.Max(1, ForgeRules.HoneIntegrity - Math.Max(0, Perks.HoneIntegrityReduction));

    /// <summary>The Integrity a Hydro-Press costs at the given Thermal: Scorch Guard lowers what a press above the range cracks.</summary>
    public int PressIntegrity(int thermal)
    {
        var cost = ForgeRules.PressIntegrityCost(thermal, RangeLow, RangeHigh);
        if (Perks.PressOverheatIntegrityReductionPercent <= 0 ||
            ForgeRules.ZoneOf(thermal, RangeLow, RangeHigh) != ForgeThermalZone.Above)
        {
            return cost;
        }

        var kept = 100 - Math.Clamp(Perks.PressOverheatIntegrityReductionPercent, 0, 100);
        return Math.Max(1, (int)Math.Round(cost * kept / 100.0, MidpointRounding.AwayFromZero));
    }

    /// <summary>The Integrity an Overdrive Press costs at the given Thermal.</summary>
    public int OverdriveIntegrity(int thermal) => PressIntegrity(thermal) * ForgePerkRules.OverdrivePressIntegrityMultiplier;

    /// <summary>The chance, in percent, that Stable Containment is announced for the next action.</summary>
    public int ContainmentChancePercent =>
        Math.Clamp(ForgeRules.StableContainmentChancePercent + Perks.ContainmentChancePercent, 0, 100);

    /// <summary>The bonus Stable Containment gives a Hydro-Press, in percent.</summary>
    public int ContainmentPressPercent => ForgeRules.StableContainmentBonusPercent;

    /// <summary>The bonus Stable Containment gives a Vibro-Hone, in percent.</summary>
    public int ContainmentHonePercent => ForgeRules.StableContainmentBonusPercent + Math.Max(0, Perks.ContainmentHonePercent);

    /// <summary>The share of its best Finish a Vibro-Hone keeps at the hot edge of the range.</summary>
    public double HoneHotEdgeMultiplier =>
        Math.Min(1.0, ForgeRules.HoneHotEdgeMultiplier + Math.Max(0, Perks.HoneHotEdgePercent) / 100.0);

    /// <summary>The share of its center bonus a Cryo-Quench keeps at the edge of the range.</summary>
    public double QuenchEdgeMultiplier =>
        Math.Min(1.0, ForgeRules.QuenchEdgeMultiplier + Math.Max(0, Perks.QuenchEdgePercent) / 100.0);

    /// <summary>The factor the style and Hammer Rhythm put on a Hydro-Press's Shape, with <paramref name="streak"/> Hydro-Presses just before it.</summary>
    public double PressFactor(int streak)
    {
        var stacks = Math.Clamp(streak, 0, ForgePerkRules.HammerRhythmMaxStacks);
        return Math.Max(MinimumOutputFactor, 1.0 + (Perks.PressOutputPercent + stacks * Math.Max(0, Perks.PressStreakPercent)) / 100.0);
    }

    /// <summary>The factor the style puts on a Vibro-Hone's Finish.</summary>
    public double HoneFactor => Math.Max(MinimumOutputFactor, 1.0 + Perks.HoneOutputPercent / 100.0);

    /// <summary>A style's penalty and the perks' bonuses add before they scale a gain; the factor never drops below this.</summary>
    private const double MinimumOutputFactor = 0.1;
}
