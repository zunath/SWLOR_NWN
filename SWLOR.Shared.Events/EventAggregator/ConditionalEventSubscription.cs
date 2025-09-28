using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.Events.EventAggregator
{
    internal class ConditionalEventSubscription<TEvent> : IDisposable where TEvent : IEvent
    {
        private readonly EventAggregator _aggregator;
        private readonly Func<TEvent, bool> _handler;
        private bool _disposed = false;

        public ConditionalEventSubscription(EventAggregator aggregator, Func<TEvent, bool> handler)
        {
            _aggregator = aggregator;
            _handler = handler;
        }

        public void Unsubscribe()
        {
            if (!_disposed)
            {
                _aggregator.RemoveConditionalHandler<TEvent>(_handler);
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Unsubscribe();
        }
    }
}
