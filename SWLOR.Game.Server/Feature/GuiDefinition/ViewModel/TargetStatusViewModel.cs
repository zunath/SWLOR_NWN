using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    internal class TargetStatusViewModel : GuiViewModelBase<TargetStatusViewModel, GuiPayloadBase>,
        IGuiRefreshable<TargetStatusRefreshEvent>
    {
        private int _screenHeight;
        private int _screenWidth;
        private int _screenScale;

        private static readonly GuiColor _shieldColor = new GuiColor(3, 87, 152);
        private static readonly GuiColor _hullColor = new GuiColor(139, 0, 0);
        private static readonly GuiColor _capacitorColor = new GuiColor(166, 111, 0);

        public string TargetName
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor Bar1Color
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public GuiColor Bar2Color
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public GuiColor Bar3Color
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string Bar1Label
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Bar2Label
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Bar3Label
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Bar1Value
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Bar2Value
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Bar3Value
        {
            get => Get<string>();
            set => Set(value);
        }

        public float Bar1Progress
        {
            get => Get<float>();
            set => Set(value);
        }
        public float Bar2Progress
        {
            get => Get<float>();
            set => Set(value);
        }
        public float Bar3Progress
        {
            get => Get<float>();
            set => Set(value);
        }

        public GuiRectangle RelativeValuePosition
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _screenHeight = -1;
            _screenScale = -1;
            _screenWidth = -1;

            UpdateWidget();
            UpdateData();
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
                const float WidgetWidth = 200f;
                const float WidgetHeight = 155f;
                const float XOffset = 255f;
                const float YOffset = 320f;

                var scale = screenScale / 100f;
                var x = screenWidth - XOffset * scale;
                var y = screenHeight - YOffset * scale;

                Geometry = new GuiRectangle(
                    x,
                    y,
                    WidgetWidth,
                    WidgetHeight);

                x = 60f * scale;
                RelativeValuePosition = new GuiRectangle(x, 2f, 110f, 50f);

                _screenHeight = screenHeight;
                _screenWidth = screenWidth;
                _screenScale = screenScale;
            }
        }

        private void UpdateData()
        {
            var (target, targetStatus) = Space.GetCurrentTarget(Player);

            if (!GetIsObjectValid(target) || targetStatus == null)
                return;

            TargetName = GetName(target);

            Bar1Color = _shieldColor;
            Bar2Color = _hullColor;
            Bar3Color = _capacitorColor;

            var currentShields = targetStatus.Shield;
            var maxShields = targetStatus.MaxShield;
            var currentHull = targetStatus.Hull;
            var maxHull = targetStatus.MaxHull;
            var currentCapacitor = targetStatus.Capacitor;
            var maxCapacitor = targetStatus.MaxCapacitor;

            Bar1Value = $"{currentShields} / {maxShields}";
            Bar2Value = $"{currentHull} / {maxHull}";
            Bar3Value = $"{currentCapacitor} / {maxCapacitor}";

            Bar1Progress = maxShields <= 0 ? 0 : (float)currentShields / (float)maxShields;
            Bar2Progress = maxHull <= 0 ? 0 : (float)currentHull / (float)maxHull;
            Bar3Progress = maxCapacitor <= 0 ? 0 : (float)currentCapacitor / (float)maxCapacitor;
        }

        public void Refresh(TargetStatusRefreshEvent payload)
        {
            if (!Space.IsPlayerInSpaceMode(Player))
                return;

            UpdateWidget();
            UpdateData();
        }
    }
}
