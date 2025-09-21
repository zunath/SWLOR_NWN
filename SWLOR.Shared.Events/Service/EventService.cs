using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Model;

namespace SWLOR.Shared.Events.Service
{
    /// <summary>
    /// Implementation of IEventSystem that provides an abstracted event system.
    /// This hides NWN script implementation details from consumers.
    /// </summary>
    public class EventService : IEventService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly Dictionary<string, List<Action<object>>> _namedEventHandlers = new();
        private readonly object _lock = new();

        public EventService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        }

        public void Publish<T>(T eventData) where T : IEvent
        {
            _eventAggregator.Publish(eventData);
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : IEvent
        {
            return _eventAggregator.Subscribe(handler);
        }

        public IDisposable Subscribe<T>(string eventName, Action<T> handler) where T : IEvent
        {
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException("Event name cannot be null or empty", nameof(eventName));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lock)
            {
                if (!_namedEventHandlers.ContainsKey(eventName))
                {
                    _namedEventHandlers[eventName] = new List<Action<object>>();
                }

                var wrapper = new Action<object>(obj => handler((T)obj));
                _namedEventHandlers[eventName].Add(wrapper);

                return new NamedEventSubscription<T>(this, eventName, wrapper);
            }
        }

        public void Unsubscribe(IDisposable subscription)
        {
            _eventAggregator.Unsubscribe(subscription);
        }

        public int GetSubscriberCount<T>() where T : IEvent
        {
            // This would require exposing internal state from EventAggregator
            // For now, return 0 as a placeholder
            return 0;
        }

        public int GetSubscriberCount(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                return 0;

            lock (_lock)
            {
                return _namedEventHandlers.TryGetValue(eventName, out var handlers) ? handlers.Count : 0;
            }
        }

        public bool HasSubscribers<T>() where T : IEvent
        {
            return GetSubscriberCount<T>() > 0;
        }

        public bool HasSubscribers(string eventName)
        {
            return GetSubscriberCount(eventName) > 0;
        }

        internal void RemoveNamedHandler<T>(string eventName, Action<object> handler)
        {
            lock (_lock)
            {
                if (_namedEventHandlers.TryGetValue(eventName, out var handlers))
                {
                    handlers.Remove(handler);
                    if (handlers.Count == 0)
                    {
                        _namedEventHandlers.Remove(eventName);
                    }
                }
            }
        }
    }

}
