using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;

namespace SWLOR.Game.Server.Service.ShuttleService
{
    /// <summary>
    /// Pure math helpers for computing shuttle travel distance, transit time, and fare between
    /// planets based on their galaxy map coordinates. Contains no NWScript calls.
    /// </summary>
    public static class GalaxyMap
    {
        // An orbital station sits just off its parent planet, so travel between the two is a quick,
        // cheap shuttle hop rather than a full interplanetary flight.
        private const int OrbitalHopSeconds = 60;
        private const int OrbitalHopFare = 25;
        private const int MinimumTransitSeconds = 300;
        private const int MaximumTransitSeconds = 600;
        private const int TransitRoundingSeconds = 15;

        /// <summary>
        /// Determines whether the route between two planets is a short orbital hop between a station
        /// and its parent planet, rather than a full interplanetary flight.
        /// </summary>
        public static bool IsOrbitalHop(PlanetType origin, PlanetType destination)
        {
            return IsStationPair(origin, destination, PlanetType.Viscara, PlanetType.CZ220) ||
                   IsStationPair(origin, destination, PlanetType.SmugglersMoon, PlanetType.SmugglersMoonStation);
        }

        private static bool IsStationPair(PlanetType origin, PlanetType destination, PlanetType planet, PlanetType station)
        {
            return (origin == planet && destination == station) ||
                   (origin == station && destination == planet);
        }

        /// <summary>
        /// Calculates the Euclidean distance between the galaxy map coordinates of two planets.
        /// </summary>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <returns>The distance between the two planets on the galaxy map.</returns>
        public static float GetDistance(PlanetType origin, PlanetType destination)
        {
            var originDetail = origin.GetAttribute<PlanetType, PlanetAttribute>();
            var destinationDetail = destination.GetAttribute<PlanetType, PlanetAttribute>();

            var dx = destinationDetail.GalaxyX - originDetail.GalaxyX;
            var dy = destinationDetail.GalaxyY - originDetail.GalaxyY;

            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Calculates the shuttle transit time, in seconds, between two planets. The raw value is
        /// rounded to the nearest 15 seconds and clamped between 300 (5 minutes) and 600
        /// (10 minutes) seconds.
        /// </summary>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <returns>The transit time, in seconds.</returns>
        public static int GetTransitSeconds(PlanetType origin, PlanetType destination)
        {
            if (IsOrbitalHop(origin, destination))
                return OrbitalHopSeconds;

            var distance = GetDistance(origin, destination);
            var raw = MinimumTransitSeconds +
                      (MaximumTransitSeconds - MinimumTransitSeconds) * (distance - 5.0) / 77.2;
            var rounded = (int)(Math.Round(raw / TransitRoundingSeconds, MidpointRounding.AwayFromZero) *
                                TransitRoundingSeconds);

            if (rounded < MinimumTransitSeconds) rounded = MinimumTransitSeconds;
            if (rounded > MaximumTransitSeconds) rounded = MaximumTransitSeconds;

            return rounded;
        }

        /// <summary>
        /// Calculates the shuttle fare, in credits, between two planets. The raw value is rounded
        /// to the nearest 5 credits with a minimum fare of 100 credits.
        /// </summary>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <returns>The fare, in credits.</returns>
        public static int GetFare(PlanetType origin, PlanetType destination)
        {
            if (IsOrbitalHop(origin, destination))
                return OrbitalHopFare;

            var distance = GetDistance(origin, destination);
            var raw = 12.0 * distance;
            var rounded = (int)(Math.Round(raw / 5.0, MidpointRounding.AwayFromZero) * 5.0);

            if (rounded < 100) rounded = 100;

            return rounded;
        }
    }
}
