using SWLOR.Game.Server.Enumeration;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// A flight in the air (or being built): its route and times, and the live shuttle interior, status console and
    /// boarding point once the instance exists. Was the private class <c>ActiveFlight</c> of <see cref="ShuttleService"/>
    ///.
    /// </summary>
    public class ShuttleActiveFlight
    {
        public string FlightId { get; set; } = string.Empty;
        public PlanetType Origin { get; set; }
        public PlanetType Destination { get; set; }
        public DateTime DepartureUtc { get; set; }
        public DateTime ArrivalUtc { get; set; }
        public uint Area { get; set; } = OBJECT_INVALID;
        public uint Console { get; set; } = OBJECT_INVALID;
        public Location EntranceLocation { get; set; }
        public DateTime LastBroadcastUtc { get; set; }
    }
}
