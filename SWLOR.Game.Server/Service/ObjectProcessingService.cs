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
        private readonly AppState _state;
        private readonly IErrorService _error;

        public ObjectProcessingService(INWScript script,
            AppState state,
            IErrorService error)
        {
            _ = script;
            _state = state;
            _error = error;
        }

        public void OnModuleLoad()
        {
            RunProcessor();
        }

        public float ProcessingTickInterval => 1f;

        private void RunProcessor()
        {
            foreach (var toUnregister in _state.UnregisterProcessingEvents)
            {
                _state.ProcessingEvents.Remove(toUnregister);
            }
            _state.UnregisterProcessingEvents.Clear();

            foreach (var @event in _state.ProcessingEvents)
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
            string globalID = Guid.NewGuid().ToString("N");
            ProcessingEvent @event = new ProcessingEvent(typeof(T), args);
            _state.ProcessingEvents.Add(globalID, @event);
            return globalID;
        }

        public void UnregisterProcessingEvent(string globalID)
        {
            if (_state.ProcessingEvents.ContainsKey(globalID))
            {
                _state.UnregisterProcessingEvents.Enqueue(globalID);
            }
        }
    }
}
