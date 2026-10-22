namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>Whether the enhancement in one slot reaches the finished item.</summary>
/// <param name="Slot">The slot, counting from 1.</param>
/// <param name="ThresholdPercent">The quality the slot needs.</param>
/// <param name="Transfers">True when the crafted quality meets the threshold; the transfer is then guaranteed.</param>
public sealed record EnhancementTransfer(int Slot, int ThresholdPercent, bool Transfers);
