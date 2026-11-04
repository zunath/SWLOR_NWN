using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;

/// <summary>
/// The numbers of a Fabricator craft that the crafter's Builder perks change. The window sums them once from the crafter's stat
/// totals when the craft starts and hands the record to <see cref="FabricatorSetup"/>, so the engine never tests a perk and a
/// preview cannot change under the player. The default changes nothing.
/// </summary>
/// <param name="RunPowerPercent">Percent added to the progress a Run makes (a style's cost is negative).</param>
/// <param name="PrecisionPowerPercent">Percent added to the quality a Precision Pass gains (a style's cost is negative).</param>
/// <param name="WearReduction">Machine Condition a Run or Precision Pass wears less, to a minimum of 1.</param>
/// <param name="RetoolCpReductionPercent">Percent off a Retool's CP, to a minimum of 1 CP.</param>
/// <param name="ServiceRestoreBonus">Machine Condition a Service restores beyond its base amount.</param>
/// <param name="ServiceCpReduction">CP a Service costs less, to a minimum of 1 CP.</param>
/// <param name="DualFeedUnlocked">True when the Fabricator offers Dual Feed.</param>
/// <param name="PrimeWindowBonusActions">Extra actions a Prime Feedstock waits for its head.</param>
/// <param name="PrimeBonusPercentBonus">Percentage points added to the bonus a Prime Feedstock gives.</param>
/// <param name="AssembleCapBonusPoints">Points added to how far Assemble lets the quality rise above the weakest sub-assembly.</param>
public sealed record FabricatorPerkStats(
    int RunPowerPercent = 0,
    int PrecisionPowerPercent = 0,
    int WearReduction = 0,
    int RetoolCpReductionPercent = 0,
    int ServiceRestoreBonus = 0,
    int ServiceCpReduction = 0,
    bool DualFeedUnlocked = false,
    int PrimeWindowBonusActions = 0,
    int PrimeBonusPercentBonus = 0,
    int AssembleCapBonusPoints = 0)
{
    /// <summary>No perks: the Fabricator as tuned.</summary>
    public static readonly FabricatorPerkStats None = new();

    /// <summary>True when any number differs from the Fabricator as tuned.</summary>
    public bool HasAny => this != None;

    /// <summary>Reads the record from a crafter's stat totals, one lookup per stat.</summary>
    /// <param name="total">The crafter's total for a stat, such as <c>Stat.GetStatAdjustment</c> for the crafting player.</param>
    public static FabricatorPerkStats FromStatTotals(Func<StatType, int> total)
    {
        if (total == null)
            throw new ArgumentNullException(nameof(total));

        return new FabricatorPerkStats(
            total(StatType.FabricatorRunPowerPercent),
            total(StatType.FabricatorPrecisionPowerPercent),
            total(StatType.FabricatorWearReduction),
            total(StatType.FabricatorRetoolCpReductionPercent),
            total(StatType.FabricatorServiceRestore),
            total(StatType.FabricatorServiceCpReduction),
            total(StatType.FabricatorDualFeedUnlocked) > 0,
            total(StatType.FabricatorPrimeWindowActions),
            total(StatType.FabricatorPrimeBonusPercent),
            total(StatType.FabricatorAssembleCapPoints));
    }
}
