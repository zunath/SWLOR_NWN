namespace SWLOR.Game.Server.Service.TournamentService
{
    /// <summary>
    /// Where one match of a bracket stands. Values are saved with the tournament and never change.
    /// </summary>
    public enum TournamentMatchState
    {
        Invalid = 0,

        /// <summary>Not called yet. It can be called once both duelists are known.</summary>
        Pending = 1,

        /// <summary>Called: the duelists have <see cref="TournamentRules.NoShowMinutes"/> minutes to stand in the ring.</summary>
        Called = 2,

        /// <summary>The duel is being fought.</summary>
        Active = 3,

        /// <summary>Interrupted too often. An official picks the winner.</summary>
        AwaitingOfficial = 4,

        /// <summary>Finished, with or without a winner.</summary>
        Done = 5
    }
}
