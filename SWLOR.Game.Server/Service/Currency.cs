using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.CurrencyService;

namespace SWLOR.Game.Server.Service
{
    public static class Currency
    {
        private static readonly Dictionary<CurrencyType, CurrencyAttribute> _currencies = new();

        /// <summary>
        /// When the module caches, cache all currency details into memory.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheCurrencies()
        {
            var currencyTypes = Enum.GetValues(typeof(CurrencyType)).Cast<CurrencyType>();
            foreach (var currencyType in currencyTypes)
            {
                // Skip over the invalid currencies.
                if (currencyType == CurrencyType.Invalid)
                    continue;

                var detail = currencyType.GetAttribute<CurrencyType, CurrencyAttribute>();
                _currencies[currencyType] = detail;
            }
        }

        /// <summary>
        /// Retrieves the details about a specific currency type.
        /// </summary>
        /// <param name="currencyType">The type of currency to retrieve.</param>
        /// <returns>The details about a currency.</returns>
        public static CurrencyAttribute GetCurrencyDetail(CurrencyType currencyType)
        {
            return _currencies[currencyType];
        }

        /// <summary>
        /// Retrieves the amount of a specific currency held by a player.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="type">The type of currency to look for</param>
        /// <returns>The amount of the currency held.</returns>
        public static int GetCurrency(uint player, CurrencyType type)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (dbPlayer == null)
                return 0;

            if (!dbPlayer.Currencies.ContainsKey(type))
                return 0;

            return dbPlayer.Currencies[type];
        }

        /// <summary>
        /// Gives a specific amount of a type of currency to a player.
        /// </summary>
        /// <param name="player">The player to give currency</param>
        /// <param name="type">The type of currency to give</param>
        /// <param name="amount">The amount of currency to give</param>
        public static void GiveCurrency(uint player, CurrencyType type, int amount)
        {
            amount = Math.Abs(amount);

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (dbPlayer == null)
                return;

            if (!dbPlayer.Currencies.ContainsKey(type))
                dbPlayer.Currencies[type] = 0;

            dbPlayer.Currencies[type] += amount;

            DB.Set(dbPlayer);
            Gui.PublishRefreshEvent(player, new CurrencyRefreshEvent());
        }

        /// <summary>
        /// Takes a specific amount of a type of currency from a player.
        /// </summary>
        /// <param name="player">The player to take currency from</param>
        /// <param name="type">The type of currency to take</param>
        /// <param name="amount">The amount of currency to take</param>
        public static void TakeCurrency(uint player, CurrencyType type, int amount)
        {
            amount = Math.Abs(amount);

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (dbPlayer == null)
                return;

            if (!dbPlayer.Currencies.ContainsKey(type))
                dbPlayer.Currencies[type] = 0;

            dbPlayer.Currencies[type] -= amount;

            DB.Set(dbPlayer);
            Gui.PublishRefreshEvent(player, new CurrencyRefreshEvent());
        }
    }
}
