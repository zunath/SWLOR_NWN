namespace SWLOR.Shared.Events.EventAggregator
{
    /// <summary>
    /// Base interface for all events in the SWLOR event system.
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// The timestamp when the event occurred.
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// The unique identifier for this event instance.
        /// </summary>
        Guid EventId { get; }
    }
}
