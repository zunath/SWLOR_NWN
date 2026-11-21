using System;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// The tariff of a player city's Trade Exchange (plan section 13, "City hubs"), mirrored by
    /// tools/SpaceRevampBalance/trade.py. A city starport trades through its planet's NPC exchange from a starport
    /// upgrade level; its leadership sets a tariff of 0 to <see cref="TradeTuning.TariffMaximumPercent"/> percent that
    /// is added to every purchase and taken from every sale, and paid into the city treasury.
    /// </summary>
    public static class TradeTariffRules
    {
        /// <summary>A stored or typed tariff kept inside 0 to the cap.</summary>
        public static int ClampPercent(int percent)
        {
            return Math.Clamp(percent, 0, TradeTuning.TariffMaximumPercent);
        }

        /// <summary>The tariff on a purchase or sale of this many credits: rounded up, never more than the credits.</summary>
        public static int GetTariff(int credits, int percent)
        {
            percent = ClampPercent(percent);
            if (credits <= 0 || percent <= 0)
                return 0;

            return (int)Math.Min(credits, Math.Ceiling(credits * (long)percent / 100.0));
        }

        /// <summary>Whether a starport of this effective upgrade level has a Trade Exchange.</summary>
        public static bool IsExchangeAvailable(int starportLevel)
        {
            return starportLevel >= TradeTuning.CityExchangeStarportLevel;
        }

        /// <summary>The exchange window's tariff line.</summary>
        public static string Describe(int percent, string cityName)
        {
            percent = ClampPercent(percent);
            return percent <= 0
                ? $"{cityName} charges no tariff."
                : $"City tariff: {percent}% ({cityName}), added to purchases and taken from sales.";
        }
    }
}
