using System;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Service;

namespace SWLOR.Shared.Events.Model
{
    /// <summary>
    /// Subscription for named events in the event system.
    /// This handles the lifecycle of named event subscriptions and provides proper cleanup.
    /// </summary>
    internal class NamedEventSubscription<T> : IDisposable where T : IEvent
    {
        private readonly EventService _eventService;
        private readonly string _eventName;
        private readonly Action<object> _handler;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the NamedEventSubscription.
        /// </summary>
        /// <param name="eventService">The event service that manages this subscription</param>
        /// <param name="eventName">The name of the event being subscribed to</param>
        /// <param name="handler">The handler wrapper for the event</param>
        public NamedEventSubscription(EventService eventService, string eventName, Action<object> handler)
        {
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _eventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// Disposes of the subscription, removing it from the event service.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _eventService.RemoveNamedHandler<T>(_eventName, _handler);
                _disposed = true;
            }
        }
    }
}
