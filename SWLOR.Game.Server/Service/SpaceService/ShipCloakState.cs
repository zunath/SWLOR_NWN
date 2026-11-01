using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A ship's running cloak. Lives in memory and ends with the flight.</summary>
    public class ShipCloakState
    {
        public DateTime StartedAt { get; init; }
        public ShipCloakProfile Profile { get; init; }

        /// <summary>The hotbar slot of the Cloaking Device that runs it.</summary>
        public int Slot { get; init; }

        /// <summary>The players who see the ship through the cloak right now (reveal range, party, Sensor Sweep).</summary>
        public HashSet<uint> VisibleTo { get; } = new();

        /// <summary>Observers a Sensor Sweep revealed the ship to, and until when.</summary>
        public Dictionary<uint, DateTime> PaintedUntil { get; } = new();
    }
}
