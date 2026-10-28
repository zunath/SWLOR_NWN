using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.TradeDefinition;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// The HoloNet price board (plan section 13): every commodity's cheapest exchange and best-paying exchange, from a
    /// snapshot that is rebuilt at most once per refresh period, so the board runs up to that long stale. Prices are
    /// quoted for one unit at Social 10.
    /// </summary>
    public static class HoloNetRules
    {
        /// <summary>Whether a snapshot taken then must be rebuilt now.</summary>
        public static bool IsStale(DateTime capturedAt, DateTime now)
        {
            return capturedAt == default || now - capturedAt >= TimeSpan.FromMinutes(TradeTuning.HoloNetRefreshMinutes);
        }

        /// <summary>How old the snapshot is, in whole minutes.</summary>
        public static int GetAgeMinutes(DateTime capturedAt, DateTime now)
        {
            return capturedAt == default ? 0 : Math.Max(0, (int)Math.Floor((now - capturedAt).TotalMinutes));
        }

        /// <summary>
        /// What the board's prices depend on besides flooding: the running incursion (its region's exchanges price at the
        /// spike), or an empty string when none runs. A board built under another key is rebuilt at once.
        /// </summary>
        public static string GetPriceKey(SpaceService.ActiveIncursion incursion) =>
            incursion == null ? string.Empty : $"{incursion.Center}|{incursion.StartedAt.Ticks}";

        /// <summary>
        /// The board from each exchange's pressure on each commodity (missing means unflooded) and its price multiplier
        /// (the incursion spike; 1 when <paramref name="priceMultiplier"/> is null).
        /// </summary>
        public static HoloNetSnapshot Build(
            Func<TradeExchangeType, CommodityType, double> pressure,
            DateTime now,
            Func<TradeExchangeType, double> priceMultiplier = null)
        {
            var rows = new List<HoloNetRow>();
            foreach (var commodity in TradeCatalog.Commodities.OrderBy(x => x.Name))
            {
                var quotes = TradeCatalog.Exchanges
                    .Select(exchange => (exchange.Type, Market: TradePricing.WithPriceMultiplier(
                        exchange.FindMarket(commodity.Type), priceMultiplier?.Invoke(exchange.Type) ?? 1.0)))
                    .Where(x => x.Market != null)
                    .Select(x => (x.Type,
                        Buy: TradePricing.GetQuotedBuyPrice(commodity.BasePrice, x.Market, pressure(x.Type, commodity.Type), TradeTuning.SocialBase),
                        Sell: TradePricing.GetQuotedSellPrice(commodity.BasePrice, x.Market, pressure(x.Type, commodity.Type), TradeTuning.SocialBase)))
                    .ToList();
                if (quotes.Count == 0)
                    continue;

                var cheapest = quotes.OrderBy(x => x.Buy).ThenBy(x => x.Type).First();
                var best = quotes.OrderByDescending(x => x.Sell).ThenBy(x => x.Type).First();
                rows.Add(new HoloNetRow
                {
                    Commodity = commodity.Type,
                    Name = commodity.Name,
                    Legality = commodity.Legality,
                    BuyAt = commodity.IsHoldingGood ? TradeExchangeType.Invalid : cheapest.Type,
                    BuyPrice = commodity.IsHoldingGood ? 0 : cheapest.Buy,
                    SellAt = best.Type,
                    SellPrice = best.Sell,
                });
            }

            return new HoloNetSnapshot { CapturedAt = now, Rows = rows };
        }
    }
}
