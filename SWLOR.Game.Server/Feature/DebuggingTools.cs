using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Core.NWScript.Enum;
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
            var window = new GuiWindow();
            var column = new GuiColumn();
            var row = new GuiRow();

            row.Elements.Add(new GuiButton
            {
                Text = "Button name"
            });

            row.Elements.Add(new GuiLabel
            {
                Text = "My Special Label"
            });


            column.Rows.Add(row);

            window.Columns.Add(column);

            var json = window.Build();
            
            NuiCreate(GetLastUsedBy(), json, "test_window");
        }

    }
}
