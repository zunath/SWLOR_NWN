using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Messaging.Messages
{
    public class AreaInstanceCreatedMessage
    {
        public NWArea Instance { get; set; }

        public AreaInstanceCreatedMessage(NWArea instance)
        {
            Instance = instance;
        }
    }
}
