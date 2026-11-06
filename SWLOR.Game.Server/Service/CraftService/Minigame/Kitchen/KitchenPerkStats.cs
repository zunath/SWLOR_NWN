using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>
/// The numbers of a Kitchen dish that the cook's Chef perks change. The window sums them once from the cook's stat totals when the
/// dish starts and hands the record to <see cref="KitchenStats"/>, so the engine never tests a perk and a preview cannot change under
/// the player. The default changes nothing.
/// </summary>
/// <param name="PantryPushBonus">Points added to the push of each free pantry seasoning.</param>
/// <param name="PushSpreadReduction">Percentage points taken off the base spread of a push, before Control narrows it.</param>
/// <param name="SeasonedPalateOwned">True when the cook may switch Seasoned Palate on; the window shows its switch.</param>
/// <param name="SeasonedPalateOn">True when Seasoned Palate is switched on for this dish: every Add of a committed ingredient pushes half as far.</param>
/// <param name="SeasonedPalateSpreadPercent">Percent by which a switched-on Seasoned Palate narrows the spread, after Control.</param>
/// <param name="LowHeatDonenessPercent">Percent added to the Doneness a Simmer gives at Low heat.</param>
/// <param name="MediumDriftReductionPercent">Percent taken off the drift at Medium heat.</param>
/// <param name="HighDriftReductionPercent">Percent taken off the drift at High heat.</param>
/// <param name="ReducePercent">How far, in percent, Reduce moves the flavor toward the target's center; above 0 the dish has Reduce.</param>
/// <param name="ReduceCpReduction">CP Reduce costs less, to a minimum of 1 CP.</param>
/// <param name="PeakAromaChancePoints">Percentage points added to the chance each turn announces Peak Aroma.</param>
/// <param name="StirCpReduction">CP a Stir costs less, to a minimum of 1 CP.</param>
/// <param name="StirDonenessPercent">Percent of a Simmer's Doneness at the current heat that a Stir also gives.</param>
/// <param name="SignatureDish">True when the dish has Signature Dish, once.</param>
public sealed record KitchenPerkStats(
    int PantryPushBonus = 0,
    int PushSpreadReduction = 0,
    bool SeasonedPalateOwned = false,
    bool SeasonedPalateOn = false,
    int SeasonedPalateSpreadPercent = 0,
    int LowHeatDonenessPercent = 0,
    int MediumDriftReductionPercent = 0,
    int HighDriftReductionPercent = 0,
    int ReducePercent = 0,
    int ReduceCpReduction = 0,
    int PeakAromaChancePoints = 0,
    int StirCpReduction = 0,
    int StirDonenessPercent = 0,
    bool SignatureDish = false)
{
    /// <summary>No perks: the Kitchen as tuned.</summary>
    public static readonly KitchenPerkStats None = new();

    /// <summary>True when any number differs from the Kitchen as tuned.</summary>
    public bool HasAny => this != None;

    /// <summary>True when the dish has Reduce.</summary>
    public bool HasReduce => ReducePercent > 0;

    /// <summary>
    /// Reads the record from a cook's stat totals, one lookup per stat. Seasoned Palate is a choice the cook makes for each dish, so
    /// it counts only when the cook owns it and <paramref name="seasonedPalateOn"/> is true.
    /// </summary>
    /// <param name="total">The cook's total for a stat, such as <c>Stat.GetStatAdjustment</c> for the cooking player.</param>
    /// <param name="seasonedPalateOn">Whether the cook switched Seasoned Palate on for this dish.</param>
    public static KitchenPerkStats FromStatTotals(Func<StatType, int> total, bool seasonedPalateOn = false)
    {
        if (total == null)
            throw new ArgumentNullException(nameof(total));

        var owned = total(StatType.KitchenSeasonedPalate) > 0;
        var on = owned && seasonedPalateOn;
        return new KitchenPerkStats(
            total(StatType.KitchenPantryPush),
            total(StatType.KitchenPushSpreadReduction),
            owned,
            on,
            on ? total(StatType.KitchenSeasonedPalateSpreadPercent) : 0,
            total(StatType.KitchenLowHeatDonenessPercent),
            total(StatType.KitchenMediumDriftReductionPercent),
            total(StatType.KitchenHighHeatDriftReductionPercent),
            total(StatType.KitchenReducePercent),
            total(StatType.KitchenReduceCpReduction),
            total(StatType.KitchenPeakAromaChancePoints),
            total(StatType.KitchenStirCpReduction),
            total(StatType.KitchenStirDonenessPercent),
            total(StatType.KitchenSignatureDish) > 0);
    }
}
