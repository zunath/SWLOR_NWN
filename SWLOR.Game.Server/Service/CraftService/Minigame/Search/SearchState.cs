namespace SWLOR.Game.Server.Service.CraftService.Minigame.Search;

/// <summary>
/// Everything the Search board knows about a site, as one immutable value. The shared meters hold the searches: Progress is the
/// searches spent, out of the scavenger's total, and the board ends when it is full. Quality, Integrity and CP are not used.
/// </summary>
/// <param name="Meters">Searches spent; the rest are empty.</param>
/// <param name="NextOpportunity">Always null: the board telegraphs nothing; the ground the scavenger has learned is the information.</param>
/// <param name="Board">The 3x3 board: which spots are promising, which are known, and what was done at each.</param>
/// <param name="OrdinaryFinds">The Searches that found something: one roll of the site's loot table each.</param>
/// <param name="DeeperFinds">The Dig Deeper attempts that found something: one roll of the site's loot table each, with the rare entries favoured.</param>
/// <param name="Attempts">The Searches and Dig Deeper attempts made, the actions that can find something.</param>
/// <param name="SuccessfulAttempts">The attempts that found something.</param>
/// <param name="Sifts">The Sifts made.</param>
/// <param name="Last">The last action resolved.</param>
public sealed record SearchState(
    MinigameMeters Meters,
    MinigameOpportunity NextOpportunity,
    SearchBoard Board,
    int OrdinaryFinds,
    int DeeperFinds,
    int Attempts,
    int SuccessfulAttempts,
    int Sifts,
    SearchEvent Last) : IMinigameState<SearchState>
{
    public SearchState WithMeters(MinigameMeters meters) => this with { Meters = meters };

    /// <summary>The searches spent.</summary>
    public int SearchesSpent => Meters.Progress.Value;

    /// <summary>The searches left.</summary>
    public int SearchesLeft => Meters.Progress.Max - Meters.Progress.Value;

    /// <summary>Everything the board has found, by Search and by Dig Deeper.</summary>
    public int Finds => OrdinaryFinds + DeeperFinds;
}
