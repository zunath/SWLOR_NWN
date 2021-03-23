using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class DebuggingTools
    {
        [NWNEventHandler("test1")]
        public static void LeaveSpace()
        {
            var player = GetLastUsedBy();

            Space.ExitSpaceMode(player);

            var wp = GetWaypointByTag("V_Colony_Exit");
            var location = GetLocation(wp);

            AssignCommand(player, () => ActionJumpToLocation(location));
        }

    }
}
