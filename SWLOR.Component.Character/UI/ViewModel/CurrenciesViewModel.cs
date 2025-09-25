using SWLOR.Component.Character.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Character.UI.ViewModel
{
    public class CurrenciesViewModel: GuiViewModelBase<CurrenciesViewModel, IGuiPayload>,
        IGuiRefreshable<CurrencyRefreshEvent>
    {
        private readonly IDatabaseService _db;
        private readonly ICurrencyService _currencyService;

        public CurrenciesViewModel(
            IGuiService guiService, 
            IDatabaseService db,
            ICurrencyService currencyService) : base(guiService)
        {
            _db = db;
            _currencyService = currencyService;
        }

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
                var detail = _currencyService.GetCurrencyDetail(currency);

                currencyNames.Add(detail.Name);
                currencyValues.Add(value);
            }

            CurrencyNames = currencyNames;
            CurrencyValues = currencyValues;
        }

        protected override void Initialize(IGuiPayload initialPayload)
        {
            LoadData();
        }

        public void Refresh(CurrencyRefreshEvent payload)
        {
            LoadData();
        }
    }
}
