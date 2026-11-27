namespace SWLOR.Game.Server.Service.PvpCombatService
{
    /// <summary>
    /// What a request to change the PvP flag or the shown identity amounts to while a PvP combat window may be open.
    /// </summary>
    public enum PvpCombatChangeOutcome
    {
        /// <summary>Nothing needs to change: the request matches what is in effect and nothing was waiting.</summary>
        Unchanged = 0,

        /// <summary>The change goes ahead now.</summary>
        Applied = 1,

        /// <summary>The window is open, so the change waits for it to end.</summary>
        Queued = 2,

        /// <summary>The request undid a waiting one, so nothing will change when the window ends.</summary>
        Cancelled = 3
    }
}
