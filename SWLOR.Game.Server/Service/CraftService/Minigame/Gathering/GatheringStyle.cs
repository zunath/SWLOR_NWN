namespace SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;

/// <summary>Gathering's styles. A gatherer has at most one active, stored as a status effect, and it applies to every Gathering window.</summary>
public enum GatheringStyle
{
    None = 0,

    /// <summary>The Prospector line's style: bigger Strikes that cost more Stability.</summary>
    StripMining = 1,

    /// <summary>The Treasure Seeker line's style: patient searching and prospecting that favours rare finds.</summary>
    CarefulHands = 2
}
