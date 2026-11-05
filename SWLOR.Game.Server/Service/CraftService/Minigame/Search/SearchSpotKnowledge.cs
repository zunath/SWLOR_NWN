namespace SWLOR.Game.Server.Service.CraftService.Minigame.Search;

/// <summary>What the player knows about a spot's ground.</summary>
public enum SearchSpotKnowledge
{
    /// <summary>Nothing yet: the ground may be promising or ordinary.</summary>
    Unknown = 0,

    /// <summary>The ground is promising: a Search there finds more often.</summary>
    Promising = 1,

    /// <summary>The ground is ordinary.</summary>
    Ordinary = 2
}
