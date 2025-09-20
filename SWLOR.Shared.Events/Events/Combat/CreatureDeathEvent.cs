using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Combat
{
    /// <summary>
    /// Event fired when a creature dies.
    /// </summary>
    public class CreatureDeathEvent : BaseEvent
    {
        /// <summary>
        /// The creature's ID.
        /// </summary>
        public string CreatureId { get; }

        /// <summary>
        /// The creature's name.
        /// </summary>
        public string CreatureName { get; }

        /// <summary>
        /// The ID of the player who killed the creature (if any).
        /// </summary>
        public string? KillerPlayerId { get; }

        /// <summary>
        /// The area where the death occurred.
        /// </summary>
        public string AreaName { get; }

        /// <summary>
        /// The creature's level.
        /// </summary>
        public int CreatureLevel { get; }

        public CreatureDeathEvent(string creatureId, string creatureName, string? killerPlayerId, string areaName, int creatureLevel)
        {
            CreatureId = creatureId;
            CreatureName = creatureName;
            KillerPlayerId = killerPlayerId;
            AreaName = areaName;
            CreatureLevel = creatureLevel;
        }
    }
}
