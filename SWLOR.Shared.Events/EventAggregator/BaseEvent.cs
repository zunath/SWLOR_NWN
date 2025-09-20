using System;

namespace SWLOR.Shared.Events.EventAggregator
{
    /// <summary>
    /// Base implementation of IEvent with common properties.
    /// </summary>
    public abstract class BaseEvent : IEvent
    {
        public DateTime Timestamp { get; }
        public Guid EventId { get; }

        protected BaseEvent()
        {
            Timestamp = DateTime.UtcNow;
            EventId = Guid.NewGuid();
        }
    }
}
