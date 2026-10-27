using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>A dock's exchange and every commodity it trades. Rows come from the generated TradeCatalog.</summary>
    public class TradeExchangeDetail
    {
        public TradeExchangeType Type { get; set; }
        public string Name { get; set; }
        public TradeAuthority Authority { get; set; }
        public int OrbitTier { get; set; }

        /// <summary>The planets whose NPC starports open this exchange.</summary>
        public PlanetType[] PlanetTypes { get; set; } = Array.Empty<PlanetType>();

        public List<TradeMarketDetail> Markets { get; set; } = new();

        /// <summary>The market for a commodity, or null when this exchange does not trade it.</summary>
        public TradeMarketDetail FindMarket(CommodityType commodity)
        {
            return Markets.FirstOrDefault(x => x.Commodity == commodity);
        }
    }
}
