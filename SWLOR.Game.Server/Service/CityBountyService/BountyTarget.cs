namespace SWLOR.Game.Server.Service.CityBountyService
{
    /// <summary>
    /// An online identity that shows the PvP flag, as a poster sees it in the Player List, and why a city cannot post a
    /// bounty on it right now.
    /// </summary>
    /// <param name="Player">The player's character in the world.</param>
    /// <param name="PlayerId">The player's id.</param>
    /// <param name="DisguiseId">The disguise shown, or empty for the player's own character.</param>
    /// <param name="Name">How the poster refers to the player: the name they gave them, or the descriptor they see.</param>
    /// <param name="Problem">Why no bounty can be posted on the identity, or an empty string when one can.</param>
    public sealed record BountyTarget(uint Player, string PlayerId, string DisguiseId, string Name, string Problem);
}
