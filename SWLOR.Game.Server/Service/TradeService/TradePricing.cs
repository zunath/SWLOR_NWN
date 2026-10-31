using System;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// Exchange price math (plan section 13), mirrored by tools/SpaceRevampBalance/trade.py. A unit's mid price is
    /// base x the market's factor x exp(-pressure / depth), inside the flood limits. The exchange sells above mid and
    /// buys below it by the spread, and the trader's Social narrows both slightly. Each unit of an order is priced at
    /// the pressure halfway through it, so splitting an order never changes its total.
    /// </summary>
    public static class TradePricing
    {
        /// <summary>The price multiplier from flooding: below 1 when players have sold here, above 1 when they bought.</summary>
        public static double GetPressureMultiplier(double pressure, int depth)
        {
            var multiplier = Math.Exp(-pressure / Math.Max(1, depth));
            return Math.Clamp(multiplier, TradeTuning.FloodMinimum, TradeTuning.FloodMaximum);
        }

        /// <summary>
        /// A market priced at a multiple of its usual factor, for an event that moves a whole exchange's prices (an
        /// incursion's price spike, plan section 30). A multiplier of 1 returns the market itself.
        /// </summary>
        public static TradeMarketDetail WithPriceMultiplier(TradeMarketDetail market, double multiplier)
        {
            if (market == null || Math.Abs(multiplier - 1.0) < 1e-9)
                return market;

            return new TradeMarketDetail
            {
                Commodity = market.Commodity,
                Role = market.Role,
                PriceFactor = market.PriceFactor * multiplier,
                Depth = market.Depth,
            };
        }

        /// <summary>The fraction Social improves prices by: 0.1% per point over 10, at most 1.5%.</summary>
        public static double GetSocialBonus(int social)
        {
            return Math.Min(TradeTuning.SocialMaximum, Math.Max(0, social - TradeTuning.SocialBase) * TradeTuning.SocialPerPoint);
        }

        /// <summary>
        /// The price of the next unit bought. <paramref name="standingBonus"/> is the fraction faction standing adds to
        /// Social's (see <see cref="TradeStandingRules.GetPriceBonus"/>).
        /// </summary>
        public static double GetUnitBuyPrice(int basePrice, TradeMarketDetail market, double pressure, int social, double standingBonus = 0)
        {
            return GetMid(basePrice, market, pressure) * (1 + TradeTuning.Spread) * (1 - (GetSocialBonus(social) + standingBonus));
        }

        public static double GetUnitSellPrice(int basePrice, TradeMarketDetail market, double pressure, int social, double standingBonus = 0)
        {
            return GetMid(basePrice, market, pressure) * (1 - TradeTuning.Spread) * (1 + GetSocialBonus(social) + standingBonus);
        }

        /// <summary>The credits units cost. Buying removes pressure, so each unit costs a little more than the last.</summary>
        public static int GetBuyCost(int basePrice, TradeMarketDetail market, double pressure, int units, int social, double standingBonus = 0)
        {
            var total = 0.0;
            for (var unit = 0; unit < units; unit++)
            {
                total += GetUnitBuyPrice(basePrice, market, pressure - unit - 0.5, social, standingBonus);
            }

            return (int)Math.Min(int.MaxValue, Math.Ceiling(total));
        }

        /// <summary>The credits units fetch. Selling adds pressure, so each unit fetches a little less than the last.</summary>
        public static int GetSellRevenue(int basePrice, TradeMarketDetail market, double pressure, int units, int social, double standingBonus = 0)
        {
            var total = 0.0;
            for (var unit = 0; unit < units; unit++)
            {
                total += GetUnitSellPrice(basePrice, market, pressure + unit + 0.5, social, standingBonus);
            }

            return (int)Math.Min(int.MaxValue, Math.Floor(total));
        }

        /// <summary>The price the exchange shows for buying one unit now.</summary>
        public static int GetQuotedBuyPrice(int basePrice, TradeMarketDetail market, double pressure, int social, double standingBonus = 0)
        {
            return GetBuyCost(basePrice, market, pressure, 1, social, standingBonus);
        }

        /// <summary>The price the exchange shows for selling one unit now.</summary>
        public static int GetQuotedSellPrice(int basePrice, TradeMarketDetail market, double pressure, int social, double standingBonus = 0)
        {
            return GetSellRevenue(basePrice, market, pressure, 1, social, standingBonus);
        }

        /// <summary>Pressure after it has decayed toward 0 for a while (half-life RecoveryHalfLifeHours).</summary>
        public static double Recover(double pressure, TimeSpan elapsed)
        {
            if (elapsed <= TimeSpan.Zero)
                return pressure;

            var recovered = pressure * Math.Pow(0.5, elapsed.TotalHours / TradeTuning.RecoveryHalfLifeHours);
            return Math.Abs(recovered) < 0.01 ? 0 : recovered;
        }

        /// <summary>Brings a market's pressure up to now.</summary>
        public static void Advance(TradeMarketState state, DateTime now)
        {
            if (state.UpdatedAt == default)
            {
                state.UpdatedAt = now;
                return;
            }

            state.Pressure = Recover(state.Pressure, now - state.UpdatedAt);
            state.UpdatedAt = now;
        }

        private static double GetMid(int basePrice, TradeMarketDetail market, double pressure)
        {
            return basePrice * market.PriceFactor * GetPressureMultiplier(pressure, market.Depth);
        }
    }
}
