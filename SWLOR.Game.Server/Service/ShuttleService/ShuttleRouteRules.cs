using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.StationService;

namespace SWLOR.Game.Server.Service.ShuttleService
{
    /// <summary>
    /// Shuttle routes with player stations (plan section 25). A station's Starport flies only to its home port (its orbit's
    /// NPC dock, or the nearest open port in Wild Space), and that port's flights terminals list the stations it serves.
    /// An orbit station is an orbital hop away; a Wild Space station is a full flight. Planet routes keep
    /// <see cref="GalaxyMap"/>'s fares and times.
    /// </summary>
    public static class ShuttleRouteRules
    {
        /// <summary>A planet-to-planet route between different planets, or a station and its home port.</summary>
        public static bool IsValid(ShuttleRoute route)
        {
            var origin = route.Origin;
            var destination = route.Destination;
            if (origin.Planet == PlanetType.Invalid || destination.Planet == PlanetType.Invalid)
                return false;

            if (origin.IsStation && destination.IsStation)
                return false;

            if (route.IsStationRoute)
            {
                var station = route.StationStop;
                var port = origin.IsStation ? destination : origin;
                return port.Planet == station.Planet;
            }

            return origin.Planet != destination.Planet;
        }

        public static int GetFare(ShuttleRoute route)
        {
            if (!route.IsStationRoute)
                return GalaxyMap.GetFare(route.Origin.Planet, route.Destination.Planet);

            return route.StationStop.IsWildSpace ? StationTuning.WildSpaceFare : StationTuning.OrbitHopFare;
        }

        public static int GetTransitSeconds(ShuttleRoute route)
        {
            if (!route.IsStationRoute)
                return GalaxyMap.GetTransitSeconds(route.Origin.Planet, route.Destination.Planet);

            return route.StationStop.IsWildSpace ? StationTuning.WildSpaceSeconds : StationTuning.OrbitHopSeconds;
        }

        public static int GetPeriodSeconds(ShuttleRoute route)
        {
            if (!route.IsStationRoute)
                return ShuttleSchedule.GetPeriodSeconds(route.Origin.Planet, route.Destination.Planet);

            return route.StationStop.IsWildSpace ? StationTuning.WildSpacePeriodSeconds : StationTuning.OrbitHopPeriodSeconds;
        }

        /// <summary>
        /// The stations a terminal at <paramref name="origin"/> can fly to: from a station, none (its only destination is its
        /// home port); from a planet, every station whose home port it is.
        /// </summary>
        public static IEnumerable<ShuttleStop> GetStationDestinations(ShuttleStop origin, IEnumerable<ShuttleStop> stations)
        {
            if (origin.IsStation)
                return Enumerable.Empty<ShuttleStop>();

            return stations.Where(x => x.IsStation && x.Planet == origin.Planet);
        }
    }
}
