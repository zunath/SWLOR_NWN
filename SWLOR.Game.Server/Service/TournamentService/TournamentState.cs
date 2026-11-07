namespace SWLOR.Game.Server.Service.TournamentService
{
    /// <summary>
    /// Where a tournament stands. Values are saved with the tournament and never change.
    /// </summary>
    public enum TournamentState
    {
        Invalid = 0,

        /// <summary>Posted. Players sign up and pay the entry fee until the start time.</summary>
        SignUp = 1,

        /// <summary>The bracket is set and the matches are being fought.</summary>
        Running = 2,

        /// <summary>Over, and the money is being paid out or returned. Kept only until that finishes, so a restart can finish it.</summary>
        Settling = 3
    }
}
