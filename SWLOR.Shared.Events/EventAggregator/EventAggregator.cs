using System.Collections.Concurrent;

namespace SWLOR.Shared.Events.EventAggregator
{
    /// <summary>
    /// Thread-safe implementation of IEventAggregator.
    /// </summary>
    public class EventAggregator : IEventAggregator
    {
        private readonly ConcurrentDictionary<Type, List<object>> _handlers = new();
        private readonly object _lock = new object();
        private readonly ILogger _logger;

        public EventAggregator(ILogger logger)
        {
            _logger = logger;
        }

        public void Publish<TEvent>(TEvent eventData) where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            
            if (!_handlers.TryGetValue(eventType, out var handlers) || !handlers.Any())
            {
                return;
            }

            foreach (var handler in handlers.ToList())
            {
                try
                {
                    if (handler is Action<TEvent> syncHandler)
                    {
                        syncHandler(eventData);
                    }
                    else if (handler is Func<TEvent, Task> asyncHandler)
                    {
                        // Run async handlers synchronously for now
                        // In a real implementation, you might want to use Task.Run or similar
                        asyncHandler(eventData).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in event handler for {eventType.Name}: {ex.Message}", ex);
                }
            }
        }

        public async Task PublishAsync<TEvent>(TEvent eventData) where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            
            if (!_handlers.TryGetValue(eventType, out var handlers) || !handlers.Any())
            {
                return;
            }

            var tasks = new List<Task>();
            
            foreach (var handler in handlers.ToList())
            {
                try
                {
                    if (handler is Action<TEvent> syncHandler)
                    {
                        syncHandler(eventData);
                    }
                    else if (handler is Func<TEvent, Task> asyncHandler)
                    {
                        tasks.Add(asyncHandler(eventData));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in event handler for {eventType.Name}: {ex.Message}", ex);
                }
            }

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            var subscription = new EventSubscription<TEvent>(this, handler);
            
            lock (_lock)
            {
                if (!_handlers.ContainsKey(eventType))
                {
                    _handlers[eventType] = new List<object>();
                }
                _handlers[eventType].Add(handler);
            }

            return subscription;
        }

        public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            var subscription = new EventSubscription<TEvent>(this, handler);
            
            lock (_lock)
            {
                if (!_handlers.ContainsKey(eventType))
                {
                    _handlers[eventType] = new List<object>();
                }
                _handlers[eventType].Add(handler);
            }

            return subscription;
        }

        public void Unsubscribe(IDisposable subscription)
        {
            if (subscription is EventSubscription<TEvent> eventSubscription)
            {
                eventSubscription.Unsubscribe();
            }
        }

        internal void RemoveHandler<TEvent>(object handler)
        {
            var eventType = typeof(TEvent);
            
            lock (_lock)
            {
                if (_handlers.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(handler);
                    if (!handlers.Any())
                    {
                        _handlers.TryRemove(eventType, out _);
                    }
                }
            }
        }
    }

    internal class EventSubscription<TEvent> : IDisposable where TEvent : IEvent
    {
        private readonly EventAggregator _aggregator;
        private readonly object _handler;
        private bool _disposed = false;

        public EventSubscription(EventAggregator aggregator, object handler)
        {
            _aggregator = aggregator;
            _handler = handler;
        }

        public void Unsubscribe()
        {
            if (!_disposed)
            {
                _aggregator.RemoveHandler<TEvent>(_handler);
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Unsubscribe();
        }
    }
}
