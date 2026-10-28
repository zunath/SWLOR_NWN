using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.HoldingService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A Planetary Holding a character owns (plan section 26): one record per holding, found by its owner. Output is
    /// commodity units in the dock warehouse, never items. A new type, so no migration.
    /// </summary>
    public class PlanetaryHolding : EntityBase
    {
        public PlanetaryHolding()
        {
            OwnerPlayerId = string.Empty;
            DockId = string.Empty;
            Level = 1;
            RefineOutput = true;
            Warehouse = new Dictionary<CommodityType, int>();
            Supplies = new Dictionary<CommodityType, int>();
            DroidItemId = string.Empty;
            DroidName = string.Empty;
            Log = new List<string>();
        }

        [Indexed]
        public string OwnerPlayerId { get; set; }

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

        /// <summary>The staff droid's controller id (<c>DROID_ITEM_ID</c>), or empty. The controller stays with the owner, cursed.</summary>
        public string DroidItemId { get; set; }

        public string DroidName { get; set; }

        /// <summary>The staff droid's Industry when it was posted (plan section 27).</summary>
        public int DroidIndustry { get; set; }

        /// <summary>The staff droid's Efficiency Protocol percent when it was posted.</summary>
        public int DroidEfficiencyPercent { get; set; }

        /// <summary>The latest events, newest last.</summary>
        public List<string> Log { get; set; }
    }
}
