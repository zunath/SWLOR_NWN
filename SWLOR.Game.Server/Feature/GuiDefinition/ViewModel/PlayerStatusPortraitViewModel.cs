using System;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// Renders thin Stamina and FP bars overlaid on the lower portion of the character portrait.
    /// HP is intentionally omitted because the engine already draws the native red HP bar on the
    /// portrait's edge. This is the alternative to the docked HP/STM/FP window (see PlayerStatusViewModel).
    /// </summary>
    internal class PlayerStatusPortraitViewModel : GuiViewModelBase<PlayerStatusPortraitViewModel, GuiPayloadBase>,
        IGuiRefreshable<PlayerStatusRefreshEvent>
    {
        private int _screenHeight;
        private int _screenWidth;
        private int _screenScale;

        private static readonly GuiColor _stmColor = new GuiColor(0, 104, 0);
        private static readonly GuiColor _fpColor = new GuiColor(3, 87, 152);

        public GuiColor StaminaColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public GuiColor FPColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string StaminaValue
        {
            get => Get<string>();
            set => Set(value);
        }

        public string FPValue
        {
            get => Get<string>();
            set => Set(value);
        }

        public float StaminaProgress
        {
            get => Get<float>();
            set => Set(value);
        }

        public float FPProgress
        {
            get => Get<float>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _screenHeight = -1;
            _screenScale = -1;
            _screenWidth = -1;

            StaminaColor = _stmColor;
            FPColor = _fpColor;

            UpdateWidget();
            UpdateSTM();
            UpdateFP();
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
                // Content dimensions of the overlay. These are logical units that the engine scales
                // by the player's GUI scale; the anchor offsets below are multiplied by that same
                // scale so the bars stay pinned over the portrait at any resolution or zoom.
                // Height must clear BOTH 23f bar rows (46f) plus whatever inter-row spacing NUI's
                // native "col" auto-layout inserts between them (see PlayerStatusPortraitDefinition).
                // That spacing is a small fixed-pixel gap the engine applies outside of our control,
                // not a logical unit we scale here, so it eats a larger share of a tight budget at
                // some GUI scales than others. Previously this was 52f (only 6f over the row sum),
                // which was reported clipping the bottom (FP) row at some scales; bumped to 60f to
                // give the same kind of safety margin the rows themselves carry over their bars.
                const float WidgetWidth = 72f;
                const float WidgetHeight = 60f;

                // Distance (at 100% scale) from the right/top screen edges to the overlay's top-left
                // corner. Tuned so the two bars sit across the lower half of the native portrait,
                // clear of the HP bar on the portrait's left edge and the button row beneath it.
                // Note: SWLOR's GetPlayerDeviceProperty returns physical pixels, so the anchor is
                // (edge - offset*scale) here, matching PlayerStatusViewModel/TargetStatusViewModel.
                const float XOffset = 64f;
                const float YOffset = 54f;

                var scale = screenScale / 100f;
                var x = screenWidth - XOffset * scale;
                var y = YOffset * scale;

                Geometry = new GuiRectangle(x, y, WidgetWidth, WidgetHeight);

                _screenHeight = screenHeight;
                _screenWidth = screenWidth;
                _screenScale = screenScale;
            }
        }

        private void UpdateSTM()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            var currentSTM = dbPlayer.Stamina;
            var maxSTM = Stat.GetMaxStamina(Player, dbPlayer);

            StaminaValue = $"{currentSTM}";
            StaminaProgress = maxSTM <= 0 ? 0f : Math.Clamp((float)currentSTM / maxSTM, 0f, 1f);
        }

        private void UpdateFP()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            var isStandard = dbPlayer.CharacterType == CharacterType.Standard;
            var currentFP = dbPlayer.FP;
            var maxFP = Stat.GetMaxFP(Player, dbPlayer);

            FPValue = isStandard ? "0" : $"{currentFP}";
            FPProgress = maxFP <= 0 || isStandard ? 0f : Math.Clamp((float)currentFP / maxFP, 0f, 1f);
        }

        public void Refresh(PlayerStatusRefreshEvent payload)
        {
            // The portrait overlay only represents on-foot vitals. In space mode the docked window
            // takes over to show shield/hull/capacitor, so ignore refreshes here.
            if (Space.IsPlayerInSpaceMode(Player))
                return;

            UpdateWidget();

            if (payload.Type == PlayerStatusRefreshEvent.StatType.STM)
                UpdateSTM();
            else if (payload.Type == PlayerStatusRefreshEvent.StatType.FP)
                UpdateFP();
        }
    }
}
