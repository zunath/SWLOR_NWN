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
        private readonly AppCache _cache;
        private readonly IErrorService _error;

        public ObjectProcessingService(INWScript script,
            AppCache cache,
            IErrorService error)
        {
            _ = script;
            _cache = cache;
            _error = error;
        }

        public void OnModuleLoad()
        {
            RunProcessor();
        }

        public float ProcessingTickInterval => 1f;

        private void RunProcessor()
        {
            foreach (var toUnregister in _cache.UnregisterProcessingEvents)
            {
                _cache.ProcessingEvents.Remove(toUnregister);
            }
            _cache.UnregisterProcessingEvents.Clear();

            foreach (var @event in _cache.ProcessingEvents)
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

            _.DelayCommand(ProcessingTickInterval, RunProcessor);
        }

        public string RegisterProcessingEvent<T>(params object[] args)
            where T: IEventProcessor
        {
            string globalID = Guid.NewGuid().ToString();
            ProcessingEvent @event = new ProcessingEvent(typeof(T), args);
            _cache.ProcessingEvents.Add(globalID, @event);
            return globalID;
        }

        public void UnregisterProcessingEvent(string globalID)
        {
            if (_cache.ProcessingEvents.ContainsKey(globalID))
            {
                _cache.UnregisterProcessingEvents.Enqueue(globalID);
            }
        }
    }
}
