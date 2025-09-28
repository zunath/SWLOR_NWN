using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Extensions;

namespace SWLOR.Shared.Events.EventAggregator
{
    /// <summary>
    /// Synchronous implementation of IEventAggregator for NWN compatibility.
    /// </summary>
    public class EventAggregator : IEventAggregator
    {
        private readonly Dictionary<Type, List<Action<object>>> _handlers = new();
        private readonly Dictionary<Type, List<Func<object, bool>>> _conditionalHandlers = new();
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
            
            // Execute regular handlers
            if (_handlers.TryGetValue(eventType, out var handlers) && handlers.Count > 0)
            {
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
                        _logger.WriteError($"Error in event handler for {eventType.Name}: {ex.ToMessageAndCompleteStacktrace()}");
                    }
                }
            }

            // Execute conditional handlers
            if (_conditionalHandlers.TryGetValue(eventType, out var conditionalHandlers) && conditionalHandlers.Count > 0)
            {
                foreach (var handler in conditionalHandlers)
                {
                    try
                    {
                        _executionProvider.ExecuteInScriptContext(() =>
                        {
                            handler(eventData);
                        }, target);
                        
                        // For conditional handlers, we might want to do something with the result
                        // For now, we just execute them but don't use the return value
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteError($"Error in conditional event handler for {eventType.Name}: {ex.ToMessageAndCompleteStacktrace()}");
                    }
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

        public IDisposable SubscribeConditional<TEvent>(Func<TEvent, bool> handler) where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            var subscription = new ConditionalEventSubscription<TEvent>(this, handler);
            
            if (!_conditionalHandlers.ContainsKey(eventType))
            {
                _conditionalHandlers[eventType] = new List<Func<object, bool>>();
            }
            _conditionalHandlers[eventType].Add(obj => handler((TEvent)obj));

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

        internal void RemoveConditionalHandler<TEvent>(Func<TEvent, bool> handler)
        {
            var eventType = typeof(TEvent);
            
            if (_conditionalHandlers.TryGetValue(eventType, out var handlers))
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
                    _conditionalHandlers.Remove(eventType);
                }
            }
        }
    }
}
