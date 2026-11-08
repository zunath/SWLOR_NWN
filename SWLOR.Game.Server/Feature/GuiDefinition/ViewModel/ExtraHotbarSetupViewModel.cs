using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ExtraHotbarService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Domain.UI;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ExtraHotbarSetupViewModel : GuiViewModelBase<ExtraHotbarSetupViewModel, GuiPayloadBase>
    {
        public const string ContentElement = "extra_hotbar_setup_content";
        public const string MainPartial = "EXTRA_HOTBAR_SETUP_MAIN";

        public static string VisibleProperty(int bar) => $"Bar{bar}Visible";
        public static string SlotCountProperty(int bar) => $"Bar{bar}SlotCount";
        public static string SlotCountTextProperty(int bar) => $"Bar{bar}SlotCountText";
        public static string LayoutProperty(int bar) => $"Bar{bar}Layout";

        public bool Bar0Visible { get => Get<bool>(); set => Set(value); }
        public bool Bar1Visible { get => Get<bool>(); set => Set(value); }
        public bool Bar2Visible { get => Get<bool>(); set => Set(value); }

        public int Bar0SlotCount { get => Get<int>(); set => Set(value); }
        public int Bar1SlotCount { get => Get<int>(); set => Set(value); }
        public int Bar2SlotCount { get => Get<int>(); set => Set(value); }

        public string Bar0SlotCountText { get => Get<string>(); set => Set(value); }
        public string Bar1SlotCountText { get => Get<string>(); set => Set(value); }
        public string Bar2SlotCountText { get => Get<string>(); set => Set(value); }

        public int Bar0Layout { get => Get<int>(); set => Set(value); }
        public int Bar1Layout { get => Get<int>(); set => Set(value); }
        public int Bar2Layout { get => Get<int>(); set => Set(value); }

        // The client drops a value that reaches it before the control showing it, which is likely while every bar
        // redraws in the same tick. A slider left without a value reports its minimum, and saving that turned bars
        // down to one slot. So the controls are sent first, then their values, and changes are only listened for
        // from the next tick, after the values are sent again.
        private bool _isListening;

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _isListening = false;
            ChangePartialView(ContentElement, MainPartial);
            LoadValues();

            var windowToken = WindowToken;
            DelayCommand(0f, () => Listen(windowToken));

            ExtraHotbars.SetUnlocked(Player, true);
        }

        private void LoadValues()
        {
            for (var index = 0; index < ExtraHotbarsService.BarCount; index++)
            {
                var bar = ExtraHotbars.GetBar(Player, index);
                Set(bar?.IsVisible ?? false, VisibleProperty(index));
                Set(bar?.SlotCount ?? ExtraHotbarsService.MaxSlots, SlotCountProperty(index));
                Set(FormatSlotCount(bar?.SlotCount ?? ExtraHotbarsService.MaxSlots), SlotCountTextProperty(index));
                Set((int)(bar?.Layout ?? ExtraHotbarLayout.Horizontal), LayoutProperty(index));
            }
        }

        private void Listen(int windowToken)
        {
            if (WindowToken != windowToken || !Gui.IsWindowOpen(Player, GuiWindowType.ExtraHotbarSetup))
                return;

            // Watching sends each value again, now that its control exists.
            LoadValues();
            WatchOnClient(model => model.Bar0Visible);
            WatchOnClient(model => model.Bar1Visible);
            WatchOnClient(model => model.Bar2Visible);
            WatchOnClient(model => model.Bar0SlotCount);
            WatchOnClient(model => model.Bar1SlotCount);
            WatchOnClient(model => model.Bar2SlotCount);
            WatchOnClient(model => model.Bar0Layout);
            WatchOnClient(model => model.Bar1Layout);
            WatchOnClient(model => model.Bar2Layout);
            _isListening = true;
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (!_isListening)
                return;

            for (var index = 0; index < ExtraHotbarsService.BarCount; index++)
            {
                if (propertyName == VisibleProperty(index))
                {
                    var isVisible = Get<bool>(propertyName);
                    ExtraHotbars.UpdateBar(Player, index, bar => bar.IsVisible = isVisible);
                }
                else if (propertyName == SlotCountProperty(index))
                {
                    var slotCount = Get<int>(propertyName);
                    Set(FormatSlotCount(slotCount), SlotCountTextProperty(index));
                    ExtraHotbars.UpdateBar(Player, index, bar => bar.SlotCount = slotCount);
                }
                else if (propertyName == LayoutProperty(index))
                {
                    var layout = (ExtraHotbarLayout)Get<int>(propertyName);
                    ExtraHotbars.UpdateBar(Player, index, bar => bar.Layout = layout);
                }
            }
        }

        public override Action OnWindowClosed() => () =>
        {
            ExtraHotbars.SetUnlocked(Player, false);
        };

        private static string FormatSlotCount(int slotCount) => slotCount == 1 ? "1 slot" : $"{slotCount} slots";
    }
}
