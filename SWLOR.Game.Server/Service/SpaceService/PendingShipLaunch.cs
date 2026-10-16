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
        public DateTime ExpiresAt { get; init; }
        public bool HasDeparted { get; set; }
    }
}
