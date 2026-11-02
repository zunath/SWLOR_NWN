namespace SWLOR.Game.Server.Service.TournamentService
{
    /// <summary>
    /// What a called match's missing duelists cost them once their five minutes are up.
    /// </summary>
    public enum NoShowDecision
    {
        None = 0,

        /// <summary>The first duelist did not show, so the second advances.</summary>
        PlayerAForfeits = 1,

        /// <summary>The second duelist did not show, so the first advances.</summary>
        PlayerBForfeits = 2,

        /// <summary>Neither showed. Nobody advances.</summary>
        BothForfeit = 3
    }
}
