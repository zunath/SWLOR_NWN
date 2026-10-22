namespace SWLOR.Game.Server.Service.CraftService.Minigame.Line;

/// <summary>
/// The adjustments an angler's perks make to the Line, every one zero until a perk grants it. The engine reads these and never
/// checks a perk by name; the perk line that sets them is drafted in <c>design/testing/crafting-revamp/agriculture-angler-perk-line-draft.md</c>.
/// </summary>
/// <param name="TensionCapBonus">Added to the Tension cap.</param>
/// <param name="ReelDistanceBonus">Added to the Distance a Reel closes.</param>
/// <param name="ReelTensionBonus">Added to the Tension a Reel adds.</param>
/// <param name="GiveLineDistanceReduction">Taken off the Distance Give Line lets out.</param>
/// <param name="SetHookDrainBonus">Added to the Stamina Set Hook drains.</param>
/// <param name="HoldDrainBonus">Added to the Stamina Hold drains.</param>
/// <param name="ForecastDepth">How many of the fish's next moves the window shows: 1, or 2.</param>
/// <param name="RareFishRungReduction">Rungs a rare fish is moved down the difficulty ladder, which makes it easier to land.</param>
public sealed record LineAnglerEffects(
    int TensionCapBonus = 0,
    int ReelDistanceBonus = 0,
    int ReelTensionBonus = 0,
    int GiveLineDistanceReduction = 0,
    int SetHookDrainBonus = 0,
    int HoldDrainBonus = 0,
    int ForecastDepth = 1,
    int RareFishRungReduction = 0)
{
    /// <summary>An angler with no perks.</summary>
    public static readonly LineAnglerEffects None = new();
}
