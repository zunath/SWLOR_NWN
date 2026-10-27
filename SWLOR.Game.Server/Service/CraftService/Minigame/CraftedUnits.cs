using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// Where a finished craft's output went: the items now in the player's inventory, and the serialized units that did not fit
/// (or could not be copied), to be held on the player until there is room.
/// </summary>
public sealed record CraftedUnits(List<uint> Delivered, List<string> Held);
