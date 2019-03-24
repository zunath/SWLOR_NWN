using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Module;
using SWLOR.Game.Server.Processor;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public static class ObjectProcessingService
    {
        private static readonly Dictionary<string, IEventProcessor> _processingEvents;
        private static DateTime _dateLastRun;

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
        }

        static ObjectProcessingService()
        {
            _dateLastRun = DateTime.UtcNow;
            _processingEvents = new Dictionary<string, IEventProcessor>();
        }

        private static void OnModuleLoad()
        {
            RegisterProcessingEvent(new AppStateProcessor());
            RegisterProcessingEvent(new ServerRestartProcessor());
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
            
            foreach (var toUnregister in AppCache.UnregisterProcessingEvents)
            {
                _processingEvents.Remove(toUnregister);
            }
            AppCache.UnregisterProcessingEvents.Clear();

            foreach (var @event in _processingEvents.Values)
            {
                try
                {
                    @event.Run();
                }
                catch (Exception ex)
                {
                    LoggingService.LogError(ex, "ObjectProcessingService. Event = " + @event);
                }
            }
        }

        public static string RegisterProcessingEvent(IEventProcessor processor)
        {
            string globalID = Guid.NewGuid().ToString();
            _processingEvents.Add(globalID, processor);
            return globalID;
        }

        public static void UnregisterProcessingEvent(string globalID)
        {
            if (_processingEvents.ContainsKey(globalID))
            {
                AppCache.UnregisterProcessingEvents.Enqueue(globalID);
            }
        }
    }
}
