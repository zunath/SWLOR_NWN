using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.HoldingService
{
    /// <summary>
    /// A Processing Plant recipe (plan section 26): OwnUnits of the holding's good plus InputUnits of a delivered good
    /// make RefinedUnits of a refined good worth more than both.
    /// </summary>
    public class HoldingChainDetail
    {
        public int OwnUnits { get; set; }
        public CommodityType Input { get; set; }
        public int InputUnits { get; set; }
        public CommodityType Refined { get; set; }
        public int RefinedUnits { get; set; }
    }
}
