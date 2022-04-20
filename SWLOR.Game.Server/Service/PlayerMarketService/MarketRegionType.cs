using System;

namespace SWLOR.Game.Server.Service.PlayerMarketService
{
    public enum MarketRegionType
    {
        [MarketRegion("Invalid", "", false, false, 0.0f)]
        Invalid = 0,
        [MarketRegion("Viscara", "MARKET_VISCARA", true, true, 0.09f)]
        Viscara = 1,
        [MarketRegion("Mon Cala", "MARKET_MON_CALA", true, true, 0.09f)]
        MonCala = 2,
        [MarketRegion("Hutlar", "MARKET_HUTLAR", true, true, 0.09f)]
        Hutlar = 3,
        [MarketRegion("Tatooine", "MARKET_TATOOINE", true, true, 0.09f)]
        Tatooine = 4,

        [MarketRegion("Player", "MARKET_PLAYER", true, false, 0.0f)]
        Player = 99
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
