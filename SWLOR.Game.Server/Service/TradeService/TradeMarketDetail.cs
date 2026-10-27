namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>How one exchange prices one commodity.</summary>
    public class TradeMarketDetail
    {
        public CommodityType Commodity { get; set; }
        public CommodityRole Role { get; set; }

        /// <summary>The multiple of the base price this exchange trades at with no flooding.</summary>
        public double PriceFactor { get; set; }

        /// <summary>The pressure, in units, that moves the price by a factor of e.</summary>
        public int Depth { get; set; }
    }
}
