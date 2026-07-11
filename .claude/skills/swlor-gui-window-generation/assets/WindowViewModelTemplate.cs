// TEMPLATE: copy to SWLOR.Game.Server/Feature/GuiDefinition/ViewModel/<YourWindow>ViewModel.cs
// and rename every "TemplateWindow" token. Delete markers as you fill them.
using System;
using SWLOR.Game.Server.Feature.GuiDefinition.Component;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class TemplateWindowViewModel : GuiViewModelBase<TemplateWindowViewModel, GuiPayloadBase>
    {
        private const int FirstTabId = 0;
        private const int SecondTabId = 1;
        public const string TabContentElement = "templatewindow_tab_content"; // TEMPLATE: unique element id
        public const string FirstTabPartial = "TEMPLATEWINDOW_TAB_FIRST";     // TEMPLATE: unique partial names
        public const string SecondTabPartial = "TEMPLATEWINDOW_TAB_SECOND";

        // Rule R4: tab registration + toggle sync are static and shared.
        private static readonly GuiTabGroup<TemplateWindowViewModel, GuiPayloadBase> Tabs =
            new GuiTabGroup<TemplateWindowViewModel, GuiPayloadBase>()
                .AddTab(FirstTabId, FirstTabPartial)
                .AddTab(SecondTabId, SecondTabPartial, m => m.RefreshSecondTab());

        private static readonly GuiToggleGroupSync TabToggles = new(FirstTabId, SecondTabId);

        public int SelectedTabId { get => Get<int>(); set => Set(value); }

        // Rule R4: this widget-bound property only forwards genuine user clicks.
        public int TabToggleValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                TabToggles.HandleClientChange(value, SelectTab);
            }
        }

        // TEMPLATE: one bound property per dynamic value in the widget inventory.
        // Use GuiBindingList<T> for list/table columns and chart data.
        public string StatusText { get => Get<string>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            // Rule R3: assign EVERY bound property before any WatchOnClient call.
            StatusText = "Ready.";   // TODO: data plumbing
            TabToggleValue = 0;

            // TEMPLATE: watch every property the CLIENT can change (inputs,
            // checkboxes, combos, sliders, toggles, color pickers).
            WatchOnClient(model => model.TabToggleValue);

            SelectTab(FirstTabId);
        }

        private void SelectTab(int tabId)
        {
            SelectedTabId = tabId;
            TabToggles.SyncTo(tabId, v => TabToggleValue = v);
            Tabs.Select(this, TabContentElement, tabId);
        }

        // Rule R6: modal close wipes the nested tab partial; this restores it.
        // TEMPLATE: delete ONLY if this window never calls ShowModal/ShowInputModal.
        protected override void OnModalClosedRestore() => Tabs.Select(this, TabContentElement, SelectedTabId);

        private void RefreshSecondTab()
        {
            // TODO: data plumbing - load whatever the second tab displays.
        }

        // TEMPLATE: one handler per clickable element. Handlers RETURN an Action.
        public Action OnClickDoSomething() => () =>
        {
            StatusText = "Clicked."; // TODO: data plumbing
        };
    }
}
