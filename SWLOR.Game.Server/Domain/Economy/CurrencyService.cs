using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CurrencyService;

namespace SWLOR.Game.Server.Domain.Economy
{
    /// <summary>
    /// The currencies a player holds: the logic and state that used to be static in <c>Service.Currency</c>, moved here
    /// unchanged. The facade still carries the event handler and forwards every call to the container's instance.
    /// </summary>
    public class CurrencyService : ICurrencyService
    {
        private readonly IDBService _db;
        private readonly Dictionary<CurrencyType, CurrencyAttribute> _currencies = new Dictionary<CurrencyType, CurrencyAttribute>();

        public CurrencyService(IDBService db)
        {
            _db = db;
        }

        public void CacheCurrencies()
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

        public CurrencyAttribute GetCurrencyDetail(CurrencyType currencyType)
        {
            return _currencies[currencyType];
        }

        public int GetCurrency(uint player, CurrencyType type)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (dbPlayer == null)
                return 0;

            if (!dbPlayer.Currencies.ContainsKey(type))
                return 0;

            return dbPlayer.Currencies[type];
        }

        public void GiveCurrency(uint player, CurrencyType type, int amount)
        {
            amount = Math.Abs(amount);

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (dbPlayer == null)
                return;

            if (!dbPlayer.Currencies.ContainsKey(type))
                dbPlayer.Currencies[type] = 0;

            dbPlayer.Currencies[type] += amount;

            _db.Set(dbPlayer);
            Gui.PublishRefreshEvent(player, new CurrencyRefreshEvent());
        }

        public void TakeCurrency(uint player, CurrencyType type, int amount)
        {
            amount = Math.Abs(amount);

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (dbPlayer == null)
                return;

            if (!dbPlayer.Currencies.ContainsKey(type))
                dbPlayer.Currencies[type] = 0;

            var balance = dbPlayer.Currencies[type];
            var newBalance = Math.Max(0, balance - amount);
            if (newBalance == balance)
                return;

            dbPlayer.Currencies[type] = newBalance;

            _db.Set(dbPlayer);
            Gui.PublishRefreshEvent(player, new CurrencyRefreshEvent());
        }
    }
}
