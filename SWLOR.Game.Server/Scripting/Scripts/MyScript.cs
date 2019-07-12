using System;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Scripting.Contracts;

namespace SWLOR.Game.Server.Scripting.Scripts
{
    public class MyScript : IScript
    {
        private Guid _onHeartbeatID;

        public void SubscribeEvents()
        {
            _onHeartbeatID = MessageHub.Instance.Subscribe<OnModuleHeartbeat>(OnModuleHeartbeat);

        }

        public void UnsubscribeEvents()
        {
            MessageHub.Instance.Unsubscribe(_onHeartbeatID);
        }

        private void OnModuleHeartbeat(OnModuleHeartbeat hb)
        {
            Console.WriteLine("Hello from HB script");
        }
    }
}