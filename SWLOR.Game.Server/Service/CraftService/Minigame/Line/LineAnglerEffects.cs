using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Line;

/// <summary>
/// The adjustments an angler's perks make to the Line, every one zero until a perk grants it. The engine reads these and never
/// checks a perk by name; the perk line that sets them is in <c>design/testing/crafting-revamp/agriculture-angler-perks.md</c>.
/// </summary>
/// <param name="TensionCapBonus">Added to the Tension cap.</param>
/// <param name="ReelDistanceBonus">Added to the Distance a Reel closes.</param>
/// <param name="ReelTensionBonus">Added to the Tension a Reel adds.</param>
/// <param name="GiveLineDistanceReduction">Taken off the Distance Give Line lets out.</param>
/// <param name="SetHookDrainBonus">Added to the Stamina Set Hook drains.</param>
/// <param name="HoldDrainBonus">Added to the Stamina Hold drains.</param>
/// <param name="ForecastDepth">How many of the fish's next moves the window shows: 1, or 2.</param>
/// <param name="RareFishRungReduction">Rungs a rare fish is moved down the difficulty ladder, which makes it easier to land.</param>
/// <param name="ForecastTurns">The turns, from the first, in which an angler who sees two moves ahead sees them.</param>
public sealed record LineAnglerEffects(
    int TensionCapBonus = 0,
    int ReelDistanceBonus = 0,
    int ReelTensionBonus = 0,
    int GiveLineDistanceReduction = 0,
    int SetHookDrainBonus = 0,
    int HoldDrainBonus = 0,
    int ForecastDepth = 1,
    int RareFishRungReduction = 0,
    int ForecastTurns = LineRules.FishSenseTurns)
{
    /// <summary>An angler with no perks.</summary>
    public static readonly LineAnglerEffects None = new();

    /// <summary>True when any number differs from an angler with no perks.</summary>
    public bool HasAny => this != None;

    /// <summary>Reads the record from an angler's stat totals, one lookup per stat.</summary>
    /// <param name="total">The angler's total for a stat, such as <c>Stat.GetStatAdjustment</c> for the fishing player.</param>
    public static LineAnglerEffects FromStatTotals(Func<StatType, int> total)
    {
        if (total == null)
            throw new ArgumentNullException(nameof(total));

        var senseTurns = Math.Max(0, total(StatType.AnglerForecastTurns));
        return new LineAnglerEffects(
            TensionCapBonus: Math.Max(0, total(StatType.AnglerTensionCap)),
            ReelDistanceBonus: Math.Max(0, total(StatType.AnglerReelDistance)),
            GiveLineDistanceReduction: Math.Max(0, total(StatType.AnglerGiveLineDistanceReduction)),
            SetHookDrainBonus: Math.Max(0, total(StatType.AnglerSetHookDrain)),
            ForecastDepth: senseTurns > 0 ? 2 : 1,
            RareFishRungReduction: Math.Max(0, total(StatType.AnglerRareFishRungs)),
            ForecastTurns: senseTurns > 0 ? senseTurns : LineRules.FishSenseTurns);
    }

    /// <summary>
    /// What a rare fish lets through (owner decision, October 2026): every fight perk at half strength, rounded down, so a bonus of 1
    /// does nothing, and Fish Sense (which cannot be halved) is off. Trophy Catch, the rare-fish perk, is the only one at full strength.
    /// </summary>
    public LineAnglerEffects AtHalfStrengthAgainstRareFish() => new(
        TensionCapBonus / 2,
        ReelDistanceBonus / 2,
        ReelTensionBonus / 2,
        GiveLineDistanceReduction / 2,
        SetHookDrainBonus / 2,
        HoldDrainBonus / 2,
        1,
        RareFishRungReduction,
        ForecastTurns);
}
