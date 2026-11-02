namespace SWLOR.Game.Server.Service.CraftService.Minigame.Search;

/// <summary>Which colour a row of the Search window takes.</summary>
public enum SearchActionKind
{
    Normal = 0,

    /// <summary>The action may spoil the spot.</summary>
    Risky = 1,

    /// <summary>The action cannot be used now.</summary>
    Unavailable = 2,

    /// <summary>The action is the last search of the site.</summary>
    LastSearch = 3
}
