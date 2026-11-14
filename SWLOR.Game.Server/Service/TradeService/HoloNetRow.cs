namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>One commodity on the HoloNet board: where it sold cheapest and where it fetched the most.</summary>
    public class HoloNetRow
    {
        public CommodityType Commodity { get; set; }
        public string Name { get; set; }
        public CommodityLegality Legality { get; set; }

        /// <summary>The exchange selling it cheapest, and its price per unit.</summary>
        public TradeExchangeType BuyAt { get; set; }
        public int BuyPrice { get; set; }

        /// <summary>The exchange paying the most for it, and its price per unit.</summary>
        public TradeExchangeType SellAt { get; set; }
        public int SellPrice { get; set; }
    }
}
