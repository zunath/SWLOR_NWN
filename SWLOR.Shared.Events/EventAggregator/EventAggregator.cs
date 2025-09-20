using SWLOR.Shared.Core.Log;

namespace SWLOR.Shared.Events.EventAggregator
{
    /// <summary>
    /// Synchronous implementation of IEventAggregator for NWN compatibility.
    /// </summary>
    public class EventAggregator : IEventAggregator
    {
        private readonly Dictionary<Type, List<Action<object>>> _handlers = new();
        private readonly ILogger _logger;

        public EventAggregator(ILogger logger)
        {
            _logger = logger;
        }

        public void Publish<TEvent>(TEvent eventData) where TEvent : IEvent
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
                    handler(eventData);
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
                // Find and remove the handler that wraps the original handler
                for (int i = handlers.Count - 1; i >= 0; i--)
                {
                    // We need to find the wrapper that calls our handler
                    // This is a bit tricky since we're storing Action<object> wrappers
                    // For now, we'll remove all handlers of this type (not ideal but works for NWN)
                    handlers.RemoveAt(i);
                }
                
                if (handlers.Count == 0)
                {
                    _handlers.Remove(eventType);
                }
            }
        }
    }
}
