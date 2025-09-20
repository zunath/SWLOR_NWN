using System;

namespace SWLOR.Shared.Events.Attributes
{
    /// <summary>
    /// Attribute to mark methods as event handlers for the new EventAggregator system.
    /// This provides a migration path from ScriptHandler attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class EventHandlerAttribute : Attribute
    {
        /// <summary>
        /// The event type this method handles.
        /// </summary>
        public Type EventType { get; }

        public EventHandlerAttribute(Type eventType)
        {
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        }
    }
}
