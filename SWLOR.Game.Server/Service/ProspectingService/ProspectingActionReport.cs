using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Service.ProspectingService
{
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
}
