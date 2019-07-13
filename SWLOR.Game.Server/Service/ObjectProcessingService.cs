using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Processor;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public static class ObjectProcessingService
    {
        private static DateTime _dateLastRun;

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
        }

        static ObjectProcessingService()
        {
            _dateLastRun = DateTime.UtcNow;
        }

        private static void OnModuleLoad()
        {
            Events.MainLoopTick += Events_MainLoopTick;
        }
        
        private static void Events_MainLoopTick(ulong frame)
        {
            RunProcessor();
        }

        public static float ProcessingTickInterval => 1f;

        private static void RunProcessor()
        {
            var delta = DateTime.UtcNow - _dateLastRun;
            if (delta.Seconds < 1) return;
            _dateLastRun = DateTime.UtcNow;
            
            MessageHub.Instance.Publish(new OnObjectProcessorRan());
        }
    }
}
