using System;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// A freight contract a player holds (plan section 13): haul a sealed load from one exchange to another by the
    /// deadline for a fixed fee. The deposit paid on accepting comes back with an on-time delivery.
    /// </summary>
    public class FreightContract
    {
        /// <summary>The offer's id (exchange, hour and slot), so the same offer cannot be taken twice.</summary>
        public string Id { get; set; }

        public TradeExchangeType Origin { get; set; }
        public TradeExchangeType Destination { get; set; }

        /// <summary>The ship carrying the sealed load.</summary>
        public string ShipId { get; set; }

        public int Volume { get; set; }
        public int Fee { get; set; }
        public int Deposit { get; set; }
        public DateTime AcceptedAt { get; set; }
        public DateTime Deadline { get; set; }

        /// <summary>
        /// A faction agent's sealed courier run (plan section 29): no fee or deposit, the agent pays on delivery, and it
        /// does not count toward the freight contract limit. Its id is the mission's.
        /// </summary>
        public bool IsAgentCourier { get; set; }
    }
}
