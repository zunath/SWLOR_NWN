using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.Events.Contracts
{
    /// <summary>
    /// Interface for the abstracted event system that hides NWN script implementation details.
    /// This provides a clean API for components to publish and subscribe to events.
    /// </summary>
    internal interface IEventService
    {
        /// <summary>
        /// Publishes an event to all subscribers.
        /// </summary>
        /// <typeparam name="T">The type of event to publish</typeparam>
        /// <param name="eventData">The event data to publish</param>
        /// <param name="target">The target of the event</param>
        void Publish<T>(T eventData, uint target) where T : IEvent;

        /// <summary>
        /// Subscribes to events of a specific type.
        /// </summary>
        /// <typeparam name="T">The type of event to subscribe to</typeparam>
        /// <param name="handler">The event handler</param>
        /// <returns>A disposable subscription that can be used to unsubscribe</returns>
        IDisposable Subscribe<T>(Action<T> handler) where T : IEvent;

        /// <summary>
        /// Subscribes to events with a specific event name.
        /// </summary>
        /// <typeparam name="T">The type of event to subscribe to</typeparam>
        /// <param name="eventName">The name of the event to subscribe to</param>
        /// <param name="handler">The event handler</param>
        /// <returns>A disposable subscription that can be used to unsubscribe</returns>
        IDisposable Subscribe<T>(string eventName, Action<T> handler) where T : IEvent;

        /// <summary>
        /// Unsubscribes from a specific subscription.
        /// </summary>
        /// <param name="subscription">The subscription to unsubscribe from</param>
        void Unsubscribe(IDisposable subscription);

        /// <summary>
        /// Gets the number of subscribers for a specific event type.
        /// </summary>
        /// <typeparam name="T">The type of event</typeparam>
        /// <returns>The number of subscribers</returns>
        int GetSubscriberCount<T>() where T : IEvent;

        /// <summary>
        /// Gets the number of subscribers for a specific event name.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <returns>The number of subscribers</returns>
        int GetSubscriberCount(string eventName);

        /// <summary>
        /// Checks if there are any subscribers for a specific event type.
        /// </summary>
        /// <typeparam name="T">The type of event</typeparam>
        /// <returns>True if there are subscribers, false otherwise</returns>
        bool HasSubscribers<T>() where T : IEvent;

        /// <summary>
        /// Checks if there are any subscribers for a specific event name.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <returns>True if there are subscribers, false otherwise</returns>
        bool HasSubscribers(string eventName);
    }
}
