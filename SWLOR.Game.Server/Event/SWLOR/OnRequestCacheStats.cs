using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
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
