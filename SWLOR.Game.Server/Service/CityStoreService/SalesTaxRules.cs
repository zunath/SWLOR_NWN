using System;

namespace SWLOR.Game.Server.Service.CityStoreService
{
    /// <summary>
    /// The city Sales tax (Player City Revamp, "Stores and sales tax"): a whole percent from 0 to 10 that the mayor sets,
    /// added to every price in the city's stores and paid into the treasury. Pure, so each rule can be tested.
    /// </summary>
    public static class SalesTaxRules
    {
        public const int MaximumPercent = 10;

        /// <summary>The markup the city store blueprints keep, which the tax rate is added to when a store opens.</summary>
        public const int StandardMarkUpPercent = 100;

        /// <summary>A stored or typed rate kept inside 0 to the cap.</summary>
        public static int ClampPercent(int percent)
        {
            return Math.Clamp(percent, 0, MaximumPercent);
        }

        /// <summary>
        /// The part of a price that is tax: the price already includes it, so it is the price times the rate over the
        /// store's markup plus the rate, rounded down to whole credits.
        /// </summary>
        /// <param name="pricePaid">What the buyer paid, tax included.</param>
        /// <param name="percent">The rate the store was opened with.</param>
        /// <param name="storeMarkUpPercent">The store's own markup, which the rate is added to.</param>
        public static int GetTaxPortion(int pricePaid, int percent, int storeMarkUpPercent = StandardMarkUpPercent)
        {
            percent = ClampPercent(percent);
            if (pricePaid <= 0 || percent <= 0)
                return 0;

            return (int)((long)pricePaid * percent / (Math.Max(0, storeMarkUpPercent) + percent));
        }

        /// <summary>The line a player reads when a city store opens.</summary>
        public static string Describe(int percent)
        {
            percent = ClampPercent(percent);
            return percent <= 0
                ? "This city charges no sales tax."
                : $"Prices include this city's {percent}% sales tax.";
        }
    }
}
