using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.HoldingService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A Planetary Holding a character owns (plan section 26): one record per holding, found by its owner. Output is
    /// commodity units in the dock warehouse, never items. A new type, so no migration. The limit of holdings is per
    /// account, counted by <see cref="OwnerCDKey"/>.
    /// </summary>
    public class PlanetaryHolding : EntityBase
    {
        public PlanetaryHolding()
        {
            OwnerPlayerId = string.Empty;
            OwnerCDKey = string.Empty;
            DockId = string.Empty;
            Level = 1;
            RefineOutput = true;
            Warehouse = new Dictionary<CommodityType, int>();
            Supplies = new Dictionary<CommodityType, int>();
            DroidItemId = string.Empty;
            DroidName = string.Empty;
            Log = new List<string>();
            PendingCollectionId = string.Empty;
            PendingCollectionShipId = string.Empty;
            PendingCollection = new Dictionary<CommodityType, int>();
            LastDeliveryId = string.Empty;
        }

        [Indexed]
        public string OwnerPlayerId { get; set; }

        /// <summary>
        /// The owner's public CD key when bought, which the per-account limit counts. Older records read empty and are
        /// filled the next time their owner's holdings load.
        /// </summary>
        [Indexed]
        public string OwnerCDKey { get; set; }

        public HoldingType Type { get; set; }

        /// <summary>The landing area resref of the NPC starport it was bought at; its ships collect here.</summary>
        public string DockId { get; set; }

        public int Level { get; set; }

        /// <summary>What the holding cost, for the sell-back refund.</summary>
        public int PricePaid { get; set; }

        public bool HasProcessingPlant { get; set; }

        /// <summary>Whether the Processing Plant refines the day's output (the owner may keep the raw good).</summary>
        public bool RefineOutput { get; set; }

        /// <summary>Output waiting at the dock warehouse for the owner's ship.</summary>
        public Dictionary<CommodityType, int> Warehouse { get; set; }

        /// <summary>Goods delivered for upgrades, the Processing Plant and repairs.</summary>
        public Dictionary<CommodityType, int> Supplies { get; set; }

        public int UpkeepOwed { get; set; }

        /// <summary>The end of the last whole day of output; the next day ends one day later.</summary>
        public DateTime LastProcessedAt { get; set; }

        public int DaysUntilEvent { get; set; }

        /// <summary>Days of doubled output still to come.</summary>
        public int BoomDays { get; set; }

        /// <summary>The setback stopping output, or Invalid.</summary>
        public HoldingSetbackType Setback { get; set; }

        public int RepairUnitsNeeded { get; set; }

        /// <summary>A customs fine waiting to be paid, or 0.</summary>
        public int CustomsFine { get; set; }

        public DateTime CustomsDeadline { get; set; }

        /// <summary>
        /// Raiders hold the site (W11o): kill them there by <see cref="RaidDeadline"/> or they take the raid's share of the
        /// warehouse. False on holdings saved before, so no migration.
        /// </summary>
        public bool RaidPending { get; set; }
        public DateTime RaidDeadline { get; set; }

        /// <summary>The staff droid's controller id (<c>DROID_ITEM_ID</c>), or empty. The controller stays with the owner, cursed.</summary>
        public string DroidItemId { get; set; }

        public string DroidName { get; set; }

        /// <summary>The staff droid's Industry when it was posted (plan section 27).</summary>
        public int DroidIndustry { get; set; }

        /// <summary>The staff droid's Efficiency Protocol percent when it was posted.</summary>
        public int DroidEfficiencyPercent { get; set; }

        /// <summary>The latest events, newest last.</summary>
        public List<string> Log { get; set; }

        /// <summary>
        /// A collection taken from the warehouse whose ship may not have saved it yet, or empty. It is saved with the
        /// warehouse it came from, so a restart before the ship saves finishes the collection at the next module load.
        /// Older records load empty.
        /// </summary>
        public string PendingCollectionId { get; set; }

        /// <summary>The ship <see cref="PendingCollection"/> goes to.</summary>
        public string PendingCollectionShipId { get; set; }

        /// <summary>The units taken for <see cref="PendingCollectionId"/>.</summary>
        public Dictionary<CommodityType, int> PendingCollection { get; set; }

        /// <summary>
        /// The last ship delivery added to the supplies, saved with them, so a delivery a restart left pending on the
        /// ship is not added twice. Older records load empty.
        /// </summary>
        public string LastDeliveryId { get; set; }
    }
}
