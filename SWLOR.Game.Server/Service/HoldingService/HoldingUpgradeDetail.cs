using System.Collections.Generic;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.HoldingService
{
    /// <summary>A holding upgrade (a level, or the Processing Plant): credits plus goods delivered to the holding's dock.</summary>
    public class HoldingUpgradeDetail
    {
        /// <summary>The level it raises the holding to; 0 for the Processing Plant.</summary>
        public int Level { get; set; }

        public int Credits { get; set; }

        public IReadOnlyDictionary<CommodityType, int> Goods { get; set; } = new Dictionary<CommodityType, int>();
    }
}
