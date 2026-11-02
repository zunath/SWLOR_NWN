namespace SWLOR.Game.Server.Service.DuelService
{
    /// <summary>
    /// How a finished duel ended.
    /// </summary>
    public enum DuelOutcomeType
    {
        /// <summary>One duelist was subdued and the other was not.</summary>
        Victory = 1,

        /// <summary>Both duelists were subdued or left together.</summary>
        Draw = 2,

        /// <summary>One duelist left the ring or logged out.</summary>
        Forfeit = 3
    }
}
