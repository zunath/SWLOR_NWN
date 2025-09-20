using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CurrenciesViewModel: GuiViewModelBase<CurrenciesViewModel, GuiPayloadBase>,
        IGuiRefreshable<CurrencyRefreshEvent>
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();

        public GuiBindingList<string> CurrencyNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<int> CurrencyValues
        {
            get => Get<GuiBindingList<int>>();
            set => Set(value);
        }

        private void LoadData()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = _db.Get<Player>(playerId);

            var currencyNames = new GuiBindingList<string>();
            var currencyValues = new GuiBindingList<int>();

            foreach (var (currency, value) in dbPlayer.Currencies)
            {
                var detail = Currency.GetCurrencyDetail(currency);

                currencyNames.Add(detail.Name);
                currencyValues.Add(value);
            }

            CurrencyNames = currencyNames;
            CurrencyValues = currencyValues;
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            LoadData();
        }

        public void Refresh(CurrencyRefreshEvent payload)
        {
            LoadData();
        }
    }
}
