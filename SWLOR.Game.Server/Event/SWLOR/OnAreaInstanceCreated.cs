using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnAreaInstanceCreated
    {
        public uint Instance { get; set; }

        public OnAreaInstanceCreated(uint instance)
        {
            Instance = instance;
        }
    }
}
