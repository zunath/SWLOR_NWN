using System;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.KeyItemService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class DebuggingTools
    {
        [NWNEventHandler("test2")]
        public static void OpenMarketBuy()
        {
            Gui.TogglePlayerWindow(GetLastUsedBy(), GuiWindowType.MarketBuying);
        }

        [NWNEventHandler("test")]
        public static void OpenMarketListing()
        {
            Gui.TogglePlayerWindow(GetLastUsedBy(), GuiWindowType.MarketListing);
        }

    }
}
