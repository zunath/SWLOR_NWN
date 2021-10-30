using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
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
            Quest.AcceptQuest(GetLastUsedBy(), "selan_request");
            Quest.AcceptQuest(GetLastUsedBy(), "cz220_smithery");
            Quest.AcceptQuest(GetLastUsedBy(), "cz220_scavenging");
            Quest.AcceptQuest(GetLastUsedBy(), "cz220_fabrication");
        }

    }
}
