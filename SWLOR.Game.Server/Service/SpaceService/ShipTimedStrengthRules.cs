using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Timed effects that come in strengths, such as a Jammer's Accuracy penalty, a Target Painter's paint or an Ion
    /// Web's slow, as pure functions. Each strength keeps its own expiry, so the strongest unexpired one applies, and a
    /// weaker one renewing never keeps a stronger one alive.
    /// </summary>
    public static class ShipTimedStrengthRules
    {
        /// <summary>
        /// Renews one strength until <paramref name="until"/>, never shortening it, and drops the strengths that have
        /// lapsed. A strength of 0 does nothing.
        /// </summary>
        public static void Refresh(Dictionary<int, DateTime> strengths, int value, DateTime until, DateTime now)
        {
            if (strengths == null)
                return;

            foreach (var lapsed in strengths.Where(x => x.Value <= now).Select(x => x.Key).ToList())
                strengths.Remove(lapsed);

            if (value != 0 && (!strengths.TryGetValue(value, out var current) || current < until))
                strengths[value] = until;
        }

        /// <summary>
        /// The strongest unexpired value: the largest, or with <paramref name="lowestWins"/> the most negative (an
        /// Accuracy penalty). 0 when none is active.
        /// </summary>
        public static int Strongest(IReadOnlyDictionary<int, DateTime> strengths, DateTime now, bool lowestWins = false)
        {
            var active = Active(strengths, now).ToList();
            if (active.Count == 0)
                return 0;

            return lowestWins ? active.Min(x => x.Key) : active.Max(x => x.Key);
        }

        /// <summary>When the strongest unexpired value lapses, or null when none is active.</summary>
        public static DateTime? StrongestUntil(IReadOnlyDictionary<int, DateTime> strengths, DateTime now, bool lowestWins = false)
        {
            var strongest = Strongest(strengths, now, lowestWins);
            return strongest == 0 ? null : strengths[strongest];
        }

        private static IEnumerable<KeyValuePair<int, DateTime>> Active(IReadOnlyDictionary<int, DateTime> strengths, DateTime now) =>
            strengths == null ? Enumerable.Empty<KeyValuePair<int, DateTime>>() : strengths.Where(x => x.Key != 0 && now < x.Value);
    }
}
