namespace SWLOR.Game.Server.Service.CraftService.Minigame.Line;

/// <summary>What an action does to the fight against the fish's move: positive values add, negative values take away.</summary>
/// <param name="Distance">The change in the fish's Distance.</param>
/// <param name="Tension">The change in the line's Tension.</param>
/// <param name="Stamina">The change in the fish's Stamina.</param>
public readonly record struct LineEffect(int Distance, int Tension, int Stamina);
