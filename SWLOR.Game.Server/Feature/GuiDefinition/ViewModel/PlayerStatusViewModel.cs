using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    internal class PlayerStatusViewModel: GuiViewModelBase<PlayerStatusViewModel, GuiPayloadBase>,
        IGuiRefreshable<PlayerStatusRefreshEvent>
    {
        private int _screenHeight;
        private int _screenWidth;
        private int _screenScale;

        private static readonly GuiColor _hpColor = new GuiColor(139, 0, 0);
        private static readonly GuiColor _stmColor = new GuiColor(0, 104, 0);
        private static readonly GuiColor _fpColor = new GuiColor(3, 87, 152);

        private static readonly GuiColor _shieldColor = new GuiColor(3, 87, 152);
        private static readonly GuiColor _hullColor = new GuiColor(139, 0, 0);
        private static readonly GuiColor _capacitorColor = new GuiColor(166, 111, 0);

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
            RelativeValuePosition = new GuiRectangle(60f, 2f, 110f, 50f);
            
            ToggleLabels(true);

            Bar1Color = _hpColor;
            Bar2Color = _stmColor;
            Bar3Color = _fpColor;
        }

        private void ToggleLabels(bool isCharacter)
        {
            if (isCharacter)
            {
                Bar1Label = "HP:";
                Bar2Label = "STM:";
                Bar3Label = "FP:";
            }
            else
            {
                Bar1Label = "SH:";
                Bar2Label = "HL:";
                Bar3Label = "CAP:";
            }
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
                const float WidgetHeight = 105f;
                const float XOffset = 255f;
                const float YOffset = 165f;

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
            if (Space.IsPlayerInSpaceMode(Player))
            {
                ToggleLabels(false);
                UpdateShipStats();
            }
            else
            {
                ToggleLabels(true);
                UpdateCharacterStats();
            }
        }

        private void UpdateCharacterStats()
        {
            Bar1Color = _hpColor;
            Bar2Color = _stmColor;
            Bar3Color = _fpColor;

            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            var currentHP = GetCurrentHitPoints(Player);
            var maxHP = GetMaxHitPoints(Player);
            var currentFP = dbPlayer.FP;
            var maxFP = Stat.GetMaxFP(Player, dbPlayer);
            var currentSTM = dbPlayer.Stamina;
            var maxSTM = Stat.GetMaxStamina(Player, dbPlayer);
            var isStandard = dbPlayer.CharacterType == CharacterType.Standard;

            Bar1Value = $"{currentHP} / {maxHP}";
            Bar2Value = $"{currentSTM} / {maxSTM}";
            Bar3Value = isStandard ? "0 / 0" : $"{currentFP} / {maxFP}";

            Bar1Progress = maxHP <= 0 ? 0 : (float)currentHP / (float)maxHP;
            Bar2Progress = maxSTM <= 0 ? 0 : (float)currentSTM / (float)maxSTM;
            Bar3Progress = maxFP <= 0 || isStandard ? 0 : (float)currentFP / (float)maxFP;

        }

        private void UpdateShipStats()
        {
            Bar1Color = _shieldColor;
            Bar2Color = _hullColor;
            Bar3Color = _capacitorColor;

            var shipStatus = Space.GetShipStatus(Player);

            var currentShields = shipStatus.Shield;
            var maxShields = shipStatus.MaxShield;
            var currentHull = shipStatus.Hull;
            var maxHull = shipStatus.MaxHull;
            var currentCapacitor = shipStatus.Capacitor;
            var maxCapacitor = shipStatus.MaxCapacitor;

            Bar1Value = $"{currentShields} / {maxShields}";
            Bar2Value = $"{currentHull} / {maxHull}";
            Bar3Value = $"{currentCapacitor} / {maxCapacitor}";

            Bar1Progress = maxShields <= 0 ? 0 : (float)currentShields / (float)maxShields;
            Bar2Progress = maxHull <= 0 ? 0 : (float)currentHull / (float)maxHull;
            Bar3Progress = maxCapacitor <= 0 ? 0 : (float)currentCapacitor / (float)maxCapacitor;
        }

        public void Refresh(PlayerStatusRefreshEvent payload)
        {
            UpdateWidget();
            UpdateData();
        }
    }
}
