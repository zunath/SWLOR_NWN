using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
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
            var builder = new TestWindowGuiDefinition();
            var windows = builder.BuildWindows();

            var json = windows[GuiWindowType.TestWindow].Build();
            Console.WriteLine(JsonDump(json));

            NuiCreate(GetLastUsedBy(), json, "test_window");
        }

    }
}
