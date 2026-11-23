using System.Collections.Generic;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The flooding of every commodity at one dock exchange (plan section 13). One record per exchange, with the id
    /// from <see cref="GetId"/>; a market with no record trades at its unflooded price.
    /// </summary>
    public class TradeExchangeState : EntityBase
    {
        public TradeExchangeState()
        {
            Markets = new Dictionary<CommodityType, TradeMarketState>();
            LastTradeIds = new Dictionary<string, string>();
        }

        public TradeExchangeType Exchange { get; set; }
        public Dictionary<CommodityType, TradeMarketState> Markets { get; set; }

        /// <summary>
        /// The last exchange trade each player pressed these markets with (player id to trade id), saved with the
        /// pressure, so a trade a restart interrupted presses them once at the player's next login (PendingTrade).
        /// Older records load empty.
        /// </summary>
        public Dictionary<string, string> LastTradeIds { get; set; }

        public static string GetId(TradeExchangeType exchange) => $"TRADE_EXCHANGE_{exchange}";
    }
}
