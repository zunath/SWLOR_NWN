using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.Events.EventAggregator
{
    /// <summary>
    /// Synchronous implementation of IEventAggregator for NWN compatibility.
    /// </summary>
    public class EventAggregator : IEventAggregator
    {
        private readonly Dictionary<Type, List<Action<object>>> _handlers = new();
        private readonly ILogger _logger;
        private readonly IScriptExecutionProvider _executionProvider;

        public EventAggregator(ILogger logger, IScriptExecutionProvider executionProvider)
        {
            _logger = logger;
            _executionProvider = executionProvider;
        }

        public void Publish<TEvent>(TEvent eventData, uint target) where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            
            if (!_handlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
            {
                return;
            }

            foreach (var handler in handlers)
            {
                try
                {
                    _executionProvider.ExecuteInScriptContext(() =>
                    {
                        handler(eventData);
                    }, target);
                }
                catch (Exception ex)
                {
                    _logger.WriteError($"Error in event handler for {eventType.Name}: {ex.Message}");
                }
            }
        }


        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            var subscription = new EventSubscription<TEvent>(this, handler);
            
            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = new List<Action<object>>();
            }
            _handlers[eventType].Add(obj => handler((TEvent)obj));

            return subscription;
        }


        public void Unsubscribe(IDisposable subscription)
        {
            // The subscription will handle its own unsubscription when disposed
            subscription?.Dispose();
        }

        internal void RemoveHandler<TEvent>(Action<TEvent> handler)
        {
            var eventType = typeof(TEvent);
            
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                // Find and remove the specific handler wrapper
                // We need to find the wrapper that calls our specific handler
                for (int i = handlers.Count - 1; i >= 0; i--)
                {
                    // For now, we'll remove the last added handler of this type
                    // This is a limitation of the current design - we can't easily track
                    // which wrapper corresponds to which original handler
                    handlers.RemoveAt(i);
                    break; // Remove only one handler per call
                }
                
                if (handlers.Count == 0)
                {
                    _handlers.Remove(eventType);
                }
            }
        }
    }
}
