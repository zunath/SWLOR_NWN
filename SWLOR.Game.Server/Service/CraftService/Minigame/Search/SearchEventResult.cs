namespace SWLOR.Game.Server.Service.CraftService.Minigame.Search;

/// <summary>How the last action turned out.</summary>
public enum SearchEventResult
{
    None = 0,

    /// <summary>A Search or a Dig Deeper found nothing.</summary>
    Nothing = 1,

    /// <summary>A Search or a Dig Deeper found something: one roll of the site's loot table.</summary>
    Found = 2,

    /// <summary>A Dig Deeper ruined the spot.</summary>
    Spoiled = 3,

    /// <summary>A Sift showed the ground around a spot.</summary>
    Sifted = 4
}
