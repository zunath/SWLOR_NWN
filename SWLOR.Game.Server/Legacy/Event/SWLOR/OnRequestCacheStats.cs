using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
{
    public class OnRequestCacheStats
    {
        public NWPlayer Player { get; set; }

        public OnRequestCacheStats(NWPlayer player)
        {
            Player = player;
        }
    }
}
