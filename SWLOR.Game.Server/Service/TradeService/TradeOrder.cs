namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>A planned purchase or sale: how many units change hands for how many credits, or why none do.</summary>
    public class TradeOrder
    {
        public TradeRefusal Refusal { get; set; }
        public int Units { get; set; }
        public int Credits { get; set; }

        /// <summary>Fewer units than were asked for, because of room, credits or what is aboard.</summary>
        public bool WasReduced { get; set; }

        public static TradeOrder Refused(TradeRefusal refusal) => new() { Refusal = refusal };
    }
}
