namespace SWLOR.Game.Server.Service.TournamentService
{
    /// <summary>
    /// How a match ended. Values are saved with the tournament and never change.
    /// </summary>
    public enum TournamentMatchResult
    {
        None = 0,

        /// <summary>One duelist was subdued and the other was not.</summary>
        Victory = 1,

        /// <summary>One duelist left the ring or logged out mid-duel, or did not show in time.</summary>
        Forfeit = 2,

        /// <summary>Neither duelist showed in time. Nobody advances.</summary>
        BothForfeit = 3,

        /// <summary>An official picked the winner after the match was interrupted too often.</summary>
        Official = 4,

        /// <summary>The other place in the match was empty, so the duelist advanced without a fight.</summary>
        Bye = 5,

        /// <summary>Nobody was left in either place, so nobody advances.</summary>
        NoContest = 6
    }
}
