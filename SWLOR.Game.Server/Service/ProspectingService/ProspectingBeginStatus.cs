namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>The result of using a harvester on a node.</summary>
    public enum ProspectingBeginStatus
    {
        /// <summary>The session began and its window opened.</summary>
        Opened = 1,

        /// <summary>Another player is working the node.</summary>
        InUse = 2,

        /// <summary>The node has nothing left, and is removed.</summary>
        Spent = 3,

        /// <summary>The harvester has no charges.</summary>
        NoCharges = 4,

        /// <summary>The session's window could not be opened, so the session did not begin.</summary>
        WindowFailed = 5
    }
}
