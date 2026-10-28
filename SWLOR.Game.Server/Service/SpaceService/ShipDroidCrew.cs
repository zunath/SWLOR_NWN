using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A player-built droid posted at a crew station or the droid bay of a ship (plan sections 10 and 27). Saved on the
    /// ship's record, so the posting survives logouts and restarts. The droid's controller item stays with its owner,
    /// cursed while posted, and the droid is never spawned as a follower; the tier, Industry, protocols and name are
    /// copied here so the droid works while its owner is offline.
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

        /// <summary>The droid's Industry when it was posted (plan section 27). Postings saved before it existed read 0.</summary>
        public int Industry { get; set; }

        /// <summary>The industrial protocols the droid ran when it was posted, by rank.</summary>
        public Dictionary<PerkType, int> Protocols { get; set; } = new();

        public DateTime PostedAt { get; set; }
    }
}
