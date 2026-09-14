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
        public int FarePaid { get; set; }
        public int TaxPaid { get; set; }
        public DateTime DepartureUtc { get; set; }
        public DateTime ArrivalUtc { get; set; }
    }
}
