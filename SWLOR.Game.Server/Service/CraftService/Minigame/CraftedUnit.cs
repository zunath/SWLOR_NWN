namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// One unit of a finished craft's output, bonuses applied, waiting outside the player's inventory: the item and its serialized form.
/// <see cref="Item"/> is invalid when the unit could not be made, and the serialized form is then the finished item's own.
/// </summary>
public sealed record CraftedUnit(uint Item, string Serialized);
