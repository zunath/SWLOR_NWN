using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// Works out how the performers of one area relate to each other: bands and their song, partners and Harmony.
    /// Pure; the snapshots carry everything it needs.
    /// </summary>
    public static class PerformanceShow
    {
        public static void Prepare(IReadOnlyList<PerformerSnapshot> performers)
        {
            for (var index = 0; index < performers.Count; index++)
            {
                var performer = performers[index];
                performer.Partner = null;
                performer.HarmonyActive = false;
                performer.BandTrackId = performer.Kind == PerformanceKind.Music ? performer.SongTrackId : null;
            }

            var bandRoots = BuildBands(performers);
            AssignBandSongs(performers, bandRoots);
            AssignPartners(performers, bandRoots);
            AssignHarmony(performers);
        }

        /// <summary>
        /// Musicians in each other's range form a band, and so do musicians linked through a third. Returns each performer's
        /// band as the index of the band's first member in the list; dancers are their own root.
        /// </summary>
        private static int[] BuildBands(IReadOnlyList<PerformerSnapshot> performers)
        {
            var roots = new int[performers.Count];
            for (var index = 0; index < roots.Length; index++)
                roots[index] = index;

            for (var first = 0; first < performers.Count; first++)
            {
                if (performers[first].Kind != PerformanceKind.Music)
                    continue;

                for (var second = first + 1; second < performers.Count; second++)
                {
                    if (performers[second].Kind != PerformanceKind.Music ||
                        !PerformanceAudienceRules.AreInEachOthersRange(performers[first], performers[second]))
                        continue;

                    var firstRoot = Find(roots, first);
                    var secondRoot = Find(roots, second);
                    if (firstRoot == secondRoot)
                        continue;

                    // Keep the lower index as the root so the result does not depend on the pairing order.
                    roots[Math.Max(firstRoot, secondRoot)] = Math.Min(firstRoot, secondRoot);
                }
            }

            for (var index = 0; index < roots.Length; index++)
                roots[index] = Find(roots, index);

            return roots;
        }

        private static int Find(int[] roots, int index)
        {
            while (roots[index] != index)
                index = roots[index];

            return index;
        }

        /// <summary>The band leader is whoever started playing first, and their song is the one the band's listeners hear.</summary>
        private static void AssignBandSongs(IReadOnlyList<PerformerSnapshot> performers, int[] bandRoots)
        {
            for (var index = 0; index < performers.Count; index++)
            {
                var member = performers[index];
                if (member.Kind != PerformanceKind.Music)
                    continue;

                var leader = member;
                for (var other = 0; other < performers.Count; other++)
                {
                    var candidate = performers[other];
                    if (candidate.Kind != PerformanceKind.Music || bandRoots[other] != bandRoots[index])
                        continue;

                    if (candidate.StartedUtc < leader.StartedUtc ||
                        candidate.StartedUtc == leader.StartedUtc && candidate.Performer < leader.Performer)
                    {
                        leader = candidate;
                    }
                }

                member.BandTrackId = leader.SongTrackId;
            }
        }

        /// <summary>A dancer's partner is the strongest other dancer in mutual range; a musician's is the strongest other member of their band.</summary>
        private static void AssignPartners(IReadOnlyList<PerformerSnapshot> performers, int[] bandRoots)
        {
            for (var index = 0; index < performers.Count; index++)
            {
                var performer = performers[index];
                PerformerSnapshot best = null;
                for (var other = 0; other < performers.Count; other++)
                {
                    var candidate = performers[other];
                    if (other == index || candidate.Kind != performer.Kind)
                        continue;

                    var isPartner = performer.Kind == PerformanceKind.Dance
                        ? PerformanceAudienceRules.AreInEachOthersRange(performer, candidate)
                        : bandRoots[other] == bandRoots[index];
                    if (isPartner && (best == null || PerformanceFollow.IsStronger(candidate, best)))
                        best = candidate;
                }

                performer.Partner = best;
            }
        }

        /// <summary>Harmony applies to a musician while any dancer performs within the musician's own range.</summary>
        private static void AssignHarmony(IReadOnlyList<PerformerSnapshot> performers)
        {
            for (var index = 0; index < performers.Count; index++)
            {
                var musician = performers[index];
                if (musician.Kind != PerformanceKind.Music)
                    continue;

                for (var other = 0; other < performers.Count; other++)
                {
                    var dancer = performers[other];
                    if (dancer.Kind != PerformanceKind.Dance ||
                        !PerformanceAudienceRules.IsWithinRange(musician.Position, dancer.Position, musician.RangeMeters))
                        continue;

                    musician.HarmonyActive = true;
                    break;
                }
            }
        }
    }
}
