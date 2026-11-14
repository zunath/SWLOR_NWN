using System;
using System.Linq;
using SWLOR.Game.Server.Feature.TradeDefinition;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The HoloNet price board (plan section 13): the best prices across the exchanges from a snapshot rebuilt at most
    /// once per refresh period, so it runs stale. The snapshot lives in memory and is rebuilt after a restart.
    /// </summary>
    public static partial class Trade
    {
        private static HoloNetSnapshot _holoNet;
        private static string _holoNetPriceKey = string.Empty;

        /// <summary>
        /// The board, rebuilt first when its snapshot is stale or an incursion started or ended since (the region's
        /// exchanges price at the incursion spike, plan section 30).
        /// </summary>
        public static HoloNetSnapshot GetHoloNetBoard()
        {
            var now = DateTime.UtcNow;
            var priceKey = HoloNetRules.GetPriceKey(Space.GetActiveIncursion());
            if (_holoNet != null && !HoloNetRules.IsStale(_holoNet.CapturedAt, now) && _holoNetPriceKey == priceKey)
                return _holoNet;

            var states = TradeCatalog.Exchanges.ToDictionary(x => x.Type, x => GetExchangeState(x.Type, now));
            _holoNet = HoloNetRules.Build(
                (exchange, commodity) => GetPressure(states[exchange], commodity),
                now,
                Space.GetIncursionPriceMultiplier);
            _holoNetPriceKey = priceKey;
            return _holoNet;
        }
    }
}
