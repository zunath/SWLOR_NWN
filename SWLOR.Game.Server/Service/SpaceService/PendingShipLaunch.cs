using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Reserves a ship and preserves the interior return location across native area loading.</summary>
    internal sealed class PendingShipLaunch
    {
        public string PlayerId { get; init; }
        public string ShipId { get; init; }
        public uint SpaceArea { get; init; }
        public Location PilotLocation { get; init; }

        /// <summary>The area the pilot leaves from: the ship's interior, or the host's when the ship undocks from it.</summary>
        public uint DepartureArea { get; init; }

        /// <summary>The host ship the launch undocks from, or empty. The ship stays docked until the pilot arrives.</summary>
        public string UndockFromShipId { get; init; } = string.Empty;

        public DateTime ExpiresAt { get; init; }
        public bool HasDeparted { get; set; }
    }
}
