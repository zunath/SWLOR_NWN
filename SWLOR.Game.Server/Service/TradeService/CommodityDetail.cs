using System;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>A commodity's fixed data. Rows come from the generated TradeCatalog.</summary>
    public class CommodityDetail
    {
        public CommodityType Type { get; set; }
        public string Name { get; set; }

        /// <summary>The price of one unit at a neutral exchange with no flooding, before the spread.</summary>
        public int BasePrice { get; set; }

        /// <summary>How much of the cargo bay one unit fills.</summary>
        public int Volume { get; set; }

        public TradeAuthority[] BannedBy { get; set; } = Array.Empty<TradeAuthority>();
        public TradeExchangeType[] Producers { get; set; } = Array.Empty<TradeExchangeType>();
        public TradeExchangeType[] Consumers { get; set; } = Array.Empty<TradeExchangeType>();
        public string Description { get; set; }

        /// <summary>Contraband when every lawful authority bans it, restricted when some do.</summary>
        public CommodityLegality Legality => BannedBy.Length == 0
            ? CommodityLegality.Legal
            : BannedBy.Length >= 3 ? CommodityLegality.Contraband : CommodityLegality.Restricted;
    }
}
