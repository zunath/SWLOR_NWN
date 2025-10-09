using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWScript.Constants;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Models;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.ValueObjects;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Character.UI.ViewModel
{
    internal class PlayerStatusViewModel: GuiViewModelBase<PlayerStatusViewModel, IGuiPayload>,
        IGuiRefreshable<PlayerStatusRefreshEvent>
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies

        private IStatCalculationService StatCalculationService => _serviceProvider.GetRequiredService<IStatCalculationService>();
        private ICharacterResourceService CharacterResourceService => _serviceProvider.GetRequiredService<ICharacterResourceService>();
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();

        public PlayerStatusViewModel(
            IGuiService guiService, 
            IDatabaseService db, 
            IServiceProvider serviceProvider) : base(guiService)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            // Services are now lazy-loaded via IServiceProvider
        }
        
        private int _screenHeight;
        private int _screenWidth;
        private int _screenScale;

        private static readonly GuiColor _hpColor = new(139, 0, 0);
        private static readonly GuiColor _stmColor = new(0, 104, 0);
        private static readonly GuiColor _fpColor = new(3, 87, 152);

        private static readonly GuiColor _shieldColor = new(3, 87, 152);
        private static readonly GuiColor _hullColor = new(139, 0, 0);
        private static readonly GuiColor _capacitorColor = new(166, 111, 0);

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

        protected override void Initialize(IGuiPayload initialPayload)
        {
            _screenHeight = -1;
            _screenScale = -1;
            _screenWidth = -1;
            
            UpdateWidget();
            UpdateAllData();
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

        private void UpdateShield(ShipStatus shipStatus)
        {
            var currentShields = shipStatus.Shield;
            var maxShields = shipStatus.MaxShield;
            Bar1Value = $"{currentShields} / {maxShields}";
            Bar1Progress = maxShields <= 0 ? 0 : (float)currentShields / (float)maxShields > 1.0f ? 1.0f : (float)currentShields / (float)maxShields;
        }

        private void UpdateHull(ShipStatus shipStatus)
        {
            var currentHull = shipStatus.Hull;
            var maxHull = shipStatus.MaxHull;
            Bar2Value = $"{currentHull} / {maxHull}";
            Bar2Progress = maxHull <= 0 ? 0 : (float)currentHull / (float)maxHull > 1.0f ? 1.0f : (float)currentHull / (float)maxHull;
        }

        private void UpdateCapacitor(ShipStatus shipStatus)
        {
            var currentCapacitor = shipStatus.Capacitor;
            var maxCapacitor = shipStatus.MaxCapacitor;
            Bar3Value = $"{currentCapacitor} / {maxCapacitor}";
            Bar3Progress = maxCapacitor <= 0 ? 0 : (float)currentCapacitor / (float)maxCapacitor > 1.0f ? 1.0f : (float)currentCapacitor / (float)maxCapacitor;
        }

        private void UpdateHP()
        {
            var currentHP = CharacterResourceService.GetCurrentHP(Player);
            var maxHP = GetMaxHitPoints(Player);

            Bar1Value = $"{currentHP} / {maxHP}";
            Bar1Progress = maxHP <= 0 ? 0 : (float)currentHP / (float)maxHP > 1.0f ? 1.0f : (float)currentHP / (float)maxHP;
        }

        private void UpdateFP()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = _db.Get<Player>(playerId);
            var currentFP = dbPlayer.FP;
            var maxFP = StatCalculationService.CalculateMaxFP(Player);
            var isStandard = dbPlayer.CharacterType == CharacterType.Standard;
            Bar3Value = isStandard ? "0 / 0" : $"{currentFP} / {maxFP}";
            Bar3Progress = maxFP <= 0 || isStandard ? 0 : (float)currentFP / (float)maxFP > 1.0f ? 1.0f : (float)currentFP / (float)maxFP;
        }

        private void UpdateSTM()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = _db.Get<Player>(playerId);
            var currentSTM = dbPlayer.Stamina;
            var maxSTM = StatCalculationService.CalculateMaxSTM(Player);

            Bar2Value = $"{currentSTM} / {maxSTM}";
            Bar2Progress = maxSTM <= 0 ? 0 : (float)currentSTM / (float)maxSTM > 1.0f ? 1.0f : (float)currentSTM / (float)maxSTM;
        }

        private void UpdateSingleData(PlayerStatusRefreshEvent.StatType type)
        {
            if (SpaceService.IsPlayerInSpaceMode(Player))
            {
                ToggleLabels(false);
                Bar1Color = _shieldColor;
                Bar2Color = _hullColor;
                Bar3Color = _capacitorColor;

                var shipStatus = SpaceService.GetShipStatus(Player);

                if (type == PlayerStatusRefreshEvent.StatType.Shield)
                {
                    UpdateShield(shipStatus);
                }
                else if (type == PlayerStatusRefreshEvent.StatType.Hull)
                {
                    UpdateHull(shipStatus);
                }
                else if (type == PlayerStatusRefreshEvent.StatType.Capacitor)
                {
                    UpdateCapacitor(shipStatus);
                }
            }
            else
            {
                ToggleLabels(true);
                Bar1Color = _hpColor;
                Bar2Color = _stmColor;
                Bar3Color = _fpColor;

                if (type == PlayerStatusRefreshEvent.StatType.HP)
                {
                    UpdateHP();
                }
                else if (type == PlayerStatusRefreshEvent.StatType.FP)
                {
                    UpdateFP();
                }
                else if (type == PlayerStatusRefreshEvent.StatType.STM)
                {
                    UpdateSTM();
                }
            }
        }

        private void UpdateAllData()
        {
            if (SpaceService.IsPlayerInSpaceMode(Player))
            {
                ToggleLabels(false);
                Bar1Color = _shieldColor;
                Bar2Color = _hullColor;
                Bar3Color = _capacitorColor;
                var shipStatus = SpaceService.GetShipStatus(Player);
                UpdateShield(shipStatus);
                UpdateHull(shipStatus);
                UpdateCapacitor(shipStatus);
            }
            else
            {
                ToggleLabels(true);
                Bar1Color = _hpColor;
                Bar2Color = _stmColor;
                Bar3Color = _fpColor;
                UpdateHP();
                UpdateFP();
                UpdateSTM();
            }
        }

        public void Refresh(PlayerStatusRefreshEvent payload)
        {
            UpdateWidget();
            UpdateSingleData(payload.Type);
        }
    }
}

