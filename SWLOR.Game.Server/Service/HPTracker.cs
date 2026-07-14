using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// An in-memory, temporary, *narrative* hit-point tracker. Tracked creatures (PCs or NPCs) appear in
    /// nearby players' "HP Tracker" NUI windows (opened with /hptracker). It never applies damage or heal
    /// effects and never touches a creature's real combat HP, and it is never persisted: trackers are
    /// cleared on removal, on the tracked creature's destruction / a tracked player's logout, and on
    /// server restart.
    ///
    /// This class is pure state + pure formatting helpers (so it is easy to unit test). Window refresh
    /// and object lifecycle are coordinated by <c>HpTrackerWindow</c>.
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

            state.Current += delta;
            if (state.Current < 0) state.Current = 0;
            if (state.Current > state.Max) state.Current = state.Max;
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
            var result = new List<uint>();

            foreach (var creature in _trackers.Keys)
            {
                if (GetIsObjectValid(creature) && GetArea(creature) == area)
                    result.Add(creature);
            }

            // Stable, deterministic order (name, then object id). Dictionary key order is not stable, and
            // the window rebuilds on every heartbeat — without this the rows would reorder on their own,
            // which flickers and, worse, makes a per-row button act on whoever slid into that row index.
            result.Sort((a, b) =>
            {
                var byName = string.CompareOrdinal(GetName(a), GetName(b));
                return byName != 0 ? byName : a.CompareTo(b);
            });

            return result;
        }

        /// <summary>Bar fill 0..1 for the given current/max (pure).</summary>
        public static float GetProgress(int current, int max)
        {
            if (max <= 0) return 0f;
            var ratio = (float)current / max;
            if (ratio < 0f) return 0f;
            if (ratio > 1f) return 1f;
            return ratio;
        }

        /// <summary>Bar color, green (full) -> yellow (half) -> red (empty) by ratio (pure).</summary>
        public static GuiColor GetBarColor(int current, int max)
        {
            var ratio = GetProgress(current, max);

            byte r, g;
            if (ratio >= 0.5f)
            {
                r = (byte)(255 * (1f - ratio) * 2f); // 0 at full, 255 at half
                g = 255;
            }
            else
            {
                r = 255;
                g = (byte)(255 * ratio * 2f);        // 255 at half, 0 at empty
            }

            return new GuiColor(r, g, 0);
        }
    }
}
