using System.Collections.Generic;
using System.Globalization;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.ShuttleService
{
    /// <summary>
    /// Deterministic shuttle departure timetable math. Departure times for a directional route
    /// are derived from a fixed schedule anchored at DateTime.UnixEpoch (UTC) and do not depend
    /// on any mutable server state. Contains no NWScript calls.
    /// </summary>
    public static class ShuttleSchedule
    {
        private const uint FnvOffsetBasis = 2166136261;
        private const uint FnvPrime = 16777619;
        private const int OrbitalHopPeriodSeconds = 60;

        /// <summary>
        /// Computes a deterministic 32-bit FNV-1a hash of a directional route between two
        /// planets, using the ASCII bytes of the string "{originId}&gt;{destinationId}".
        /// String.GetHashCode is intentionally not used since it is randomized per process.
        /// </summary>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <returns>A 32-bit hash unique to the directional route.</returns>
        public static uint GetRouteHash(PlanetType origin, PlanetType destination)
        {
            var routeKey = $"{(int)origin}>{(int)destination}";
            var hash = FnvOffsetBasis;

            foreach (var c in routeKey)
            {
                hash ^= (byte)c;
                hash *= FnvPrime;
            }

            return hash;
        }

        /// <summary>
        /// Computes the deterministic departure period, in seconds, for a directional route.
        /// Orbital hops return a short 60-second period; all other routes range from 240 to
        /// 360 seconds in steps of 30.
        /// </summary>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <returns>The departure period, in seconds.</returns>
        public static int GetPeriodSeconds(PlanetType origin, PlanetType destination)
        {
            // Orbital hops run frequently so a short trip isn't gated by a long wait.
            if (GalaxyMap.IsOrbitalHop(origin, destination))
                return OrbitalHopPeriodSeconds;

            return 240 + 30 * (int)(GetRouteHash(origin, destination) % 5);
        }

        /// <summary>
        /// Computes the deterministic departure offset, in seconds, for a directional route.
        /// </summary>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <returns>The departure offset, in seconds, within a single period.</returns>
        public static int GetOffsetSeconds(PlanetType origin, PlanetType destination)
        {
            var period = GetPeriodSeconds(origin, destination);
            return (int)((GetRouteHash(origin, destination) / 7) % (uint)period);
        }

        /// <summary>
        /// Computes the next scheduled departure time for a directional route, strictly after
        /// utcNow and at most one period later.
        /// </summary>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <param name="utcNow">The current UTC time to schedule from.</param>
        /// <returns>The next departure time, in UTC.</returns>
        public static DateTime GetNextDepartureUtc(PlanetType origin, PlanetType destination, DateTime utcNow)
        {
            var period = GetPeriodSeconds(origin, destination);
            var offset = GetOffsetSeconds(origin, destination);
            var t = (long)Math.Floor((utcNow - DateTime.UnixEpoch).TotalSeconds);

            long next;
            if (t < offset)
            {
                next = offset;
            }
            else
            {
                next = ((t - offset) / period + 1) * period + offset;
            }

            return DateTime.SpecifyKind(DateTime.UnixEpoch.AddSeconds(next), DateTimeKind.Utc);
        }

        /// <summary>
        /// Enumerates every scheduled departure time for a directional route within the range
        /// (fromExclusive, toInclusive], in ascending order.
        /// </summary>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <param name="fromExclusive">The exclusive lower bound of the range.</param>
        /// <param name="toInclusive">The inclusive upper bound of the range.</param>
        /// <returns>An ascending sequence of departure times, in UTC.</returns>
        public static IEnumerable<DateTime> GetDeparturesBetween(PlanetType origin, PlanetType destination, DateTime fromExclusive, DateTime toInclusive)
        {
            var current = GetNextDepartureUtc(origin, destination, fromExclusive);

            while (current <= toInclusive)
            {
                yield return current;
                current = GetNextDepartureUtc(origin, destination, current);
            }
        }

        /// <summary>
        /// Builds a stable flight identifier encoding the route and departure time, in the form
        /// "{originId}&gt;{destinationId}@{departureTicks}".
        /// </summary>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <param name="departureUtc">The scheduled departure time, in UTC.</param>
        /// <returns>The flight identifier string.</returns>
        public static string BuildFlightId(PlanetType origin, PlanetType destination, DateTime departureUtc)
        {
            return $"{(int)origin}>{(int)destination}@{departureUtc.Ticks}";
        }

        /// <summary>
        /// Attempts to parse a flight identifier produced by BuildFlightId back into its origin,
        /// destination, and departure time components. Returns false on any malformed input.
        /// </summary>
        /// <param name="flightId">The flight identifier to parse.</param>
        /// <param name="origin">The parsed origin planet, or Invalid on failure.</param>
        /// <param name="destination">The parsed destination planet, or Invalid on failure.</param>
        /// <param name="departureUtc">The parsed departure time in UTC, or default on failure.</param>
        /// <returns>True if parsing succeeded; otherwise false.</returns>
        public static bool TryParseFlightId(string flightId, out PlanetType origin, out PlanetType destination, out DateTime departureUtc)
        {
            origin = PlanetType.Invalid;
            destination = PlanetType.Invalid;
            departureUtc = default;

            if (string.IsNullOrEmpty(flightId))
                return false;

            var atIndex = flightId.IndexOf('@');
            if (atIndex < 0)
                return false;

            var routePart = flightId.Substring(0, atIndex);
            var ticksPart = flightId.Substring(atIndex + 1);

            var separatorIndex = routePart.IndexOf('>');
            if (separatorIndex < 0)
                return false;

            var originPart = routePart.Substring(0, separatorIndex);
            var destinationPart = routePart.Substring(separatorIndex + 1);

            if (!int.TryParse(originPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var originValue))
                return false;

            if (!int.TryParse(destinationPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var destinationValue))
                return false;

            if (!long.TryParse(ticksPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
                return false;

            // Guard the DateTime tick range so out-of-range values fail the Try-pattern
            // rather than throwing from the DateTime constructor.
            if (ticks < 0 || ticks > DateTime.MaxValue.Ticks)
                return false;

            origin = (PlanetType)originValue;
            destination = (PlanetType)destinationValue;
            departureUtc = new DateTime(ticks, DateTimeKind.Utc);

            return true;
        }
    }
}
