using System;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// Plans a purchase or sale at an exchange (plan section 13) and the Piloting XP a profitable sale gives (plan
    /// section 4, XP rules). An order asking for more than fits, is affordable or is aboard trades what it can.
    /// </summary>
    public static class TradeRules
    {
        /// <summary>The most units one order may ask for.</summary>
        public const int MaximumOrderUnits = 10000;

        public static TradeOrder PlanBuy(
            CommodityDetail commodity,
            TradeMarketDetail market,
            double pressure,
            int social,
            int requestedUnits,
            int capacity,
            int freeVolume,
            int credits,
            double standingBonus = 0,
            int tariffPercent = 0)
        {
            if (requestedUnits <= 0 || requestedUnits > MaximumOrderUnits)
                return TradeOrder.Refused(TradeRefusal.InvalidAmount);
            if (commodity == null || market == null)
                return TradeOrder.Refused(TradeRefusal.NotTradedHere);
            if (commodity.IsHoldingGood)
                return TradeOrder.Refused(TradeRefusal.HoldingsOnly);
            if (capacity <= 0)
                return TradeOrder.Refused(TradeRefusal.NoCargoBay);

            var units = Math.Min(requestedUnits, Math.Max(0, freeVolume) / Math.Max(1, commodity.Volume));
            if (units <= 0)
                return TradeOrder.Refused(TradeRefusal.CargoBayFull);

            var cost = TradePricing.GetBuyCost(commodity.BasePrice, market, pressure, units, social, standingBonus);
            if (cost + TradeTariffRules.GetTariff(cost, tariffPercent) > credits)
            {
                units = GetAffordableUnits(commodity, market, pressure, social, standingBonus, tariffPercent, units, credits);
                if (units <= 0)
                    return TradeOrder.Refused(TradeRefusal.NotEnoughCredits);

                cost = TradePricing.GetBuyCost(commodity.BasePrice, market, pressure, units, social, standingBonus);
            }

            var tariff = TradeTariffRules.GetTariff(cost, tariffPercent);
            return new TradeOrder { Units = units, Credits = cost + tariff, Tariff = tariff, WasReduced = units < requestedUnits };
        }

        public static TradeOrder PlanSell(
            CommodityDetail commodity,
            TradeMarketDetail market,
            double pressure,
            int social,
            int requestedUnits,
            int unitsAboard,
            double standingBonus = 0,
            int tariffPercent = 0)
        {
            if (requestedUnits <= 0 || requestedUnits > MaximumOrderUnits)
                return TradeOrder.Refused(TradeRefusal.InvalidAmount);
            if (commodity == null || market == null)
                return TradeOrder.Refused(TradeRefusal.NotTradedHere);
            if (unitsAboard <= 0)
                return TradeOrder.Refused(TradeRefusal.NoneAboard);

            var units = Math.Min(requestedUnits, unitsAboard);
            var revenue = TradePricing.GetSellRevenue(commodity.BasePrice, market, pressure, units, social, standingBonus);
            var tariff = TradeTariffRules.GetTariff(revenue, tariffPercent);
            return new TradeOrder { Units = units, Credits = revenue - tariff, Tariff = tariff, WasReduced = units < requestedUnits };
        }

        /// <summary>What buying one unit costs the player now, tariff included (the price the exchange window shows).</summary>
        public static int GetQuotedBuyPrice(CommodityDetail commodity, TradeMarketDetail market, double pressure, int social, double standingBonus, int tariffPercent)
        {
            var price = TradePricing.GetQuotedBuyPrice(commodity.BasePrice, market, pressure, social, standingBonus);
            return price + TradeTariffRules.GetTariff(price, tariffPercent);
        }

        /// <summary>What selling one unit pays the player now, tariff taken (the price the exchange window shows).</summary>
        public static int GetQuotedSellPrice(CommodityDetail commodity, TradeMarketDetail market, double pressure, int social, double standingBonus, int tariffPercent)
        {
            var price = TradePricing.GetQuotedSellPrice(commodity.BasePrice, market, pressure, social, standingBonus);
            return price - TradeTariffRules.GetTariff(price, tariffPercent);
        }

        /// <summary>Piloting XP for a sale: 1 per credit of profit over what the units cost, at most two kills' worth.</summary>
        public static int GetSaleXp(int profit)
        {
            if (profit <= 0)
                return 0;

            return (int)Math.Min(TradeTuning.XpMaximumPerSale, Math.Floor(profit * TradeTuning.XpPerProfitCredit));
        }

        /// <summary>The red text a refused order shows.</summary>
        public static string GetRefusalMessage(TradeRefusal refusal)
        {
            return refusal switch
            {
                TradeRefusal.InvalidAmount => $"Enter a number of units from 1 to {MaximumOrderUnits}.",
                TradeRefusal.NotTradedHere => "This exchange does not trade that commodity.",
                TradeRefusal.NoCargoBay => "This ship has no cargo bay.",
                TradeRefusal.CargoBayFull => "The ship's cargo bay has no room for that.",
                TradeRefusal.NotEnoughCredits => "You do not have enough credits.",
                TradeRefusal.NoneAboard => "The ship carries none of that commodity.",
                TradeRefusal.ShipNotDocked => "That ship is not docked here.",
                TradeRefusal.NoExchangeHere => "This starport has no Trade Exchange.",
                TradeRefusal.HoldingsOnly => "Exchanges do not sell that. Only Planetary Holdings make it.",
                _ => string.Empty,
            };
        }

        private static int GetAffordableUnits(
            CommodityDetail commodity,
            TradeMarketDetail market,
            double pressure,
            int social,
            double standingBonus,
            int tariffPercent,
            int maximum,
            int credits)
        {
            int low = 0, high = maximum;
            while (low < high)
            {
                var middle = (low + high + 1) / 2;
                var cost = TradePricing.GetBuyCost(commodity.BasePrice, market, pressure, middle, social, standingBonus);
                if (cost + TradeTariffRules.GetTariff(cost, tariffPercent) <= credits)
                    low = middle;
                else
                    high = middle - 1;
            }

            return low;
        }
    }
}
