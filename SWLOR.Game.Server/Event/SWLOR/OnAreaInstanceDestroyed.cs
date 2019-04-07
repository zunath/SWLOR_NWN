using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnAreaInstanceDestroyed
    {
        public NWArea Instance { get; set; }

        public OnAreaInstanceDestroyed(NWArea instance)
        {
            Instance = instance;
        }
    }
}
