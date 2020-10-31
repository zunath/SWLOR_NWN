using System;
using System.Collections.Generic;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Service.Legacy;

namespace SWLOR.Game.Server.Event.Area
{
    public static class AreaEvents
    {
        [NWNEventHandler("area_enter")]
        public static void OnAreaEnter()
        {
            BaseService.OnAreaEnter();
            MessageHub.Instance.Publish(new OnAreaEnter());
        }

        [NWNEventHandler("area_exit")]
        public static void OnAreaExit()
        {
            MessageHub.Instance.Publish(new OnAreaExit());
        }

        [NWNEventHandler("area_heartbeat")]
        public static void OnAreaHeartbeat()
        {
            MessageHub.Instance.Publish(new OnAreaHeartbeat());
        }

        [NWNEventHandler("area_user_def")]
        public static void OnAreaUserDefined()
        {
            MessageHub.Instance.Publish(new OnAreaUserDefined());
        }
    }
}
