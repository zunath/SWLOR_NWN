namespace SWLOR.Game.Server.Service.TournamentService
{
    /// <summary>
    /// How a tournament ended. Values are saved with the tournament and never change.
    /// </summary>
    public enum TournamentOutcome
    {
        None = 0,

        /// <summary>The final ended with a champion, who is paid the prize.</summary>
        Completed = 1,

        /// <summary>Called off: entry fees go back to the entrants and the prize back to the treasury.</summary>
        Cancelled = 2,

        /// <summary>The city or its ring was removed: entry fees go back to the entrants and the treasury goes with the city.</summary>
        CityRemoved = 3
    }
}
