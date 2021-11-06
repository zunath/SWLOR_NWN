using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class MiniMaps
    {
        private const string AreaMiniMapVariable = "MINI_MAP_DISABLED";

        /// <summary>
        /// If a player enters an area with a disabled mini-map,
        /// disable it.
        /// </summary>
        [NWNEventHandler("area_enter")]
        public static void DisableMiniMap()
        {
            var area = OBJECT_SELF;
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var isMiniMapDisabled = GetLocalInt(area, AreaMiniMapVariable);
            if (isMiniMapDisabled != 1) return;

            SetGuiPanelDisabled(player, GuiPanel.Minimap, true);
        }

        /// <summary>
        /// Ensures the mini-map is always re-enabled when leaving an area.
        /// </summary>
        [NWNEventHandler("area_exit")]
        public static void EnableMiniMap()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player)) return;

            SetGuiPanelDisabled(player, GuiPanel.Minimap, false);
        }

        /// <summary>
        /// Skips the character sheet panel open event and shows the SWLOR character sheet instead.
        /// </summary>
        [NWNEventHandler("mod_gui_event")]
        public static void MiniMapGui()
        {
            var player = GetLastGuiEventPlayer();
            var type = GetLastGuiEventType();
            var panelType = (GuiPanel)GetLastGuiEventInteger();
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (type != GuiEventType.DisabledPanelAttemptOpen) return;
            if (panelType != GuiPanel.Minimap) return;

            var area = GetArea(player);
            if (GetLocalInt(area, AreaMiniMapVariable) != 1) return;

            SendMessageToPC(player, ColorToken.Red("You do not have a map of this area."));
        }
    }
}
