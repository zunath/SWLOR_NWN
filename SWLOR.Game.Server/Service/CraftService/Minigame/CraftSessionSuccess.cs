namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>A finished craft: the item that was created and the outcome that decided it.</summary>
/// <param name="Item">
/// The crafted item as it was made outside the inventory. It is destroyed once its units are delivered or held, so it is valid only until
/// the script that finished the craft ends.
/// </param>
/// <param name="Outcome">What the final quality produced.</param>
public sealed record CraftSessionSuccess(uint Item, CraftOutcome Outcome);
