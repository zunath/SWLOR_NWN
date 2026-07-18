using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Component;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CurrenciesViewModel: GuiViewModelBase<CurrenciesViewModel, GuiPayloadBase>,
        IGuiRefreshable<CurrencyRefreshEvent>
    {
        // One row DTO per currency, replacing the two hand-synced parallel
        // GuiBindingList instances LoadData used to build in lockstep.
        private sealed class CurrencyEntry
        {
            public string Name { get; }
            public int Value { get; }

            public CurrencyEntry(string name, int value)
            {
                Name = name;
                Value = value;
            }
        }

        private static readonly GuiTableSource<CurrenciesViewModel, CurrencyEntry> CurrenciesTable =
            new GuiTableSource<CurrenciesViewModel, CurrencyEntry>()
                .Column((m, v) => m.CurrencyNames = v, r => r.Name)
                .Column((m, v) => m.CurrencyValues = v, r => r.Value);

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
            var dbPlayer = DB.Get<Player>(playerId);

            var rows = new List<CurrencyEntry>();

            foreach (var (currency, value) in dbPlayer.Currencies)
            {
                var detail = Currency.GetCurrencyDetail(currency);
                rows.Add(new CurrencyEntry(detail.Name, value));
            }

            CurrenciesTable.Refresh(this, rows);
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
