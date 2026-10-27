using SWLOR.Game.Server.Service.CurrencyService;

namespace SWLOR.Game.Server.Domain.Economy
{
    /// <summary>
    /// The currencies a player holds. The static facade <c>Service.Currency</c> forwards to the container's instance of
    /// this interface and keeps the event handler.
    /// </summary>
    public interface ICurrencyService
    {
        /// <summary>
        /// When the module caches, cache all currency details into memory.
        /// </summary>
        void CacheCurrencies();

        /// <summary>
        /// Retrieves the details about a specific currency type.
        /// </summary>
        /// <param name="currencyType">The type of currency to retrieve.</param>
        /// <returns>The details about a currency.</returns>
        CurrencyAttribute GetCurrencyDetail(CurrencyType currencyType);

        /// <summary>
        /// Retrieves the amount of a specific currency held by a player.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="type">The type of currency to look for</param>
        /// <returns>The amount of the currency held.</returns>
        int GetCurrency(uint player, CurrencyType type);

        /// <summary>
        /// Gives a specific amount of a type of currency to a player.
        /// </summary>
        /// <param name="player">The player to give currency</param>
        /// <param name="type">The type of currency to give</param>
        /// <param name="amount">The amount of currency to give</param>
        void GiveCurrency(uint player, CurrencyType type, int amount);

        /// <summary>
        /// Takes a specific amount of a type of currency from a player.
        /// If the balance is less than the amount, it is reduced to zero (never below zero).
        /// </summary>
        /// <param name="player">The player to take currency from</param>
        /// <param name="type">The type of currency to take</param>
        /// <param name="amount">The amount of currency to take</param>
        void TakeCurrency(uint player, CurrencyType type, int amount);
    }
}
