using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.SpaceService
{
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
