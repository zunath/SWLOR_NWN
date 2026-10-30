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
        private string _slotsPartial;
        private bool _useDefaultSpot;
        private bool _hasTitleBar;

        // One inventory scan shared by every equipment slot during a single refresh.
        private List<uint> _handItems;

        public static string IconProperty(int slot) => $"Slot{slot}Icon";
        public static string TooltipProperty(int slot) => $"Slot{slot}Tooltip";
        public static string CountProperty(int slot) => $"Slot{slot}Count";
        public static string HasIconProperty(int slot) => $"Slot{slot}HasIcon";
        public static string IsActiveProperty(int slot) => $"Slot{slot}IsActive";
        public static string PairMainIconProperty(int slot) => $"Slot{slot}PairMainIcon";
        public static string PairOffIconProperty(int slot) => $"Slot{slot}PairOffIcon";
        public static string IsPairProperty(int slot) => $"Slot{slot}IsPair";

        // Three-part weapons draw their middle and top icons over the bottom one. A stack is the full-slot icon
        // or one half of a two-item slot.
        public const string FullStack = "Icon";
        public const string PairMainStack = "PairMainIcon";
        public const string PairOffStack = "PairOffIcon";
        public static string MiddleLayerProperty(int slot, string stack) => $"Slot{slot}{stack}Middle";
        public static string TopLayerProperty(int slot, string stack) => $"Slot{slot}{stack}Top";
        public static string HasLayersProperty(int slot, string stack) => $"Slot{slot}{stack}HasLayers";
        private static readonly string[] _stacks = { FullStack, PairMainStack, PairOffStack };
        public static string SlotsPartial(ExtraHotbarLayout layout, int slotCount) =>
            $"EXTRA_HOTBAR_{layout.ToString().ToUpperInvariant()}_{slotCount}";

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

        public string Slot0PairMainIcon { get => Get<string>(); set => Set(value); }
        public string Slot1PairMainIcon { get => Get<string>(); set => Set(value); }
        public string Slot2PairMainIcon { get => Get<string>(); set => Set(value); }
        public string Slot3PairMainIcon { get => Get<string>(); set => Set(value); }
        public string Slot4PairMainIcon { get => Get<string>(); set => Set(value); }
        public string Slot5PairMainIcon { get => Get<string>(); set => Set(value); }
        public string Slot6PairMainIcon { get => Get<string>(); set => Set(value); }
        public string Slot7PairMainIcon { get => Get<string>(); set => Set(value); }
        public string Slot8PairMainIcon { get => Get<string>(); set => Set(value); }
        public string Slot9PairMainIcon { get => Get<string>(); set => Set(value); }
        public string Slot10PairMainIcon { get => Get<string>(); set => Set(value); }
        public string Slot11PairMainIcon { get => Get<string>(); set => Set(value); }

        public string Slot0PairOffIcon { get => Get<string>(); set => Set(value); }
        public string Slot1PairOffIcon { get => Get<string>(); set => Set(value); }
        public string Slot2PairOffIcon { get => Get<string>(); set => Set(value); }
        public string Slot3PairOffIcon { get => Get<string>(); set => Set(value); }
        public string Slot4PairOffIcon { get => Get<string>(); set => Set(value); }
        public string Slot5PairOffIcon { get => Get<string>(); set => Set(value); }
        public string Slot6PairOffIcon { get => Get<string>(); set => Set(value); }
        public string Slot7PairOffIcon { get => Get<string>(); set => Set(value); }
        public string Slot8PairOffIcon { get => Get<string>(); set => Set(value); }
        public string Slot9PairOffIcon { get => Get<string>(); set => Set(value); }
        public string Slot10PairOffIcon { get => Get<string>(); set => Set(value); }
        public string Slot11PairOffIcon { get => Get<string>(); set => Set(value); }

        public bool Slot0IsPair { get => Get<bool>(); set => Set(value); }
        public bool Slot1IsPair { get => Get<bool>(); set => Set(value); }
        public bool Slot2IsPair { get => Get<bool>(); set => Set(value); }
        public bool Slot3IsPair { get => Get<bool>(); set => Set(value); }
        public bool Slot4IsPair { get => Get<bool>(); set => Set(value); }
        public bool Slot5IsPair { get => Get<bool>(); set => Set(value); }
        public bool Slot6IsPair { get => Get<bool>(); set => Set(value); }
        public bool Slot7IsPair { get => Get<bool>(); set => Set(value); }
        public bool Slot8IsPair { get => Get<bool>(); set => Set(value); }
        public bool Slot9IsPair { get => Get<bool>(); set => Set(value); }
        public bool Slot10IsPair { get => Get<bool>(); set => Set(value); }
        public bool Slot11IsPair { get => Get<bool>(); set => Set(value); }

        public string Slot0IconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot1IconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot2IconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot3IconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot4IconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot5IconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot6IconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot7IconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot8IconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot9IconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot10IconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot11IconMiddle { get => Get<string>(); set => Set(value); }

        public string Slot0IconTop { get => Get<string>(); set => Set(value); }
        public string Slot1IconTop { get => Get<string>(); set => Set(value); }
        public string Slot2IconTop { get => Get<string>(); set => Set(value); }
        public string Slot3IconTop { get => Get<string>(); set => Set(value); }
        public string Slot4IconTop { get => Get<string>(); set => Set(value); }
        public string Slot5IconTop { get => Get<string>(); set => Set(value); }
        public string Slot6IconTop { get => Get<string>(); set => Set(value); }
        public string Slot7IconTop { get => Get<string>(); set => Set(value); }
        public string Slot8IconTop { get => Get<string>(); set => Set(value); }
        public string Slot9IconTop { get => Get<string>(); set => Set(value); }
        public string Slot10IconTop { get => Get<string>(); set => Set(value); }
        public string Slot11IconTop { get => Get<string>(); set => Set(value); }

        public bool Slot0IconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot1IconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot2IconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot3IconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot4IconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot5IconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot6IconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot7IconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot8IconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot9IconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot10IconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot11IconHasLayers { get => Get<bool>(); set => Set(value); }

        public string Slot0PairMainIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot1PairMainIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot2PairMainIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot3PairMainIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot4PairMainIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot5PairMainIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot6PairMainIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot7PairMainIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot8PairMainIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot9PairMainIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot10PairMainIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot11PairMainIconMiddle { get => Get<string>(); set => Set(value); }

        public string Slot0PairMainIconTop { get => Get<string>(); set => Set(value); }
        public string Slot1PairMainIconTop { get => Get<string>(); set => Set(value); }
        public string Slot2PairMainIconTop { get => Get<string>(); set => Set(value); }
        public string Slot3PairMainIconTop { get => Get<string>(); set => Set(value); }
        public string Slot4PairMainIconTop { get => Get<string>(); set => Set(value); }
        public string Slot5PairMainIconTop { get => Get<string>(); set => Set(value); }
        public string Slot6PairMainIconTop { get => Get<string>(); set => Set(value); }
        public string Slot7PairMainIconTop { get => Get<string>(); set => Set(value); }
        public string Slot8PairMainIconTop { get => Get<string>(); set => Set(value); }
        public string Slot9PairMainIconTop { get => Get<string>(); set => Set(value); }
        public string Slot10PairMainIconTop { get => Get<string>(); set => Set(value); }
        public string Slot11PairMainIconTop { get => Get<string>(); set => Set(value); }

        public bool Slot0PairMainIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot1PairMainIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot2PairMainIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot3PairMainIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot4PairMainIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot5PairMainIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot6PairMainIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot7PairMainIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot8PairMainIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot9PairMainIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot10PairMainIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot11PairMainIconHasLayers { get => Get<bool>(); set => Set(value); }

        public string Slot0PairOffIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot1PairOffIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot2PairOffIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot3PairOffIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot4PairOffIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot5PairOffIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot6PairOffIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot7PairOffIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot8PairOffIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot9PairOffIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot10PairOffIconMiddle { get => Get<string>(); set => Set(value); }
        public string Slot11PairOffIconMiddle { get => Get<string>(); set => Set(value); }

        public string Slot0PairOffIconTop { get => Get<string>(); set => Set(value); }
        public string Slot1PairOffIconTop { get => Get<string>(); set => Set(value); }
        public string Slot2PairOffIconTop { get => Get<string>(); set => Set(value); }
        public string Slot3PairOffIconTop { get => Get<string>(); set => Set(value); }
        public string Slot4PairOffIconTop { get => Get<string>(); set => Set(value); }
        public string Slot5PairOffIconTop { get => Get<string>(); set => Set(value); }
        public string Slot6PairOffIconTop { get => Get<string>(); set => Set(value); }
        public string Slot7PairOffIconTop { get => Get<string>(); set => Set(value); }
        public string Slot8PairOffIconTop { get => Get<string>(); set => Set(value); }
        public string Slot9PairOffIconTop { get => Get<string>(); set => Set(value); }
        public string Slot10PairOffIconTop { get => Get<string>(); set => Set(value); }
        public string Slot11PairOffIconTop { get => Get<string>(); set => Set(value); }

        public bool Slot0PairOffIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot1PairOffIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot2PairOffIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot3PairOffIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot4PairOffIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot5PairOffIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot6PairOffIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot7PairOffIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot8PairOffIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot9PairOffIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot10PairOffIconHasLayers { get => Get<bool>(); set => Set(value); }
        public bool Slot11PairOffIconHasLayers { get => Get<bool>(); set => Set(value); }

        private int BarIndex => ExtraHotbars.GetBarIndex(WindowType);

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var dbPlayer = DB.Get<Entity.Player>(GetObjectUUID(Player));
            _useDefaultSpot = dbPlayer == null || !dbPlayer.WindowGeometries.ContainsKey(WindowType);
            _slotsPartial = null;
            _hasTitleBar = false;
            TitleBar = false;
            IsTransparent = true;
            for (var slot = 0; slot < ExtraHotbars.MaxSlots; slot++)
            {
                Set(string.Empty, IconProperty(slot));
                Set(false, HasIconProperty(slot));
                Set(false, IsActiveProperty(slot));
                Set(string.Empty, PairMainIconProperty(slot));
                Set(string.Empty, PairOffIconProperty(slot));
                Set(false, IsPairProperty(slot));
                foreach (var stack in _stacks)
                {
                    Set(string.Empty, MiddleLayerProperty(slot, stack));
                    Set(string.Empty, TopLayerProperty(slot, stack));
                    Set(false, HasLayersProperty(slot, stack));
                }
                Set(string.Empty, TooltipProperty(slot));
                Set(string.Empty, CountProperty(slot));
            }

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

            _handItems = null;
            for (var slot = 0; slot < _bar.SlotCount; slot++)
            {
                if (_bar.Slots[slot].Type is ExtraHotbarSlotType.Item or ExtraHotbarSlotType.ActionMode or ExtraHotbarSlotType.Equipment)
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

            var slotsPartial = SlotsPartial(_bar.Layout, _bar.SlotCount);
            if (slotsPartial != _slotsPartial)
            {
                _slotsPartial = slotsPartial;
                ChangePartialView(SlotsElement, slotsPartial);
            }

            _handItems = null;
            for (var slot = 0; slot < ExtraHotbars.MaxSlots; slot++)
                RefreshSlot(slot, _bar.Slots[slot]);

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
            var isPair = false;
            var upperLayers = new Dictionary<string, string[]>();
            var pairMainIcon = string.Empty;
            var pairOffIcon = string.Empty;

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
                case ExtraHotbarSlotType.Equipment:
                    _handItems ??= ExtraHotbars.GetHandItems(Player);
                    var (mainHand, offHand) = ExtraHotbars.FindEquipItems(slot, _handItems);
                    var mainLayers = GetEquipIconLayers(slot.MainHand, mainHand);
                    var offLayers = GetEquipIconLayers(slot.OffHand, offHand);
                    // A set with a missing item cannot be put away by a click, so it never reads as worn.
                    var isComplete = (slot.MainHand == null || GetIsObjectValid(mainHand)) &&
                                     (slot.OffHand == null || GetIsObjectValid(offHand));
                    isActive = isComplete && ExtraHotbars.IsEquipped(Player, mainHand, offHand);
                    tooltip = $"{(isActive ? "Unequip" : "Equip")} {GetEquipNames(slot)}";

                    // Two items sit side by side; weapon icons are tall, so each half still reads clearly.
                    if (slot.MainHand != null && slot.OffHand != null)
                    {
                        isPair = true;
                        pairMainIcon = mainLayers[0];
                        pairOffIcon = offLayers[0];
                        upperLayers[PairMainStack] = mainLayers;
                        upperLayers[PairOffStack] = offLayers;
                    }
                    else
                    {
                        var layers = slot.MainHand != null ? mainLayers : offLayers;
                        icon = layers[0];
                        upperLayers[FullStack] = layers;
                    }
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
            if (isPair)
            {
                SetIfChanged(PairMainIconProperty(slotIndex), pairMainIcon);
                SetIfChanged(PairOffIconProperty(slotIndex), pairOffIcon);
            }
            if (Get<bool>(IsPairProperty(slotIndex)) != isPair)
                Set(isPair, IsPairProperty(slotIndex));

            foreach (var stack in _stacks)
            {
                var hasLayers = upperLayers.TryGetValue(stack, out var layers) && layers.Length == 3;
                if (hasLayers)
                {
                    SetIfChanged(MiddleLayerProperty(slotIndex, stack), layers[1]);
                    SetIfChanged(TopLayerProperty(slotIndex, stack), layers[2]);
                }
                if (Get<bool>(HasLayersProperty(slotIndex, stack)) != hasLayers)
                    Set(hasLayers, HasLayersProperty(slotIndex, stack));
            }
        }

        private static string[] GetEquipIconLayers(ExtraHotbarEquipItem equipItem, uint item)
        {
            if (equipItem == null)
                return new[] { string.Empty };

            return GetIsObjectValid(item)
                ? Item.GetIconLayers(item)
                : new[] { Cache.GetItemIconByResref(equipItem.ItemResref) };
        }

        private static string GetEquipNames(ExtraHotbarSlot slot)
        {
            if (slot.MainHand != null && slot.OffHand != null)
                return $"{slot.MainHand.ItemName} and {slot.OffHand.ItemName}";

            return slot.MainHand != null
                ? slot.MainHand.ItemName
                : $"{slot.OffHand?.ItemName} (off hand)";
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
