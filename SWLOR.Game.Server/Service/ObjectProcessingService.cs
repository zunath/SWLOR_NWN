using System;
using NWN;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Module;
using SWLOR.Game.Server.Processor;
using SWLOR.Game.Server.Processor.Contracts;

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
            RegisterProcessingEvent<AppStateProcessor>();
            RegisterProcessingEvent<ServerRestartProcessor>();
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
                AppCache.ProcessingEvents.Remove(toUnregister);
            }
            AppCache.UnregisterProcessingEvents.Clear();

            foreach (var @event in AppCache.ProcessingEvents)
            {
                try
                {
                    App.ResolveByInterface<IEventProcessor>(@event.Value.ProcessorType.ToString(), (processor) =>
                    {
                        processor.Run(@event.Value.Args);
                    });
                }
                catch (Exception ex)
                {
                    ErrorService.LogError(ex, "ObjectProcessingService. EventID = " + @event.Key);
                }
            }
        }

        public static string RegisterProcessingEvent<T>(params object[] args)
            where T: IEventProcessor
        {
            string globalID = Guid.NewGuid().ToString();
            ProcessingEvent @event = new ProcessingEvent(typeof(T), args);
            AppCache.ProcessingEvents.Add(globalID, @event);
            return globalID;
        }

        public static void UnregisterProcessingEvent(string globalID)
        {
            if (AppCache.ProcessingEvents.ContainsKey(globalID))
            {
                AppCache.UnregisterProcessingEvents.Enqueue(globalID);
            }
        }
    }
}
