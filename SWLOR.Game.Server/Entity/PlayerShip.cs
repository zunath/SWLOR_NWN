using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Entity
{
    public class PlayerShip: EntityBase
    {
        public PlayerShip()
        {
            PlayerHotBars = new Dictionary<string, string>();
            DroidCrew = new List<ShipDroidCrew>();
            DockedToShipId = string.Empty;
            DockedCrewPlayerIds = new List<string>();
        }

        [Indexed]
        public string OwnerPlayerId { get; set; }
        [Indexed]
        public string PropertyId { get; set; }
        public string SerializedItem { get; set; }
        public ShipStatus Status { get; set; }
        public Dictionary<string, string> PlayerHotBars { get; set; }

        /// <summary>Droids posted at the ship's crew stations (plan section 10). Older records load with none.</summary>
        public List<ShipDroidCrew> DroidCrew { get; set; }

        /// <summary>
        /// The Large or Capital player ship this ship is docked aboard (plan section 10, "Ship-to-ship docking"), or
        /// empty. Older records load empty.
        /// </summary>
        [Indexed]
        public string DockedToShipId { get; set; }

        /// <summary>
        /// The players who came aboard the host with this ship, the docking pilot first. They return to it when the
        /// host is destroyed. Older records load with none.
        /// </summary>
        public List<string> DockedCrewPlayerIds { get; set; }
    }
}
