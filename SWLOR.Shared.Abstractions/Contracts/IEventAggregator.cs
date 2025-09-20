namespace SWLOR.Shared.Events.EventAggregator
{
    /// <summary>
    /// Strongly-typed event aggregator for SWLOR events.
    /// Provides type-safe event publishing and subscription.
    /// </summary>
    public interface IEventAggregator
    {
        /// <summary>
        /// Publishes an event to all registered subscribers.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to publish</typeparam>
        /// <param name="eventData">The event data to publish</param>
        void Publish<TEvent>(TEvent eventData) where TEvent : IEvent;

        /// <summary>
        /// Subscribes to events of a specific type.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to subscribe to</typeparam>
        /// <param name="handler">The event handler</param>
        /// <returns>A subscription token for unsubscribing</returns>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;

        /// <summary>
        /// Unsubscribes from events using a subscription token.
        /// </summary>
        /// <param name="subscription">The subscription token</param>
        void Unsubscribe(IDisposable subscription);
    }
}
