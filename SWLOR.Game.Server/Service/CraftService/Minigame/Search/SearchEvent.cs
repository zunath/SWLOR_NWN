namespace SWLOR.Game.Server.Service.CraftService.Minigame.Search;

/// <summary>What the last action was.</summary>
public enum SearchEventKind
{
    None = 0,
    Search = 1,
    DigDeeper = 2,
    Sift = 3
}

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

/// <summary>
/// The last action the board resolved, kept in the board's state so a window or a lifecycle can read what happened without
/// comparing counters.
/// </summary>
/// <param name="Kind">The action.</param>
/// <param name="Spot">The spot it was aimed at, or -1 before any action.</param>
/// <param name="Result">How it turned out.</param>
/// <param name="Revealed">For a Sift, the spots whose ground it showed; zero otherwise.</param>
public sealed record SearchEvent(SearchEventKind Kind, int Spot, SearchEventResult Result, int Revealed = 0)
{
    public static readonly SearchEvent None = new(SearchEventKind.None, -1, SearchEventResult.None);

    /// <summary>True for a Search or a Dig Deeper: the actions that can find something.</summary>
    public bool IsAttempt => Kind is SearchEventKind.Search or SearchEventKind.DigDeeper;

    /// <summary>
    /// True for every action: each one pays Gathering XP, the site's full XP when it finds something and a quarter of it when it does not. A
    /// Sift never finds, so it pays the quarter.
    /// </summary>
    public bool PaysXp => Kind != SearchEventKind.None;

    /// <summary>True when the action found something: a Search that found, or a Dig Deeper that did.</summary>
    public bool IsFind => Result == SearchEventResult.Found;
}
