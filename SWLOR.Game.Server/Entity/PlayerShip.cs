using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;

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
            Cargo = new Dictionary<CommodityType, CargoLot>();
            SealedFreight = new List<SealedFreightLot>();
        }

        [Indexed]
        public string OwnerPlayerId { get; set; }
        [Indexed]
        public string PropertyId { get; set; }
        public string SerializedItem { get; set; }
        public ShipStatus Status { get; set; }
        public Dictionary<string, string> PlayerHotBars { get; set; }

        /// <summary>
        /// The appearance of the Hull Design a starport Refit gave the ship (plan section 7), or 0 for its base model.
        /// Older records load 0.
        /// </summary>
        public int HullDesignAppearance { get; set; }

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

        /// <summary>
        /// Commodity units in the ship's cargo bay (plan section 13), never items: they cannot enter an inventory, the
        /// bank, the market or contracts. Older records load empty.
        /// </summary>
        public Dictionary<CommodityType, CargoLot> Cargo { get; set; }

        /// <summary>
        /// Freight contracts' sealed loads aboard (plan section 13): they fill cargo volume and cannot be sold. Older
        /// records load with none.
        /// </summary>
        public List<SealedFreightLot> SealedFreight { get; set; }
    }
}
