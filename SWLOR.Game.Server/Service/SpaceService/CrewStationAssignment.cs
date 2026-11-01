using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A player at a crew station or a viewport aboard a ship in flight (plan section 10). Their character is kept
    /// hidden beside the ship and a copy of them stands at the console. Held in memory only; the player's
    /// <c>ActiveCrewShipId</c> marks it in the database so a disconnect is cleaned up at the next login.
    /// </summary>
    internal sealed class CrewStationAssignment
    {
        public string ShipId { get; init; }
        public string PropertyId { get; init; }

        /// <summary>The pilot flying the ship: the ship itself while it is in space.</summary>
        public uint Pilot { get; init; }

        /// <summary>The seat worked, or Invalid for a passenger watching from a viewport.</summary>
        public ShipCrewSeat Seat { get; init; }

        public bool IsViewer => Seat == ShipCrewSeat.Invalid;

        /// <summary>Where the player stood at the console. They return here when they leave.</summary>
        public Location ConsoleLocation { get; init; }

        /// <summary>The copy of the player that stands at the console.</summary>
        public uint Copy { get; set; } = OBJECT_INVALID;

        /// <summary>The crew member's own global recast for one-press station modules (plan section 9).</summary>
        public DateTime GlobalRecast { get; set; }
    }
}
