using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CurrenciesViewModel: GuiViewModelBase<CurrenciesViewModel, GuiPayloadBase>,
        IGuiRefreshable<CurrencyRefreshEvent>
    {
        private readonly List<CurrencyType> _currencyTypes = new();

        public GuiBindingList<string> CurrencyIcons
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> CurrencyNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> CurrencyDescriptions
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> CurrencyAmountText
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> CurrencyHasUse
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<bool> CurrencyCanUse
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        private void LoadData()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            var currencyIcons = new GuiBindingList<string>();
            var currencyNames = new GuiBindingList<string>();
            var currencyDescriptions = new GuiBindingList<string>();
            var currencyAmountText = new GuiBindingList<string>();
            var currencyHasUse = new GuiBindingList<bool>();
            var currencyCanUse = new GuiBindingList<bool>();

            _currencyTypes.Clear();
            _currencyTypes.AddRange(Enum.GetValues(typeof(CurrencyType))
                .Cast<CurrencyType>()
                .Where(type => type != CurrencyType.Invalid));

            foreach (var currency in _currencyTypes)
            {
                var detail = Currency.GetCurrencyDetail(currency);
                var amount = dbPlayer.Currencies.TryGetValue(currency, out var value) ? value : 0;

                currencyIcons.Add(detail.IconResref);
                currencyNames.Add(detail.Name);
                currencyDescriptions.Add(detail.Description);
                currencyAmountText.Add(amount.ToString("N0", CultureInfo.InvariantCulture));
                // Only Language Tokens are spent from this window; the others are spent elsewhere.
                var hasUse = currency == CurrencyType.LanguageToken;
                currencyHasUse.Add(hasUse);
                currencyCanUse.Add(hasUse && amount > 0);
            }

            CurrencyIcons = currencyIcons;
            CurrencyNames = currencyNames;
            CurrencyDescriptions = currencyDescriptions;
            CurrencyAmountText = currencyAmountText;
            CurrencyHasUse = currencyHasUse;
            CurrencyCanUse = currencyCanUse;
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            LoadData();
        }

        public void Refresh(CurrencyRefreshEvent payload)
        {
            LoadData();
        }

        public Action OnClickUse() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _currencyTypes.Count)
                return;

            if (_currencyTypes[index] == CurrencyType.LanguageToken &&
                Currency.GetCurrency(Player, CurrencyType.LanguageToken) > 0 &&
                !Gui.IsWindowOpen(Player, GuiWindowType.LanguageToken))
            {
                Gui.TogglePlayerWindow(Player, GuiWindowType.LanguageToken);
            }
        };
    }
}
