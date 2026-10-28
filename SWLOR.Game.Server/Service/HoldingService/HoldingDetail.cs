using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.HoldingService
{
    /// <summary>A holding type's fixed data. Rows come from the generated HoldingCatalog.</summary>
    public class HoldingDetail
    {
        public HoldingType Type { get; set; }
        public string Name { get; set; }

        /// <summary>The dock exchange of the planet it stands on.</summary>
        public TradeExchangeType Exchange { get; set; }

        /// <summary>The good it makes.</summary>
        public CommodityType Output { get; set; }

        /// <summary>A level 1 holding's units a day, before the droid.</summary>
        public int BatchUnits { get; set; }

        public int PurchasePrice { get; set; }

        /// <summary>Credits a day by level (index 0 is level 1).</summary>
        public int[] UpkeepByLevel { get; set; } = Array.Empty<int>();

        /// <summary>Levels 2 and 3: credits and goods delivered to the holding's dock.</summary>
        public List<HoldingUpgradeDetail> Upgrades { get; set; } = new();

        public HoldingUpgradeDetail Plant { get; set; }

        /// <summary>What the Processing Plant makes from the holding's own output.</summary>
        public HoldingChainDetail Chain { get; set; }

        public HoldingSetbackType Setback { get; set; }

        /// <summary>The goods that end a setback.</summary>
        public CommodityType RepairGood { get; set; }

        public int RepairUnits { get; set; }

        /// <summary>Whether customs inspect it (contraband holdings).</summary>
        public bool CustomsInspections { get; set; }

        /// <summary>The instance template of its ground site.</summary>
        public string SiteAreaResref { get; set; }

        public string Description { get; set; }
    }
}
