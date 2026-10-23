using SWLOR.Game.Server.Service.CraftService.Minigame;

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

    /// <summary>What happened when a player used a harvester on a node.</summary>
    /// <param name="Status">Whether a session began, and if not why.</param>
    /// <param name="Session">The session, when one began.</param>
    public sealed record ProspectingBegin(ProspectingBeginStatus Status, ProspectingSession Session)
    {
        public bool Opened => Status == ProspectingBeginStatus.Opened;
    }

    /// <summary>The result of asking a session to carry out an action.</summary>
    /// <param name="Started">True when the action began (a Leave resolves at once).</param>
    /// <param name="Reason">Why it was refused, when it was.</param>
    public readonly record struct ProspectingActionStart(bool Started, string Reason)
    {
        public static ProspectingActionStart Ok => new(true, string.Empty);

        public static ProspectingActionStart Refused(string reason) => new(false, reason);
    }

    /// <summary>What an action did, once it resolved: what the engine reports, and what it handed the player.</summary>
    /// <param name="ActionId">The action.</param>
    /// <param name="Result">What the engine reported. Null when the session had ended before the action resolved.</param>
    /// <param name="Delivered">The items the action put in the player's inventory.</param>
    /// <param name="Xp">The Gathering XP the action paid.</param>
    /// <param name="SeamsFound">The seams the action revealed.</param>
    /// <param name="Ended">True when the action ended the session.</param>
    /// <param name="Message">The words for the session's end, when it ended.</param>
    public sealed record ProspectingActionReport(
        string ActionId,
        MinigameActionResult Result,
        HarvestDelivery Delivered,
        int Xp,
        int SeamsFound,
        bool Ended,
        string Message);

    /// <summary>Progress of a Quick Harvest: one unit at a time, each taking a harvest delay.</summary>
    /// <param name="UnitsDone">The units taken so far.</param>
    /// <param name="UnitsTotal">The units it set out to take.</param>
    /// <param name="Xp">The XP paid for the last unit.</param>
    /// <param name="Finished">True when the Quick Harvest is over: the last unit was taken or it was cut short.</param>
    /// <param name="Message">The words for its end, when it ended.</param>
    public sealed record QuickHarvestReport(int UnitsDone, int UnitsTotal, int Xp, bool Finished, string Message);
}
