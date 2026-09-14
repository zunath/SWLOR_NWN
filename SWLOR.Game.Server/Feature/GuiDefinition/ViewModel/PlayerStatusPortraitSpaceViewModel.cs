using System;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// Space-mode counterpart to <see cref="PlayerStatusPortraitViewModel"/>. Renders thin Shield,
    /// Hull and Capacitor bars overlaid on the portrait as an alternative to the docked SH/HL/CAP
    /// window. Unlike on foot there is no native bar to lean on, so all three ship vitals are shown.
    /// </summary>
    internal class PlayerStatusPortraitSpaceViewModel : GuiViewModelBase<PlayerStatusPortraitSpaceViewModel, GuiPayloadBase>,
        IGuiRefreshable<PlayerStatusRefreshEvent>
    {
        private int _screenHeight;
        private int _screenWidth;
        private int _screenScale;

        private static readonly GuiColor _shieldColor = new GuiColor(3, 87, 152);
        private static readonly GuiColor _hullColor = new GuiColor(139, 0, 0);
        private static readonly GuiColor _capacitorColor = new GuiColor(166, 111, 0);

        public GuiColor ShieldColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public GuiColor HullColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public GuiColor CapacitorColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string ShieldValue
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HullValue
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CapacitorValue
        {
            get => Get<string>();
            set => Set(value);
        }

        public float ShieldProgress
        {
            get => Get<float>();
            set => Set(value);
        }

        public float HullProgress
        {
            get => Get<float>();
            set => Set(value);
        }

        public float CapacitorProgress
        {
            get => Get<float>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _screenHeight = -1;
            _screenScale = -1;
            _screenWidth = -1;

            ShieldColor = _shieldColor;
            HullColor = _hullColor;
            CapacitorColor = _capacitorColor;

            UpdateWidget();
            UpdateAllData();
        }

        private void UpdateWidget()
        {
            var screenHeight = GetPlayerDeviceProperty(Player, PlayerDevicePropertyType.GuiHeight);
            var screenWidth = GetPlayerDeviceProperty(Player, PlayerDevicePropertyType.GuiWidth);
            var screenScale = GetPlayerDeviceProperty(Player, PlayerDevicePropertyType.GuiScale);

            if (_screenHeight != screenHeight ||
                _screenWidth != screenWidth ||
                _screenScale != screenScale)
            {
                // Three bars, so this is taller than the on-foot overlay; height must fit all three
                // rows plus NUI's inter-row margins or the bottom row clips (see the definition).
                const float WidgetWidth = 72f;
                const float WidgetHeight = 74f;

                // Anchored top-right and scaled by GUI scale, same convention as the on-foot overlay.
                // YOffset is smaller than the 2-bar overlay so the taller stack still sits over the
                // portrait with its bottom clear of the button row beneath it.
                const float XOffset = 64f;
                const float YOffset = 31f;

                var scale = screenScale / 100f;
                var x = screenWidth - XOffset * scale;
                var y = YOffset * scale;

                Geometry = new GuiRectangle(x, y, WidgetWidth, WidgetHeight);

                _screenHeight = screenHeight;
                _screenWidth = screenWidth;
                _screenScale = screenScale;
            }
        }

        private void UpdateAllData()
        {
            var shipStatus = Space.GetShipStatus(Player);
            if (shipStatus == null)
                return;

            UpdateShield(shipStatus);
            UpdateHull(shipStatus);
            UpdateCapacitor(shipStatus);
        }

        private void UpdateShield(ShipStatus shipStatus)
        {
            var current = shipStatus.Shield;
            var max = shipStatus.MaxShield;
            ShieldValue = $"{current}";
            ShieldProgress = max <= 0 ? 0f : Math.Clamp((float)current / max, 0f, 1f);
        }

        private void UpdateHull(ShipStatus shipStatus)
        {
            var current = shipStatus.Hull;
            var max = shipStatus.MaxHull;
            HullValue = $"{current}";
            HullProgress = max <= 0 ? 0f : Math.Clamp((float)current / max, 0f, 1f);
        }

        private void UpdateCapacitor(ShipStatus shipStatus)
        {
            var current = shipStatus.Capacitor;
            var max = shipStatus.MaxCapacitor;
            CapacitorValue = $"{current}";
            CapacitorProgress = max <= 0 ? 0f : Math.Clamp((float)current / max, 0f, 1f);
        }

        public void Refresh(PlayerStatusRefreshEvent payload)
        {
            if (!Space.IsPlayerInSpaceMode(Player))
                return;

            var shipStatus = Space.GetShipStatus(Player);
            if (shipStatus == null)
                return;

            UpdateWidget();

            if (payload.Type == PlayerStatusRefreshEvent.StatType.Shield)
                UpdateShield(shipStatus);
            else if (payload.Type == PlayerStatusRefreshEvent.StatType.Hull)
                UpdateHull(shipStatus);
            else if (payload.Type == PlayerStatusRefreshEvent.StatType.Capacitor)
                UpdateCapacitor(shipStatus);
        }
    }
}
