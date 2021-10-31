using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.KeyItemService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class DebuggingTools
    {
        [NWNEventHandler("test2")]
        public static void OpenMarketBuy()
        {
        }

        [NWNEventHandler("test")]
        public static void OpenMarketListing()
        {
            KeyItem.GiveKeyItem(GetLastUsedBy(), KeyItemType.CZ220ShuttlePass);
        }

    }
}
