using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ExtraHotbarSetupViewModel : GuiViewModelBase<ExtraHotbarSetupViewModel, GuiPayloadBase>
    {
        public const string ContentElement = "extra_hotbar_setup_content";
        public const string MainPartial = "EXTRA_HOTBAR_SETUP_MAIN";

        public static string VisibleProperty(int bar) => $"Bar{bar}Visible";
        public static string SlotCountProperty(int bar) => $"Bar{bar}SlotCount";
        public static string SlotCountTextProperty(int bar) => $"Bar{bar}SlotCountText";
        public static string VerticalProperty(int bar) => $"Bar{bar}Vertical";

        public bool Bar0Visible { get => Get<bool>(); set => Set(value); }
        public bool Bar1Visible { get => Get<bool>(); set => Set(value); }
        public bool Bar2Visible { get => Get<bool>(); set => Set(value); }

        public int Bar0SlotCount { get => Get<int>(); set => Set(value); }
        public int Bar1SlotCount { get => Get<int>(); set => Set(value); }
        public int Bar2SlotCount { get => Get<int>(); set => Set(value); }

        public string Bar0SlotCountText { get => Get<string>(); set => Set(value); }
        public string Bar1SlotCountText { get => Get<string>(); set => Set(value); }
        public string Bar2SlotCountText { get => Get<string>(); set => Set(value); }

        public bool Bar0Vertical { get => Get<bool>(); set => Set(value); }
        public bool Bar1Vertical { get => Get<bool>(); set => Set(value); }
        public bool Bar2Vertical { get => Get<bool>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            for (var index = 0; index < ExtraHotbars.BarCount; index++)
            {
                var bar = ExtraHotbars.GetBar(Player, index);
                Set(bar?.IsVisible ?? false, VisibleProperty(index));
                Set(bar?.SlotCount ?? ExtraHotbars.MaxSlots, SlotCountProperty(index));
                Set(FormatSlotCount(bar?.SlotCount ?? ExtraHotbars.MaxSlots), SlotCountTextProperty(index));
                Set(bar?.IsVertical ?? false, VerticalProperty(index));
            }

            WatchOnClient(model => model.Bar0Visible);
            WatchOnClient(model => model.Bar1Visible);
            WatchOnClient(model => model.Bar2Visible);
            WatchOnClient(model => model.Bar0SlotCount);
            WatchOnClient(model => model.Bar1SlotCount);
            WatchOnClient(model => model.Bar2SlotCount);
            WatchOnClient(model => model.Bar0Vertical);
            WatchOnClient(model => model.Bar1Vertical);
            WatchOnClient(model => model.Bar2Vertical);

            ChangePartialView(ContentElement, MainPartial);
            ExtraHotbars.SetUnlocked(Player, true);
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            for (var index = 0; index < ExtraHotbars.BarCount; index++)
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
                else if (propertyName == VerticalProperty(index))
                {
                    var isVertical = Get<bool>(propertyName);
                    ExtraHotbars.UpdateBar(Player, index, bar => bar.IsVertical = isVertical);
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
