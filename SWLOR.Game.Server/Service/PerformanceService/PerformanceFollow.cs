using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>Which performer of a kind a watcher follows. Pure, so it is tested without the engine.</summary>
    public static class PerformanceFollow
    {
        /// <summary>
        /// The watcher's stored choice while it is still a candidate; otherwise the performer with the highest SOC-scaled XP
        /// bonus. Ties go to the performer already followed (the longest watched), then the one who started first.
        /// </summary>
        /// <returns>The followed performer, or null when no candidate of that kind covers the watcher.</returns>
        public static PerformerSnapshot Choose(
            IReadOnlyList<PerformerSnapshot> candidates,
            PerformanceKind kind,
            uint chosenPerformer,
            uint previouslyFollowed)
        {
            PerformerSnapshot best = null;
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (candidate.Kind != kind)
                    continue;

                if (chosenPerformer != 0 && candidate.Performer == chosenPerformer)
                    return candidate;

                if (best == null || IsPreferred(candidate, best, previouslyFollowed))
                    best = candidate;
            }

            return best;
        }

        /// <summary>Picks the dancer and musician the watcher follows and records them on their state, which a Flourish reads.</summary>
        public static void Apply(PerformanceWatcherEntry entry, PerformanceAudienceState state)
        {
            entry.Dancer = Choose(entry.Candidates, PerformanceKind.Dance, state.DancerChoice, state.FollowedDancer);
            entry.Musician = Choose(entry.Candidates, PerformanceKind.Music, state.MusicianChoice, state.FollowedMusician);
            state.FollowedDancer = entry.Dancer?.Performer ?? 0;
            state.FollowedMusician = entry.Musician?.Performer ?? 0;
        }

        /// <summary>Whether a is a stronger default follow than b: the same ordering partners are picked by, plus the followed tiebreak.</summary>
        private static bool IsPreferred(PerformerSnapshot a, PerformerSnapshot b, uint previouslyFollowed)
        {
            if (a.XpScaled != b.XpScaled)
                return a.XpScaled > b.XpScaled;

            var aFollowed = previouslyFollowed != 0 && a.Performer == previouslyFollowed;
            var bFollowed = previouslyFollowed != 0 && b.Performer == previouslyFollowed;
            if (aFollowed != bFollowed)
                return aFollowed;

            return IsStronger(a, b);
        }

        /// <summary>Highest XP bonus first, then the performer who started earlier, then the lower id so the order never flips between ticks.</summary>
        public static bool IsStronger(PerformerSnapshot a, PerformerSnapshot b)
        {
            if (a.XpScaled != b.XpScaled)
                return a.XpScaled > b.XpScaled;

            if (a.StartedUtc != b.StartedUtc)
                return a.StartedUtc < b.StartedUtc;

            return a.Performer < b.Performer;
        }
    }
}
