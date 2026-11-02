using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public partial class ManageCityViewModel
    {
        public const string TabContentElement = "managecity_content";
        public const string CityTabPartial = "MANAGECITY_CITY";
        public const string EdictsTabPartial = "MANAGECITY_EDICTS";
        public const string SuccessionTabPartial = "MANAGECITY_SUCCESSION";

        private const int CityTabId = 0;
        private const int EdictsTabId = 1;
        private const int SuccessionTabId = 2;

        private static readonly GuiTabGroup<ManageCityViewModel, GuiPayloadBase> Tabs =
            new GuiTabGroup<ManageCityViewModel, GuiPayloadBase>()
                .AddTab(CityTabId, CityTabPartial)
                .AddTab(EdictsTabId, EdictsTabPartial, model => model.RefreshEdictsTab())
                .AddTab(SuccessionTabId, SuccessionTabPartial, model => model.RefreshSuccessionTab());

        private static readonly GuiToggleGroupSync TabToggles = new(CityTabId, EdictsTabId, SuccessionTabId);

        public int SelectedTabId
        {
            get => Get<int>();
            set => Set(value);
        }

        // Only a genuine click on the tab strip changes tabs; the echo of a programmatic change is ignored.
        public int TabToggleValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                TabToggles.HandleClientChange(value, SelectTab);
            }
        }

        private void InitializeTabs()
        {
            TabToggleValue = CityTabId;
            SelectedTabId = CityTabId;
            InitializeEdictsTab();
            InitializeSuccessionTab();
            InitializeStreetLife();
        }

        private void SelectTab(int tabId)
        {
            SelectedTabId = tabId;
            TabToggles.SyncTo(tabId, value => TabToggleValue = value);
            Tabs.Select(this, TabContentElement, tabId, RestoreCityInputs);
        }

        // Closing a modal rebuilds the window, so the selected tab is selected again to refresh what the modal changed.
        protected override void OnModalClosedRestore() => Tabs.Select(this, TabContentElement, SelectedTabId, RestoreCityInputs);

        /// <summary>
        /// Pushes the City tab's text fields again once its layout exists, since the client does not keep what was typed
        /// into a field that was off screen.
        /// </summary>
        private void RestoreCityInputs()
        {
            if (SelectedTabId != CityTabId)
                return;

            CityName = CityName;
            TransportationTax = TransportationTax;
            CitizenshipTax = CitizenshipTax;
            ExchangeTariff = ExchangeTariff;
            ShipyardFee = ShipyardFee;
        }
    }
}
