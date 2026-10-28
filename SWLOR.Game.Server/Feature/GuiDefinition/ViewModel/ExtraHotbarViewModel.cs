using System.Collections.Generic;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ExtraHotbarService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ExtraHotbarViewModel : GuiViewModelBase<ExtraHotbarViewModel, GuiPayloadBase>,
        IGuiRefreshable<ExtraHotbarRefreshEvent>,
        IGuiRefreshable<ExtraHotbarItemCountRefreshEvent>
    {
        public const string SlotsElement = "extra_hotbar_slots";
        public const string HorizontalPartial = "EXTRA_HOTBAR_HORIZONTAL";
        public const string VerticalPartial = "EXTRA_HOTBAR_VERTICAL";

        public const float SlotSize = 36f;
        public const float SlotMargin = 1f;
        public const float InitialX = 400f;

        // Measured in the client: the window pads its content on every side, and a title bar adds its own height.
        private const float WindowPadding = 14f;
        private const float TitleBarHeight = 32f;
        private const float SlotStep = SlotSize + SlotMargin * 2f;

        public const string CommandIcon = "ife_emotes";

        private ExtraHotbar _bar;
        private bool _isVertical;

        public static string IconProperty(int slot) => $"Slot{slot}Icon";
        public static string TooltipProperty(int slot) => $"Slot{slot}Tooltip";
        public static string CountProperty(int slot) => $"Slot{slot}Count";
        public static string HasIconProperty(int slot) => $"Slot{slot}HasIcon";

        public static float GetWidth(int slotCount, bool isVertical) =>
            (isVertical ? SlotStep : slotCount * SlotStep) + WindowPadding * 2f;

        public static float GetHeight(int slotCount, bool isVertical, bool hasTitleBar) =>
            (isVertical ? slotCount * SlotStep : SlotStep) + WindowPadding * 2f + (hasTitleBar ? TitleBarHeight : 0f);

        public object TitleBar { get => Get<object>(); set => Set(value); }
        public bool IsTransparent { get => Get<bool>(); set => Set(value); }

        public string Slot0Icon { get => Get<string>(); set => Set(value); }
        public string Slot1Icon { get => Get<string>(); set => Set(value); }
        public string Slot2Icon { get => Get<string>(); set => Set(value); }
        public string Slot3Icon { get => Get<string>(); set => Set(value); }
        public string Slot4Icon { get => Get<string>(); set => Set(value); }
        public string Slot5Icon { get => Get<string>(); set => Set(value); }
        public string Slot6Icon { get => Get<string>(); set => Set(value); }
        public string Slot7Icon { get => Get<string>(); set => Set(value); }
        public string Slot8Icon { get => Get<string>(); set => Set(value); }
        public string Slot9Icon { get => Get<string>(); set => Set(value); }
        public string Slot10Icon { get => Get<string>(); set => Set(value); }
        public string Slot11Icon { get => Get<string>(); set => Set(value); }

        public string Slot0Tooltip { get => Get<string>(); set => Set(value); }
        public string Slot1Tooltip { get => Get<string>(); set => Set(value); }
        public string Slot2Tooltip { get => Get<string>(); set => Set(value); }
        public string Slot3Tooltip { get => Get<string>(); set => Set(value); }
        public string Slot4Tooltip { get => Get<string>(); set => Set(value); }
        public string Slot5Tooltip { get => Get<string>(); set => Set(value); }
        public string Slot6Tooltip { get => Get<string>(); set => Set(value); }
        public string Slot7Tooltip { get => Get<string>(); set => Set(value); }
        public string Slot8Tooltip { get => Get<string>(); set => Set(value); }
        public string Slot9Tooltip { get => Get<string>(); set => Set(value); }
        public string Slot10Tooltip { get => Get<string>(); set => Set(value); }
        public string Slot11Tooltip { get => Get<string>(); set => Set(value); }

        public string Slot0Count { get => Get<string>(); set => Set(value); }
        public string Slot1Count { get => Get<string>(); set => Set(value); }
        public string Slot2Count { get => Get<string>(); set => Set(value); }
        public string Slot3Count { get => Get<string>(); set => Set(value); }
        public string Slot4Count { get => Get<string>(); set => Set(value); }
        public string Slot5Count { get => Get<string>(); set => Set(value); }
        public string Slot6Count { get => Get<string>(); set => Set(value); }
        public string Slot7Count { get => Get<string>(); set => Set(value); }
        public string Slot8Count { get => Get<string>(); set => Set(value); }
        public string Slot9Count { get => Get<string>(); set => Set(value); }
        public string Slot10Count { get => Get<string>(); set => Set(value); }
        public string Slot11Count { get => Get<string>(); set => Set(value); }

        public bool Slot0HasIcon { get => Get<bool>(); set => Set(value); }
        public bool Slot1HasIcon { get => Get<bool>(); set => Set(value); }
        public bool Slot2HasIcon { get => Get<bool>(); set => Set(value); }
        public bool Slot3HasIcon { get => Get<bool>(); set => Set(value); }
        public bool Slot4HasIcon { get => Get<bool>(); set => Set(value); }
        public bool Slot5HasIcon { get => Get<bool>(); set => Set(value); }
        public bool Slot6HasIcon { get => Get<bool>(); set => Set(value); }
        public bool Slot7HasIcon { get => Get<bool>(); set => Set(value); }
        public bool Slot8HasIcon { get => Get<bool>(); set => Set(value); }
        public bool Slot9HasIcon { get => Get<bool>(); set => Set(value); }
        public bool Slot10HasIcon { get => Get<bool>(); set => Set(value); }
        public bool Slot11HasIcon { get => Get<bool>(); set => Set(value); }

        private int BarIndex => ExtraHotbars.GetBarIndex(WindowType);

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _isVertical = false;
            TitleBar = false;
            IsTransparent = true;
            for (var slot = 0; slot < ExtraHotbars.MaxSlots; slot++)
            {
                Set(string.Empty, IconProperty(slot));
                Set(false, HasIconProperty(slot));
                Set(string.Empty, TooltipProperty(slot));
                Set(string.Empty, CountProperty(slot));
            }

            ChangePartialView(SlotsElement, HorizontalPartial);
            LoadBar();
        }

        public void Refresh(ExtraHotbarRefreshEvent payload)
        {
            LoadBar();
        }

        public void Refresh(ExtraHotbarItemCountRefreshEvent payload)
        {
            if (_bar == null)
                return;

            for (var slot = 0; slot < _bar.SlotCount; slot++)
            {
                if (_bar.Slots[slot].Type == ExtraHotbarSlotType.Item)
                    RefreshSlot(slot, _bar.Slots[slot]);
            }
        }

        private void LoadBar()
        {
            _bar = ExtraHotbars.GetBar(Player, BarIndex);
            if (_bar == null)
                return;

            var isUnlocked = ExtraHotbars.IsUnlocked(Player);
            TitleBar = isUnlocked ? $"Hotbar {BarIndex + 1}" : false;
            IsTransparent = !isUnlocked;

            if (_bar.IsVertical != _isVertical)
            {
                _isVertical = _bar.IsVertical;
                ChangePartialView(SlotsElement, _isVertical ? VerticalPartial : HorizontalPartial);
            }

            for (var slot = 0; slot < ExtraHotbars.MaxSlots; slot++)
                RefreshSlot(slot, _bar.Slots[slot]);

            ResizeToFit(isUnlocked);
        }

        // The top edge never moves, so a saved position is right whether the bar was saved locked or unlocked.
        private void ResizeToFit(bool hasTitleBar)
        {
            Geometry = new GuiRectangle(
                Geometry.X,
                Geometry.Y,
                GetWidth(_bar.SlotCount, _bar.IsVertical),
                GetHeight(_bar.SlotCount, _bar.IsVertical, hasTitleBar));
        }

        private void RefreshSlot(int slotIndex, ExtraHotbarSlot slot)
        {
            var icon = string.Empty;
            var tooltip = "Right-click to choose what this slot does.";
            var count = string.Empty;

            switch (slot.Type)
            {
                case ExtraHotbarSlotType.Ability:
                    if (Ability.IsFeatRegistered(slot.Feat))
                    {
                        icon = ExtraHotbars.GetAbilityIcon(slot.Feat);
                        tooltip = Ability.GetAbilityDetail(slot.Feat).Name;
                    }
                    break;
                case ExtraHotbarSlotType.Item:
                    var item = ExtraHotbars.FindItem(Player, slot.ItemResref);
                    var quantity = ExtraHotbars.CountItems(Player, slot.ItemResref);
                    icon = GetIsObjectValid(item) ? Item.GetIconResref(item) : Cache.GetItemIconByResref(slot.ItemResref);
                    tooltip = $"{slot.ItemName} ({quantity})";
                    count = quantity.ToString();
                    break;
                case ExtraHotbarSlotType.Command:
                    icon = CommandIcon;
                    tooltip = slot.Command;
                    count = slot.Command.TrimStart('/').Split(' ')[0];
                    break;
            }

            // An empty resref draws a missing-texture box, so the icon is hidden instead.
            if (!string.IsNullOrEmpty(icon))
                SetIfChanged(IconProperty(slotIndex), icon);
            if (Get<bool>(HasIconProperty(slotIndex)) != !string.IsNullOrEmpty(icon))
                Set(!string.IsNullOrEmpty(icon), HasIconProperty(slotIndex));
            SetIfChanged(TooltipProperty(slotIndex), tooltip);
            SetIfChanged(CountProperty(slotIndex), count);
        }

        private void SetIfChanged(string propertyName, string value)
        {
            if (Get<string>(propertyName) != value)
                Set(value, propertyName);
        }

        public Action OnClickSlot(int slot) => () =>
        {
            if (_bar == null)
                return;

            if (_bar.Slots[slot].Type == ExtraHotbarSlotType.Empty)
            {
                OpenPicker(slot);
                return;
            }

            ExtraHotbars.UseSlot(Player, _bar.Slots[slot]);
        };

        public Action OnMouseDownSlot(int slot) => () =>
        {
            var payload = NuiGetEventPayload();
            if ((NuiMouseButton)JsonGetInt(JsonObjectGet(payload, "mouse_btn")) != NuiMouseButton.Right)
                return;

            OpenPicker(slot);
        };

        private void OpenPicker(int slot)
        {
            var payload = new ExtraHotbarSlotPickerPayload(BarIndex, slot);
            if (Gui.IsWindowOpen(Player, GuiWindowType.ExtraHotbarSlotPicker))
                Gui.ClosePlayerWindow(Player, GuiWindowType.ExtraHotbarSlotPicker);

            Gui.TogglePlayerWindow(Player, GuiWindowType.ExtraHotbarSlotPicker, payload);
        }
    }
}
