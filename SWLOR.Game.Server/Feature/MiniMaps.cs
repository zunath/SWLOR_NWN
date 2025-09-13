using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Feature
{
    public static class MiniMaps
    {
        private const string AreaMiniMapVariable = "MINI_MAP_DISABLED";

        /// <summary>
        /// If a player enters an area with a disabled mini-map and they do not have the map key item, disable the window.
        /// If a player enters an area with the associated map key item, fully explore it for them.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void DisableMiniMap()
        {
            var area = OBJECT_SELF;
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var isMiniMapDisabled = GetLocalInt(area, AreaMiniMapVariable);
            if (isMiniMapDisabled == 1)
            {
                SetGuiPanelDisabled(player, GuiPanel.Minimap, true);
            }

            var keyItemId = GetLocalInt(area, "MAP_KEY_ITEM_ID");
            if (keyItemId > 0)
            {
                var keyItemType = (KeyItemType)keyItemId;
                var hasKeyItem = KeyItem.HasKeyItem(player, keyItemType);

                if (hasKeyItem)
                {
                    SetGuiPanelDisabled(player, GuiPanel.Minimap, false);
                    ExploreAreaForPlayer(area, player);
                }
            }
        }

        /// <summary>
        /// Ensures the mini-map is always re-enabled when leaving an area.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void EnableMiniMap()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player)) return;

            SetGuiPanelDisabled(player, GuiPanel.Minimap, false);
        }

        /// <summary>
        /// Skips the character sheet panel open event and shows the SWLOR character sheet instead.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleGuiEvent)]
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
