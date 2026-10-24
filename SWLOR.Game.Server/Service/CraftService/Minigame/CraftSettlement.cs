namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>What settling a failed craft did.</summary>
/// <param name="Settled">False when there was no craft to settle.</param>
/// <param name="ItemsLost">Component stacks and enhancements that were lost.</param>
/// <param name="ItemsKept">Component stacks and enhancements that came back, or wait to.</param>
/// <param name="Xp">The XP granted for the failed attempt.</param>
public sealed record CraftSettlement(bool Settled, int ItemsLost, int ItemsKept, int Xp)
{
    public static readonly CraftSettlement None = new(false, 0, 0, 0);
}
