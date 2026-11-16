namespace SWLOR.Game.Server.Service.InitiativeTrackerService
{
    /// <summary>
    /// One line of an initiative tracker: a member's own entry, or a named entry a manager added for an NPC.
    /// </summary>
    public class InitiativeTrackerEntry
    {
        public int Id { get; init; }

        /// <summary>
        /// The member this entry belongs to, or OBJECT_INVALID for a named entry.
        /// </summary>
        public uint Owner { get; init; }

        /// <summary>
        /// The text of a named entry. Member entries resolve their name for each viewer instead.
        /// </summary>
        public string Name { get; init; }

        public int Value { get; set; }

        /// <summary>
        /// The order the entry was first added in, which breaks ties between equal values.
        /// </summary>
        public int AddedOrder { get; init; }

        public bool IsNamed => Owner == OBJECT_INVALID;
    }
}
