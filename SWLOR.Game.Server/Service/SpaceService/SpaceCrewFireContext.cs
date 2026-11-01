using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A crew member working a station module: the ship fires it, and the crew member's attributes, perks and
    /// effectiveness set its strength (plan section 10, "While crewing").
    /// </summary>
    public class SpaceCrewFireContext
    {
        /// <summary>The ship that fires: its pilot's creature.</summary>
        public uint Ship { get; init; }

        /// <summary>
        /// The crew member whose attributes and stats the shot or repair reads, or OBJECT_INVALID for a droid,
        /// which has no creature at its post.
        /// </summary>
        public uint CrewMember { get; init; }

        /// <summary>The droid at work, or null for a player crew member.</summary>
        public ShipDroidCrew Droid { get; init; }

        public ShipCrewSeat Seat { get; init; }

        /// <summary>The station's effectiveness: repair amounts and weapon DMG are multiplied by it.</summary>
        public double Effectiveness { get; init; } = 1.0;

        /// <summary>The skill the crew member earns combat points in: the seat's skill.</summary>
        public SkillType Skill { get; init; } = SkillType.Piloting;

        /// <summary>
        /// Whether the crew member is using one of their own Piloting abilities rather than firing a module: the ship
        /// activates it, and the crew member pays its STM and recast and lends it their perks, Social and Leadership
        /// (<see cref="SpaceCrew.GetAbilityUser"/>).
        /// </summary>
        public bool IsAbility { get; init; }
    }
}
