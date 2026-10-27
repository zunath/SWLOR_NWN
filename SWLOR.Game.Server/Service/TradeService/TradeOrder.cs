namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>A planned purchase or sale: how many units change hands for how many credits, or why none do.</summary>
    public class TradeOrder
    {
        public TradeRefusal Refusal { get; set; }
        public int Units { get; set; }

        /// <summary>The credits that change hands with the player: a purchase's cost plus the tariff, a sale's revenue minus it.</summary>
        public int Credits { get; set; }

        /// <summary>The part of <see cref="Credits"/> that is the city's tariff (0 at an NPC dock).</summary>
        public int Tariff { get; set; }

        /// <summary>Fewer units than were asked for, because of room, credits or what is aboard.</summary>
        public bool WasReduced { get; set; }

        public static TradeOrder Refused(TradeRefusal refusal) => new() { Refusal = refusal };
    }
}
