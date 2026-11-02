using System.Linq;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.TradeDefinition;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.IndustryService
{
    /// <summary>
    /// The Shadowport Edict and the lawful port it replaces (Player City Revamp, "Edict catalog"). A city door of a lawful exchange
    /// buys the goods its authority bans, which the shared market does not trade: near black-market prices with a small bust chance
    /// under Shadowport, low prices with a high bust chance without it. A bust is the customs result: a fine, the banned cargo
    /// confiscated, and lawful standing lost. Under Shadowport normal goods sell 15% lower. An open port keeps its market's rules.
    /// The rules never touch the shared market's pressure or its buy prices.
    /// </summary>
    public static class ShadowportRules
    {
        private static readonly Dictionary<CommodityType, TradeMarketDetail> _blackMarketReference = BuildBlackMarketReference();

        public static ShadowportStance GetStance(TradeAuthority exchangeAuthority, bool shadowportInEffect)
        {
            if (exchangeAuthority == TradeAuthority.None)
                return ShadowportStance.OpenPort;

            return shadowportInEffect ? ShadowportStance.Shadowport : ShadowportStance.Lawful;
        }

        /// <summary>
        /// Whether a city whose Trade Exchange trades through this exchange is at an open port, which has no customs authority and
        /// no exchange for an unknown planet. Shadowport changes nothing there, so such a city cannot enact it.
        /// </summary>
        public static bool IsOpenPort(TradeExchangeType exchange)
        {
            return (TradeLookup.GetExchange(exchange)?.Authority ?? TradeAuthority.None) == TradeAuthority.None;
        }

        /// <summary>The share of the usual price normal goods sell for: 0.85 under Shadowport, otherwise 1.</summary>
        public static double GetNormalGoodsSaleFactor(ShadowportStance stance)
        {
            return stance == ShadowportStance.Shadowport ? ShadowportTuning.NormalGoodsSaleFactor : 1.0;
        }

        /// <summary>The share of the black-market price contraband sells for, or 0 at an open port.</summary>
        public static double GetPriceShare(ShadowportStance stance)
        {
            return stance switch
            {
                ShadowportStance.Shadowport => ShadowportTuning.ShadowportPriceShare,
                ShadowportStance.Lawful => ShadowportTuning.LawfulPriceShare,
                _ => 0.0,
            };
        }

        /// <summary>
        /// The market a black market prices a good by: the lowest price factor among the black markets that do not make it, or null
        /// when no black market buys it.
        /// </summary>
        public static TradeMarketDetail GetBlackMarketReference(CommodityType commodity)
        {
            return _blackMarketReference.TryGetValue(commodity, out var market) ? market : null;
        }

        /// <summary>
        /// The market a lawful door buys a banned good by: the black market's depth and price factor times the stance's share. Null at
        /// an open port and for a good no black market buys.
        /// </summary>
        public static TradeMarketDetail GetContrabandMarket(CommodityType commodity, ShadowportStance stance)
        {
            var share = GetPriceShare(stance);
            var reference = GetBlackMarketReference(commodity);
            if (share <= 0 || reference == null)
                return null;

            return new TradeMarketDetail
            {
                Commodity = commodity,
                Role = CommodityRole.Consumer,
                PriceFactor = reference.PriceFactor * share,
                Depth = reference.Depth,
            };
        }

        /// <summary>
        /// The goods a lawful door buys only as contraband: those its authority bans that its shared market does not trade. Empty at
        /// an open port.
        /// </summary>
        public static List<CommodityType> GetContrabandCommodities(TradeExchangeDetail exchange, ShadowportStance stance)
        {
            if (exchange == null || stance == ShadowportStance.OpenPort)
                return new List<CommodityType>();

            return TradeCatalog.Commodities
                .Where(commodity => CustomsRules.IsBanned(commodity.Type, exchange.Authority) &&
                                    exchange.FindMarket(commodity.Type) == null &&
                                    GetBlackMarketReference(commodity.Type) != null)
                .Select(commodity => commodity.Type)
                .OrderBy(type => TradeLookup.GetCommodity(type)?.Name, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>Whether a sale of this good at a door of this exchange is a contraband sale.</summary>
        public static bool IsContrabandSale(TradeExchangeDetail exchange, CommodityType commodity, ShadowportStance stance)
        {
            return exchange != null &&
                   stance != ShadowportStance.OpenPort &&
                   CustomsRules.IsBanned(commodity, exchange.Authority) &&
                   exchange.FindMarket(commodity) == null &&
                   GetBlackMarketReference(commodity) != null;
        }

        /// <summary>
        /// The chance a sale of contraband is busted: the chance one customs patrol scan would find the cargo aboard (the cargo's
        /// banned base value, after a compartment's cut and the pilot's standing), times the stance's multiple.
        /// </summary>
        public static double GetBustChance(int bannedValue, double compartmentPercent, double standingCutPercent, ShadowportStance stance)
        {
            if (stance == ShadowportStance.OpenPort || bannedValue <= 0)
                return 0.0;

            var multiple = stance == ShadowportStance.Shadowport ? ShadowportTuning.ShadowportBustMultiple : ShadowportTuning.LawfulBustMultiple;
            var scan = CustomsRules.GetScanChance(bannedValue, compartmentPercent, standingCutPercent);
            return Math.Clamp(scan * multiple, 0.0, ShadowportTuning.BustMaximum);
        }

        /// <summary>The exchange window's line about the door, or empty at an open port.</summary>
        public static string Describe(ShadowportStance stance)
        {
            return stance switch
            {
                ShadowportStance.Shadowport => "Shadowport: contraband near black-market price, small bust risk; goods 15% lower.",
                ShadowportStance.Lawful => "Lawful port: contraband sells low, with a high chance of a customs bust.",
                _ => string.Empty,
            };
        }

        private static Dictionary<CommodityType, TradeMarketDetail> BuildBlackMarketReference()
        {
            var reference = new Dictionary<CommodityType, TradeMarketDetail>();
            foreach (var exchange in TradeCatalog.Exchanges.Where(x => x.IsBlackMarket))
            {
                foreach (var market in exchange.Markets.Where(x => x.Role != CommodityRole.Producer))
                {
                    var commodity = TradeLookup.GetCommodity(market.Commodity);
                    if (commodity == null || commodity.Producers.Contains(exchange.Type) || commodity.HoldingSources.Contains(exchange.Type))
                        continue;

                    if (!reference.TryGetValue(market.Commodity, out var lowest) || market.PriceFactor < lowest.PriceFactor)
                        reference[market.Commodity] = market;
                }
            }

            return reference;
        }
    }
}
