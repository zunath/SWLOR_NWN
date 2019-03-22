using System;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class ObjectProcessingService : IObjectProcessingService
    {
        private readonly INWScript _;
        private readonly IErrorService _error;
        private DateTime _dateLastRun;

        public ObjectProcessingService(INWScript script,
            IErrorService error)
        {
            _ = script;
            _error = error;
            _dateLastRun = DateTime.UtcNow;
        }

        public void OnModuleLoad()
        {
            Events.MainLoopTick += Events_MainLoopTick;
        }

        private void Events_MainLoopTick(ulong frame)
        {
            RunProcessor();
        }

        public float ProcessingTickInterval => 1f;

        private void RunProcessor()
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
                    _error.LogError(ex, "ObjectProcessingService. EventID = " + @event.Key);
                }
            }
        }

        public string RegisterProcessingEvent<T>(params object[] args)
            where T: IEventProcessor
        {
            string globalID = Guid.NewGuid().ToString();
            ProcessingEvent @event = new ProcessingEvent(typeof(T), args);
            AppCache.ProcessingEvents.Add(globalID, @event);
            return globalID;
        }

        public void UnregisterProcessingEvent(string globalID)
        {
            if (AppCache.ProcessingEvents.ContainsKey(globalID))
            {
                AppCache.UnregisterProcessingEvents.Enqueue(globalID);
            }
        }
    }
}
