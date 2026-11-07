namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>The starlane hops between two exchanges. Rows come from the generated TradeCatalog.</summary>
    public class TradeRouteDetail
    {
        public TradeExchangeType From { get; set; }
        public TradeExchangeType To { get; set; }
        public int Hops { get; set; }
    }
}
