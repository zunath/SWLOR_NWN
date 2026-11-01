namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>A finished craft: the item that was created and the outcome that decided it.</summary>
/// <param name="Item">The crafted item.</param>
/// <param name="Outcome">What the final quality produced.</param>
public sealed record CraftSessionSuccess(uint Item, CraftOutcome Outcome);
