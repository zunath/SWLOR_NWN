using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Messaging.Messages
{
    public class AreaInstanceDestroyedMessage
    {
        public NWArea Instance { get; set; }

        public AreaInstanceDestroyedMessage(NWArea instance)
        {
            Instance = instance;
        }
    }
}
