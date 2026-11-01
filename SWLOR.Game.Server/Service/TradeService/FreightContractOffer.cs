namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>A freight contract an exchange offers this hour.</summary>
    public class FreightContractOffer
    {
        public string Id { get; set; }
        public TradeExchangeType Origin { get; set; }
        public TradeExchangeType Destination { get; set; }
        public int Volume { get; set; }
        public int Hops { get; set; }
        public int Fee { get; set; }
        public int Deposit { get; set; }
        public int DeadlineMinutes { get; set; }

        /// <summary>The standing with the offering exchange's faction needed to take this load (0 for any pilot).</summary>
        public int MinimumStanding { get; set; }
    }
}
