namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A player-built droid posted at a crew station of a ship (plan section 10, "Droid crew"). Saved on the ship's
    /// record, so the posting survives logouts and restarts. The droid's controller item stays with its owner,
    /// cursed while posted, and the droid is never spawned as a follower; the tier and name are copied here so the
    /// droid works while its owner is offline.
    /// </summary>
    public class ShipDroidCrew
    {
        public ShipCrewSeat Seat { get; set; }

        /// <summary>The player who owns the droid's controller item.</summary>
        public string OwnerPlayerId { get; set; } = string.Empty;

        /// <summary>The controller item's droid id (its <c>DROID_ITEM_ID</c> local).</summary>
        public string DroidItemId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int Tier { get; set; } = 1;

        public DateTime PostedAt { get; set; }
    }
}
