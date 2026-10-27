using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Service.CurrencyService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the currencies. The logic and state live in <see cref="ICurrencyService"/>; every member here
    /// forwards to the container's instance, and the event handler stays here so its name in the handler order manifest
    /// does not change.
    /// </summary>
    public static class Currency
    {
        private static ICurrencyService Instance => FacadeAccessor.Resolve<ICurrencyService>();

        /// <summary>
        /// When the module caches, cache all currency details into memory.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheCurrencies()
        {
            Instance.CacheCurrencies();
        }

        /// <summary>
        /// Retrieves the details about a specific currency type.
        /// </summary>
        /// <param name="currencyType">The type of currency to retrieve.</param>
        /// <returns>The details about a currency.</returns>
        public static CurrencyAttribute GetCurrencyDetail(CurrencyType currencyType)
        {
            return Instance.GetCurrencyDetail(currencyType);
        }

        /// <summary>
        /// Retrieves the amount of a specific currency held by a player.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="type">The type of currency to look for</param>
        /// <returns>The amount of the currency held.</returns>
        public static int GetCurrency(uint player, CurrencyType type)
        {
            return Instance.GetCurrency(player, type);
        }

        /// <summary>
        /// Gives a specific amount of a type of currency to a player.
        /// </summary>
        /// <param name="player">The player to give currency</param>
        /// <param name="type">The type of currency to give</param>
        /// <param name="amount">The amount of currency to give</param>
        public static void GiveCurrency(uint player, CurrencyType type, int amount)
        {
            Instance.GiveCurrency(player, type, amount);
        }

        /// <summary>
        /// Takes a specific amount of a type of currency from a player.
        /// If the balance is less than the amount, it is reduced to zero (never below zero).
        /// </summary>
        /// <param name="player">The player to take currency from</param>
        /// <param name="type">The type of currency to take</param>
        /// <param name="amount">The amount of currency to take</param>
        public static void TakeCurrency(uint player, CurrencyType type, int amount)
        {
            Instance.TakeCurrency(player, type, amount);
        }
    }
}
