using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A ship docked aboard a host, as <see cref="ShipDockingRules.AssignReturningPlayers"/> reads it.</summary>
    public sealed class ShipDockVisitor
    {
        public string ShipId { get; init; }
        public string OwnerPlayerId { get; init; }

        /// <summary>The players who came aboard with the ship, the docking pilot first.</summary>
        public IReadOnlyList<string> CrewPlayerIds { get; init; } = new List<string>();
    }
}
