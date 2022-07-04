using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature
{
    public static class PlayerStatusWindow
    {
        [NWNEventHandler("interval_pc_1s")]
        public static void RefreshPlayerStatus()
        {
            var player = OBJECT_SELF;
            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent());
        }

        [NWNEventHandler("mod_enter")]
        [NWNEventHandler("area_enter")]
        public static void LoadPlayerStatusWindow()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            if(!Gui.IsWindowOpen(player, GuiWindowType.PlayerStatus))
                Gui.TogglePlayerWindow(player, GuiWindowType.PlayerStatus);
        }
    }
}
