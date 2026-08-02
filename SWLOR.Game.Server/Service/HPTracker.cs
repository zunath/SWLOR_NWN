using System.Collections.Generic;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// An in-memory, temporary, *narrative* hit-point tracker. Tracked creatures (PCs or NPCs) appear in
    /// nearby players' "HP Tracker" NUI windows (opened with /hptracker). It never applies damage or heal
    /// effects and never touches a creature's real combat HP, and it is never persisted: trackers are
    /// cleared on removal, on the tracked creature's death or destruction / a tracked player's logout, and
    /// on server restart.
    ///
    /// This class is pure state (so it is easy to unit test). Window refresh and object lifecycle are
    /// coordinated by <c>HPTrackerWindow</c>; all presentation lives in <c>HPTrackerViewModel</c>.
    /// </summary>
    public static class HPTracker
    {
        private class TrackerState
        {
            public int Current;
            public int Max;
        }

        private static readonly Dictionary<uint, TrackerState> _trackers = new();

        /// <summary>Creates or replaces a tracker (single-value callers pass current == max = full).</summary>
        public static void Set(uint creature, int current, int max)
        {
            if (max < 1) max = 1;
            if (current < 0) current = 0;
            if (current > max) current = max;

            _trackers[creature] = new TrackerState { Current = current, Max = max };
        }

        /// <summary>Adjusts current HP by delta, clamped to [0, Max]. No-op if the creature has no tracker.</summary>
        public static void Adjust(uint creature, int delta)
        {
            if (!_trackers.TryGetValue(creature, out var state))
                return;

            // Widen to long so a near-int.MaxValue Current + delta can't overflow before the clamp.
            var next = (long)state.Current + delta;
            if (next < 0) next = 0;
            if (next > state.Max) next = state.Max;
            state.Current = (int)next;
        }

        /// <summary>Removes a tracker. Returns true if one was present.</summary>
        public static bool Remove(uint creature) => _trackers.Remove(creature);

        public static bool Has(uint creature) => _trackers.ContainsKey(creature);

        /// <summary>Returns the current/max pair. Only call after Has() is true.</summary>
        public static (int Current, int Max) Get(uint creature)
        {
            var state = _trackers[creature];
            return (state.Current, state.Max);
        }

        /// <summary>Tracked creatures in the same area as the viewer (the "in range" rule for the window).</summary>
        public static List<uint> GetTrackedInArea(uint viewer)
        {
            var area = GetArea(viewer);

            // A viewer with no valid area (dead in Limbo, mid-transition) would otherwise match every tracked
            // creature that is likewise arealess, exposing unrelated trackers. Show nothing until in an area.
            if (!GetIsObjectValid(area))
                return new List<uint>();

            // Cache each name once up front: GetName crosses the C#/C++ interop boundary, and the comparator
            // below would otherwise call it O(N log N) times on every window rebuild.
            var inArea = new List<(uint Id, string Name)>();

            foreach (var creature in _trackers.Keys)
            {
                if (GetIsObjectValid(creature) && GetArea(creature) == area)
                    inArea.Add((creature, GetName(creature)));
            }

            // Stable, deterministic order (name, then object id). Dictionary key order is not stable, and
            // the window rebuilds on every refresh — without this the rows would reorder on their own,
            // which flickers and, worse, makes a per-row button act on whoever slid into that row index.
            inArea.Sort((a, b) =>
            {
                var byName = string.CompareOrdinal(a.Name, b.Name);
                return byName != 0 ? byName : a.Id.CompareTo(b.Id);
            });

            var result = new List<uint>(inArea.Count);
            foreach (var item in inArea)
                result.Add(item.Id);

            return result;
        }
    }
}
