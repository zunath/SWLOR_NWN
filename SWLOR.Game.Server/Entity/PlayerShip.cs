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
            DockedDerelictId = string.Empty;
            Cargo = new Dictionary<CommodityType, CargoLot>();
            SealedFreight = new List<SealedFreightLot>();
            SaleId = string.Empty;
            SaleSellerPlayerId = string.Empty;
            SaleBuyerPlayerId = string.Empty;
            LastHoldingCollectionId = string.Empty;
            PendingHoldingDeliveryId = string.Empty;
            PendingHoldingDeliveryHoldingId = string.Empty;
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
        /// The derelict this ship is docked at while its party is aboard (plan section 28, the derelict's signature id),
        /// or empty. Derelicts live in memory, so a value left by a restart is cleared when read. Older records load empty.
        /// </summary>
        public string DockedDerelictId { get; set; }

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

        /// <summary>
        /// A market sale whose proceeds the seller is still owed (plan section 7), or empty. It is saved with the new
        /// owner, so a restart between the ownership change and the payout leaves the payout to make at the next
        /// module load. Older records load empty.
        /// </summary>
        public string SaleId { get; set; }

        /// <summary>The player owed <see cref="SaleProceeds"/> for <see cref="SaleId"/>.</summary>
        public string SaleSellerPlayerId { get; set; }

        /// <summary>The credits owed for <see cref="SaleId"/>, after the market tax.</summary>
        public int SaleProceeds { get; set; }

        /// <summary>
        /// The buyer of <see cref="SaleId"/>. Saved before the ship changes hands, so a restart part-way through the
        /// transfer finishes it at the next module load. Older records load empty.
        /// </summary>
        public string SaleBuyerPlayerId { get; set; }

        /// <summary>
        /// The last Planetary Holding collection loaded into the cargo bay, saved with the cargo, so a collection a
        /// restart left pending is not loaded twice. Older records load empty.
        /// </summary>
        public string LastHoldingCollectionId { get; set; }

        /// <summary>
        /// A Planetary Holding delivery taken out of the cargo bay whose holding may not have saved it yet, or empty.
        /// It is saved with the cargo it came from, so a restart before the holding saves finishes the delivery at the
        /// next module load. Older records load empty.
        /// </summary>
        public string PendingHoldingDeliveryId { get; set; }

        /// <summary>The holding <see cref="PendingHoldingDeliveryId"/> goes to.</summary>
        public string PendingHoldingDeliveryHoldingId { get; set; }

        /// <summary>The good taken for <see cref="PendingHoldingDeliveryId"/>.</summary>
        public CommodityType PendingHoldingDeliveryCommodity { get; set; }

        /// <summary>The units taken for <see cref="PendingHoldingDeliveryId"/>.</summary>
        public int PendingHoldingDeliveryUnits { get; set; }
    }
}
