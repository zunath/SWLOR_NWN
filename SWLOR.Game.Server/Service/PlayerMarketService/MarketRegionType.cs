using System;

namespace SWLOR.Game.Server.Service.PlayerMarketService
{
    public enum MarketRegionType
    {
        [MarketRegion("Invalid", "", false, false, 0.0f)]
        Invalid = 0,
        [MarketRegion("Global", "MARKET_GLOBAL", true, true, 0.09f)]
        Global = 1
    }

    public class MarketRegionAttribute : Attribute
    {
        public string Name { get; }
        public string MarketId { get; }
        public bool IsActive { get; }
        public bool IsStandardMarket { get; }
        public float TaxRate { get; }

        public MarketRegionAttribute(
            string name, 
            string marketId, 
            bool isActive, 
            bool isStandardMarket,
            float taxRate)
        {
            Name = name;
            MarketId = marketId;
            IsActive = isActive;
            IsStandardMarket = isStandardMarket;
            TaxRate = taxRate;
        }
    }
}
