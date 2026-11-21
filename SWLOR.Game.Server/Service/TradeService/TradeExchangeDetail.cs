using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.FactionService;

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

        /// <summary>The space area the dock's ships launch into.</summary>
        public string OrbitResref { get; set; }

        /// <summary>A black market pays a premium for restricted and contraband goods (already in its price factors).</summary>
        public bool IsBlackMarket { get; set; }

        /// <summary>The faction a freight contract delivered here earns standing with, or Invalid.</summary>
        public FactionType StandingFaction { get; set; }

        public List<TradeMarketDetail> Markets { get; set; } = new();

        /// <summary>The market for a commodity, or null when this exchange does not trade it.</summary>
        public TradeMarketDetail FindMarket(CommodityType commodity)
        {
            return Markets.FirstOrDefault(x => x.Commodity == commodity);
        }
    }
}
