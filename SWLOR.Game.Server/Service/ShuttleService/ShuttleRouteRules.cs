using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.StationService;

namespace SWLOR.Game.Server.Service.ShuttleService
{
    /// <summary>
    /// Shuttle routes with player stations (plan section 25) and Home Port flights to city Starports (Player City Revamp). A
    /// station's Starport flies only to its home port (its orbit's NPC dock, or the nearest open port in Wild Space), and that
    /// port's flights terminals list the stations it serves. An orbit station is an orbital hop away; a Wild Space station is a
    /// full flight. A Home Port flight is a planet-to-planet flight that ends at a city's own Starport, or at a station from a
    /// terminal on another planet (the interplanetary flight to its home port, then its own shuttle). Planet routes keep
    /// <see cref="GalaxyMap"/>'s fares and times.
    /// </summary>
    public static class ShuttleRouteRules
    {
        /// <summary>
        /// A planet-to-planet route between different planets, a station and its home port, or a Home Port flight. Whether a
        /// character may book a Home Port flight is <see cref="HomePortRules"/>' question, not this one's.
        /// </summary>
        public static bool IsValid(ShuttleRoute route)
        {
            var origin = route.Origin;
            var destination = route.Destination;
            if (origin.Planet == PlanetType.Invalid || destination.Planet == PlanetType.Invalid)
                return false;

            // A city's Starport is only ever where a Home Port flight lands, from a planet's terminal on another planet.
            if (origin.IsCityStarport)
                return false;

            if (destination.IsCityStarport)
                return !origin.IsStation && origin.Planet != destination.Planet;

            if (origin.IsStation && destination.IsStation)
                return false;

            if (route.IsStationRoute)
            {
                var station = route.StationStop;
                var port = origin.IsStation ? destination : origin;
                return port.Planet == station.Planet || route.IsHomePort;
            }

            return origin.Planet != destination.Planet;
        }

        public static int GetFare(ShuttleRoute route)
        {
            // The terminal's own fare to that planet, whichever city's Starport the flight ends at.
            if (route.IsHomePort)
            {
                var fare = GalaxyMap.GetFare(route.Origin.Planet, route.Destination.Planet);
                return route.Destination.IsStation ? fare + GetStationHopFare(route.Destination) : fare;
            }

            if (!route.IsStationRoute)
                return GalaxyMap.GetFare(route.Origin.Planet, route.Destination.Planet);

            return GetStationHopFare(route.StationStop);
        }

        /// <summary>The flight time of a route with no Starport level: a Home Port flight loses nothing.</summary>
        public static int GetTransitSeconds(ShuttleRoute route)
        {
            return GetTransitSeconds(route, 0);
        }

        /// <summary>
        /// The flight time of a route. A Home Port flight loses 30 seconds per effective Starport level of the city it ends at, once, on
        /// its interplanetary leg; a station's orbital or Wild Space shuttle after that leg is unchanged. Every other route ignores
        /// the level.
        /// </summary>
        /// <param name="route">The route.</param>
        /// <param name="homeStarportLevel">The effective Starport level of the Home Port city, or 0.</param>
        public static int GetTransitSeconds(ShuttleRoute route, int homeStarportLevel)
        {
            if (route.IsHomePort)
            {
                var flight = HomePortRules.GetInterplanetarySeconds(route.Origin.Planet, route.Destination.Planet, homeStarportLevel);
                return route.Destination.IsStation ? flight + GetStationHopSeconds(route.Destination) : flight;
            }

            if (!route.IsStationRoute)
                return GalaxyMap.GetTransitSeconds(route.Origin.Planet, route.Destination.Planet);

            return GetStationHopSeconds(route.StationStop);
        }

        public static int GetPeriodSeconds(ShuttleRoute route)
        {
            // A Home Port flight leaves on the schedule of its interplanetary leg.
            if (route.IsHomePort || !route.IsStationRoute)
                return ShuttleSchedule.GetPeriodSeconds(route.Origin.Planet, route.Destination.Planet);

            return route.StationStop.IsWildSpace ? StationTuning.WildSpacePeriodSeconds : StationTuning.OrbitHopPeriodSeconds;
        }

        private static int GetStationHopFare(ShuttleStop station)
        {
            return station.IsWildSpace ? StationTuning.WildSpaceFare : StationTuning.OrbitHopFare;
        }

        private static int GetStationHopSeconds(ShuttleStop station)
        {
            return station.IsWildSpace ? StationTuning.WildSpaceSeconds : StationTuning.OrbitHopSeconds;
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

        /// <summary>
        /// The route a ride record flies, rebuilt from the planets and ids it saved, so a ticket or a flight survives a restart.
        /// A station that no longer exists keeps its home port and id; a city Starport needs no lookup, since its stop is only the
        /// planet and the city's id.
        /// </summary>
        /// <param name="origin">The origin planet.</param>
        /// <param name="originStationId">The station the ride leaves from, or empty.</param>
        /// <param name="originCityId">The city Starport the ride leaves from, or empty.</param>
        /// <param name="destination">The destination planet.</param>
        /// <param name="destinationStationId">The station the ride flies to, or empty.</param>
        /// <param name="destinationCityId">The city Starport the ride flies to, or empty.</param>
        /// <param name="getStation">Looks a station's stop up, returning a stop that is not a station for a station that is gone.</param>
        public static ShuttleRoute BuildRideRoute(
            PlanetType origin,
            string originStationId,
            string originCityId,
            PlanetType destination,
            string destinationStationId,
            string destinationCityId,
            Func<string, ShuttleStop> getStation)
        {
            return new ShuttleRoute(
                BuildRideStop(origin, originStationId, originCityId, getStation),
                BuildRideStop(destination, destinationStationId, destinationCityId, getStation));
        }

        private static ShuttleStop BuildRideStop(PlanetType planet, string stationId, string cityId, Func<string, ShuttleStop> getStation)
        {
            if (!string.IsNullOrWhiteSpace(stationId))
            {
                var stop = getStation(stationId);
                return stop.IsStation ? stop : ShuttleStop.ForStation(stationId, planet, false);
            }

            return string.IsNullOrWhiteSpace(cityId)
                ? ShuttleStop.ForPlanet(planet)
                : ShuttleStop.ForCityStarport(planet, cityId);
        }
    }
}
