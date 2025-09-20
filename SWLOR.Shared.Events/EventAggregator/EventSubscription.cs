using System;

namespace SWLOR.Shared.Events.EventAggregator
{
    internal class EventSubscription<TEvent> : IDisposable where TEvent : IEvent
    {
        private readonly EventAggregator _aggregator;
        private readonly Action<TEvent> _handler;
        private bool _disposed = false;

        public EventSubscription(EventAggregator aggregator, Action<TEvent> handler)
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
