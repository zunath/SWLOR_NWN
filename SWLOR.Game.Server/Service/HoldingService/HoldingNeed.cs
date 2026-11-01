using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.HoldingService
{
    /// <summary>A good the holding takes deliveries of: how many it wants in all, how many it has, and what for.</summary>
    public class HoldingNeed
    {
        public CommodityType Commodity { get; set; }
        public int Wanted { get; set; }
        public int Have { get; set; }
        public string Purpose { get; set; } = string.Empty;

        public int Missing => System.Math.Max(0, Wanted - Have);
    }
}
