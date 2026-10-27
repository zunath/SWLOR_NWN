using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.TradeDefinition;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>Finds commodities and exchanges in the generated TradeCatalog.</summary>
    public static class TradeLookup
    {
        private static readonly Dictionary<CommodityType, CommodityDetail> _commodities =
            TradeCatalog.Commodities.ToDictionary(x => x.Type);

        private static readonly Dictionary<TradeExchangeType, TradeExchangeDetail> _exchanges =
            TradeCatalog.Exchanges.ToDictionary(x => x.Type);

        private static readonly Dictionary<PlanetType, TradeExchangeType> _exchangeByPlanet = TradeCatalog.Exchanges
            .SelectMany(exchange => exchange.PlanetTypes.Select(planet => (planet, exchange.Type)))
            .ToDictionary(x => x.planet, x => x.Type);

        /// <summary>The commodity's data, or null for an unknown type.</summary>
        public static CommodityDetail GetCommodity(CommodityType type)
        {
            return _commodities.TryGetValue(type, out var detail) ? detail : null;
        }

        /// <summary>The exchange's data, or null for an unknown type.</summary>
        public static TradeExchangeDetail GetExchange(TradeExchangeType type)
        {
            return _exchanges.TryGetValue(type, out var detail) ? detail : null;
        }

        /// <summary>The exchange an NPC starport on this planet opens, or Invalid when it has none.</summary>
        public static TradeExchangeType GetExchangeForPlanet(PlanetType planet)
        {
            return _exchangeByPlanet.TryGetValue(planet, out var type) ? type : TradeExchangeType.Invalid;
        }

        /// <summary>How much of the cargo bay one unit fills; 0 for an unknown commodity.</summary>
        public static int GetVolume(CommodityType type)
        {
            return GetCommodity(type)?.Volume ?? 0;
        }
    }
}
