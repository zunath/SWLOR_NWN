using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnAreaInstanceCreated
    {
        public NWArea Instance { get; set; }

        public OnAreaInstanceCreated(NWArea instance)
        {
            Instance = instance;
        }
    }
}
