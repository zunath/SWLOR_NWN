using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnAreaInstanceDestroyed
    {
        public uint Instance { get; set; }

        public OnAreaInstanceDestroyed(uint instance)
        {
            Instance = instance;
        }
    }
}
