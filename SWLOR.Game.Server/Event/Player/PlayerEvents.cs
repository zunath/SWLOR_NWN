using System;
using System.Collections.Generic;
using System.Text;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Event.Player
{
    public static class PlayerEvents
    {
        public static void OnDamaged()
        {
            MessageHub.Instance.Publish(new OnPlayerDamaged());
        }

        public static void OnHeartbeat()
        {
            MessageHub.Instance.Publish(new OnPlayerHeartbeat());
        }
    }
}
