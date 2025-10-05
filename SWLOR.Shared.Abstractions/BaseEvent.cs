using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.Abstractions
{
    /// <summary>
    /// Base implementation of IEvent with common properties.
    /// </summary>
    public abstract class BaseEvent : IEvent
    {
        public DateTime Timestamp { get; }
        public Guid EventId { get; }
        public abstract string Script { get; }

        protected BaseEvent()
        {
            Timestamp = DateTime.UtcNow;
            EventId = Guid.NewGuid();
        }
    }
}
