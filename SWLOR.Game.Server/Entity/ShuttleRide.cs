using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.ShuttleService;

namespace SWLOR.Game.Server.Entity
{
    public class ShuttleRide: EntityBase
    {
        public ShuttleRide()
        {
            PlayerId = string.Empty;
            FlightId = string.Empty;
        }

        public ShuttleRide(string playerId)
        {
            PlayerId = playerId;
            FlightId = string.Empty;
        }

        [Indexed]
        public string PlayerId { get; set; }

        [Indexed]
        public ShuttleRideStatus Status { get; set; }

        [Indexed]
        public string FlightId { get; set; }

        public PlanetType Origin { get; set; }
        public PlanetType Destination { get; set; }

        /// <summary>The player station the ride leaves from, or empty when it leaves a planet. Older records load empty.</summary>
        public string OriginStationId { get; set; } = string.Empty;

        /// <summary>The player station the ride flies to, or empty when it flies to a planet. Older records load empty.</summary>
        public string DestinationStationId { get; set; } = string.Empty;
        public int FarePaid { get; set; }
        public int TaxPaid { get; set; }
        public DateTime DepartureUtc { get; set; }
        public DateTime ArrivalUtc { get; set; }
    }
}
