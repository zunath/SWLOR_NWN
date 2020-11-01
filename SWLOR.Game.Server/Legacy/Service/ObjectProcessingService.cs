using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.Event.SWLOR;
using SWLOR.Game.Server.Legacy.Messaging;

namespace SWLOR.Game.Server.Legacy.Service
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
            Scheduler.ScheduleRepeating(RunProcessor, TimeSpan.FromSeconds(1));
        }

        public static float ProcessingTickInterval => 1f;

        private static void RunProcessor()
        {
            var now = DateTime.UtcNow;
            var delta = now - _dateLastRun;
            if (delta.Seconds < 1) return;
            _dateLastRun = now;
            
            MessageHub.Instance.Publish(new OnObjectProcessorRan());
        }
    }
}
