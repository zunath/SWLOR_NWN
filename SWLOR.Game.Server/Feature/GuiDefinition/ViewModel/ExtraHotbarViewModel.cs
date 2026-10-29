using System.Collections.Generic;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ExtraHotbarService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ExtraHotbarViewModel : GuiViewModelBase<ExtraHotbarViewModel, GuiPayloadBase>,
        IGuiRefreshable<ExtraHotbarRefreshEvent>,
        IGuiRefreshable<ExtraHotbarStatusRefreshEvent>
    {
        public const string SlotsElement = "extra_hotbar_slots";
        public const string HorizontalPartial = "EXTRA_HOTBAR_HORIZONTAL";
        public const string VerticalPartial = "EXTRA_HOTBAR_VERTICAL";
        public const string StackedPartial = "EXTRA_HOTBAR_STACKED";
        public const int StackedRowLength = 6;

        public const float SlotSize = 36f;
        public const float SlotMargin = 1f;
        public const float InitialX = 400f;

        // Measured in the client: the window pads its content on every side, and a title bar adds its own height.
        private const float WindowPadding = 14f;
        private const float TitleBarHeight = 32f;
        private const float SlotStep = SlotSize + SlotMargin * 2f;

        // New bars start in the bottom-right corner, stacked above the chat entry bar, bar 1 lowest.
        // The chat entry bar and the native quickbar take the bottom 54 pixels at 100% UI scale.
        private const float DefaultBottomReserve = 56f;
        private const float ScreenEdgeGap = 4f;

        public const string CommandIcon = "ife_emotes";

        private ExtraHotbar _bar;
        private ExtraHotbarLayout _layout;
        private bool _useDefaultSpot;
        private bool _hasTitleBar;

        public static string IconProperty(int slot) => $"Slot{slot}Icon";
        public static string TooltipProperty(int slot) => $"Slot{slot}Tooltip";
        public static string CountProperty(int slot) => $"Slot{slot}Count";
        public static string HasIconProperty(int slot) => $"Slot{slot}HasIcon";
        public static string IsActiveProperty(int slot) => $"Slot{slot}IsActive";
        public static string IsVisibleProperty(int slot) => $"Slot{slot}IsVisible";

        public static float GetWidth(int slotCount, ExtraHotbarLayout layout) =>
            GetColumns(slotCount, layout) * SlotStep + WindowPadding * 2f;

        public static float GetHeight(int slotCount, ExtraHotbarLayout layout, bool hasTitleBar) =>
            GetRows(slotCount, layout) * SlotStep + WindowPadding * 2f + (hasTitleBar ? TitleBarHeight : 0f);

        private static int GetColumns(int slotCount, ExtraHotbarLayout layout) => layout switch
        {
            ExtraHotbarLayout.Vertical => 1,
            ExtraHotbarLayout.Stacked => System.Math.Min(slotCount, StackedRowLength),
            _ => slotCount
        };

        private static int GetRows(int slotCount, ExtraHotbarLayout layout) => layout switch
        {
            ExtraHotbarLayout.Vertical => slotCount,
            ExtraHotbarLayout.Stacked => (slotCount + StackedRowLength - 1) / StackedRowLength,
            _ => 1
        };

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

        public bool Slot0IsActive { get => Get<bool>(); set => Set(value); }
        public bool Slot1IsActive { get => Get<bool>(); set => Set(value); }
        public bool Slot2IsActive { get => Get<bool>(); set => Set(value); }
        public bool Slot3IsActive { get => Get<bool>(); set => Set(value); }
        public bool Slot4IsActive { get => Get<bool>(); set => Set(value); }
        public bool Slot5IsActive { get => Get<bool>(); set => Set(value); }
        public bool Slot6IsActive { get => Get<bool>(); set => Set(value); }
        public bool Slot7IsActive { get => Get<bool>(); set => Set(value); }
        public bool Slot8IsActive { get => Get<bool>(); set => Set(value); }
        public bool Slot9IsActive { get => Get<bool>(); set => Set(value); }
        public bool Slot10IsActive { get => Get<bool>(); set => Set(value); }
        public bool Slot11IsActive { get => Get<bool>(); set => Set(value); }

        public bool Slot0IsVisible { get => Get<bool>(); set => Set(value); }
        public bool Slot1IsVisible { get => Get<bool>(); set => Set(value); }
        public bool Slot2IsVisible { get => Get<bool>(); set => Set(value); }
        public bool Slot3IsVisible { get => Get<bool>(); set => Set(value); }
        public bool Slot4IsVisible { get => Get<bool>(); set => Set(value); }
        public bool Slot5IsVisible { get => Get<bool>(); set => Set(value); }
        public bool Slot6IsVisible { get => Get<bool>(); set => Set(value); }
        public bool Slot7IsVisible { get => Get<bool>(); set => Set(value); }
        public bool Slot8IsVisible { get => Get<bool>(); set => Set(value); }
        public bool Slot9IsVisible { get => Get<bool>(); set => Set(value); }
        public bool Slot10IsVisible { get => Get<bool>(); set => Set(value); }
        public bool Slot11IsVisible { get => Get<bool>(); set => Set(value); }

        private int BarIndex => ExtraHotbars.GetBarIndex(WindowType);

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var dbPlayer = DB.Get<Entity.Player>(GetObjectUUID(Player));
            _useDefaultSpot = dbPlayer == null || !dbPlayer.WindowGeometries.ContainsKey(WindowType);
            _layout = ExtraHotbarLayout.Horizontal;
            _hasTitleBar = false;
            TitleBar = false;
            IsTransparent = true;
            for (var slot = 0; slot < ExtraHotbars.MaxSlots; slot++)
            {
                Set(string.Empty, IconProperty(slot));
                Set(false, HasIconProperty(slot));
                Set(false, IsActiveProperty(slot));
                Set(true, IsVisibleProperty(slot));
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

        public void Refresh(ExtraHotbarStatusRefreshEvent payload)
        {
            if (_bar == null)
                return;

            for (var slot = 0; slot < _bar.SlotCount; slot++)
            {
                if (_bar.Slots[slot].Type is ExtraHotbarSlotType.Item or ExtraHotbarSlotType.ActionMode)
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

            if (_bar.Layout != _layout)
            {
                _layout = _bar.Layout;
                ChangePartialView(SlotsElement, _layout switch
                {
                    ExtraHotbarLayout.Vertical => VerticalPartial,
                    ExtraHotbarLayout.Stacked => StackedPartial,
                    _ => HorizontalPartial
                });
            }

            for (var slot = 0; slot < ExtraHotbars.MaxSlots; slot++)
            {
                RefreshSlot(slot, _bar.Slots[slot]);
                var isVisible = slot < _bar.SlotCount;
                if (Get<bool>(IsVisibleProperty(slot)) != isVisible)
                    Set(isVisible, IsVisibleProperty(slot));
            }

            ResizeToFit(isUnlocked);
        }

        // The title bar grows the bar upward so its slots stay put while it is unlocked.
        private void ResizeToFit(bool hasTitleBar)
        {
            var width = GetWidth(_bar.SlotCount, _bar.Layout);
            var height = GetHeight(_bar.SlotCount, _bar.Layout, hasTitleBar);
            var x = Geometry.X;
            var y = Geometry.Y;

            // Positions are screen pixels while sizes grow with the UI scale.
            var screenWidth = GetPlayerDeviceProperty(Player, PlayerDevicePropertyType.GuiWidth);
            var screenHeight = GetPlayerDeviceProperty(Player, PlayerDevicePropertyType.GuiHeight);
            var scale = GetScale();

            if (hasTitleBar != _hasTitleBar)
            {
                y += (hasTitleBar ? -TitleBarHeight : TitleBarHeight) * scale;
                _hasTitleBar = hasTitleBar;
            }

            if (screenWidth > 0 && screenHeight > 0)
            {
                if (_useDefaultSpot)
                {
                    var lowerBarsHeight = 0f;
                    for (var index = 0; index < BarIndex; index++)
                    {
                        var lowerBar = ExtraHotbars.GetBar(Player, index);
                        lowerBarsHeight += GetHeight(lowerBar.SlotCount, lowerBar.Layout, false) * scale;
                    }

                    x = screenWidth - ScreenEdgeGap - width * scale;
                    y = screenHeight - DefaultBottomReserve * scale - lowerBarsHeight - height * scale;
                    _useDefaultSpot = false;
                }

                // Keeps the whole bar on screen after a resolution change or when it grows.
                x = System.Math.Clamp(x, 0f, System.Math.Max(0f, screenWidth - width * scale));
                y = System.Math.Clamp(y, 0f, System.Math.Max(0f, screenHeight - height * scale));
            }

            Geometry = new GuiRectangle(x, y, width, height);
        }

        /// <summary>
        /// Drops the title bar so the bar is saved where its slots sit when locked.
        /// </summary>
        public void ApplyLockedGeometry()
        {
            if (!_hasTitleBar || _bar == null)
                return;

            _hasTitleBar = false;
            Geometry = new GuiRectangle(
                Geometry.X,
                Geometry.Y + TitleBarHeight * GetScale(),
                GetWidth(_bar.SlotCount, _bar.Layout),
                GetHeight(_bar.SlotCount, _bar.Layout, false));
        }

        private float GetScale()
        {
            var scale = GetPlayerDeviceProperty(Player, PlayerDevicePropertyType.GuiScale) / 100f;
            return scale > 0f ? scale : 1f;
        }

        private void RefreshSlot(int slotIndex, ExtraHotbarSlot slot)
        {
            var icon = string.Empty;
            var tooltip = "Right-click to choose what this slot does.";
            var count = string.Empty;
            var isActive = false;

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
                case ExtraHotbarSlotType.ActionMode:
                    isActive = GetActionMode(Player, slot.ActionMode);
                    icon = ExtraHotbars.GetActionModeIcon(slot.ActionMode);
                    tooltip = $"{ExtraHotbars.GetActionModeName(slot.ActionMode)} ({(isActive ? "on" : "off")})";
                    break;
            }

            // An empty resref draws a missing-texture box, so the icon is hidden instead.
            if (!string.IsNullOrEmpty(icon))
                SetIfChanged(IconProperty(slotIndex), icon);
            if (Get<bool>(HasIconProperty(slotIndex)) != !string.IsNullOrEmpty(icon))
                Set(!string.IsNullOrEmpty(icon), HasIconProperty(slotIndex));
            if (Get<bool>(IsActiveProperty(slotIndex)) != isActive)
                Set(isActive, IsActiveProperty(slotIndex));
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
