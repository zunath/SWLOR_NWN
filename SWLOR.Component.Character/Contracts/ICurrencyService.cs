using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Character.Contracts
{
    public interface ICurrencyService
    {
        void CacheCurrencies();
        CurrencyAttribute GetCurrencyDetail(CurrencyType currencyType);
        int GetCurrency(uint player, CurrencyType type);
        void GiveCurrency(uint player, CurrencyType type, int amount);
        void TakeCurrency(uint player, CurrencyType type, int amount);
    }
}
