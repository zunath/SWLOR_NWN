namespace SWLOR.Shared.Abstractions.Contracts
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

        /// <summary>
        /// The script name associated with this event type.
        /// This is used by the ScriptRegistry to map events to their corresponding scripts.
        /// </summary>
        string ScriptName { get; }
    }
}
