using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class DebuggingTools
    {
        [NWNEventHandler("test2")]
        public static void KillMe()
        {
            var player = GetLastUsedBy();

            Space.ApplyShipDamage(player, player, 999);
        }

        [NWNEventHandler("test_window")]
        public static void TestWindow()
        {
            var player = GetLastUsedBy();

            Gui.ShowPlayerWindow(player, GuiWindowType.TestWindow);
        }

    }
}
