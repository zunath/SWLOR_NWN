using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public class AbilityRecastCleanup
    {
        [NWNEventHandler("interval_pc_1s")]
        public static void CleanUpExpiredRecastTimers()
        {
            var player = OBJECT_SELF;
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var now = DateTime.UtcNow;

            foreach (var (group, dateTime) in dbPlayer.RecastTimes)
            {
                if(dateTime > now) continue;

                dbPlayer.RecastTimes.Remove(group);
            }

            DB.Set(playerId, dbPlayer);
        }
    }
}
