using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// A temporary, in-memory, *narrative* hit-point tracker rendered as a bar in a
    /// creature's name-plate (above its model). This is purely a display / bookkeeping
    /// tool: it never applies damage or heal effects and never touches a creature's real
    /// combat hit points. Trackers are transient and are cleared on object destruction
    /// and on the tracked player's logout (they do not survive a server restart).
    ///
    /// Display is driven by <see cref="PlayerPlugin.SetCreatureNameOverride"/>, which is a
    /// per-observer, client-side override. Because it is client-side, server-side
    /// GetName(creature) still returns the true name, so the label is rebuilt on demand and
    /// only the {current, max} pair is persisted here.
    /// </summary>
    public static class HPTracker
    {
        private class TrackerState
        {
            public int Current;
            public int Max;
        }

        // Creature -> tracked HP. Keyed by object id; purged on destroy / logout so a reused
        // object id can never inherit a stale tracker (mirrors the lifecycle in Enmity).
        private static readonly Dictionary<uint, TrackerState> _trackers = new();

        private const int BarSegments = 10;

        // Block characters for the bar, written as escapes to stay independent of source encoding.
        private const char FilledSegment = '█'; // full block
        private const char EmptySegment = '░';  // light shade

        /// <summary>
        /// Creates or replaces a tracker on the given creature and refreshes its name-plate.
        /// A single-value call passes current == max (starts full).
        /// </summary>
        public static void Set(uint creature, int current, int max)
        {
            if (max < 1) max = 1;
            if (current < 0) current = 0;
            if (current > max) current = max;

            _trackers[creature] = new TrackerState { Current = current, Max = max };
            Refresh(creature);
        }

        /// <summary>
        /// Adjusts the current HP of an existing tracker by delta, clamped to [0, Max].
        /// Does nothing if the creature has no tracker.
        /// </summary>
        public static void Adjust(uint creature, int delta)
        {
            if (!_trackers.TryGetValue(creature, out var state))
                return;

            state.Current += delta;
            if (state.Current < 0) state.Current = 0;
            if (state.Current > state.Max) state.Current = state.Max;

            Refresh(creature);
        }

        /// <summary>
        /// Removes a tracker and clears the name-plate override from all observers.
        /// </summary>
        public static void Remove(uint creature)
        {
            if (!_trackers.ContainsKey(creature))
                return;

            Clear(creature);
            _trackers.Remove(creature);
        }

        /// <summary>
        /// Returns whether the creature currently has a tracker.
        /// </summary>
        public static bool Has(uint creature)
        {
            return _trackers.ContainsKey(creature);
        }

        /// <summary>
        /// Returns the current/max pair of a tracked creature. Only call after Has() is true.
        /// </summary>
        public static (int Current, int Max) Get(uint creature)
        {
            var state = _trackers[creature];
            return (state.Current, state.Max);
        }

        /// <summary>
        /// Re-applies the name-plate override for a tracked creature to every observing
        /// player (including DM clients) currently in the creature's area.
        /// </summary>
        private static void Refresh(uint creature)
        {
            if (!_trackers.TryGetValue(creature, out var state))
                return;

            var label = BuildLabel(creature, state);
            var area = GetArea(creature);

            // GetFirstPC/GetNextPC includes DM clients (unlike Area.GetPlayersInArea), and DMs
            // are the primary audience for these bars.
            for (var observer = GetFirstPC(); GetIsObjectValid(observer); observer = GetNextPC())
            {
                if (GetArea(observer) != area) continue;
                PlayerPlugin.SetCreatureNameOverride(observer, creature, label);
            }
        }

        /// <summary>
        /// Clears the name-plate override for a creature from every observer in its area.
        /// </summary>
        private static void Clear(uint creature)
        {
            // Sweep ALL online players (not just the creature's current area): an observer may
            // have received the override and then moved to another area before the tracker was
            // removed. Clearing an override a client never had is harmless, and doing so here
            // prevents a stale "ghost" bar from lingering when that observer returns.
            for (var observer = GetFirstPC(); GetIsObjectValid(observer); observer = GetNextPC())
            {
                PlayerPlugin.SetCreatureNameOverride(observer, creature, string.Empty);
            }
        }

        /// <summary>
        /// Applies the override for a single tracked creature to a single observer. Used when
        /// a player enters an area so they immediately see any existing trackers there.
        /// </summary>
        private static void ApplyToObserver(uint observer, uint creature)
        {
            if (!_trackers.TryGetValue(creature, out var state))
                return;

            PlayerPlugin.SetCreatureNameOverride(observer, creature, BuildLabel(creature, state));
        }

        private static string BuildLabel(uint creature, TrackerState state)
        {
            var bar = BuildBar(state.Current, state.Max);
            return $"{GetName(creature)}\n{bar} {state.Current}/{state.Max}";
        }

        private static string BuildBar(int current, int max)
        {
            var ratio = max <= 0 ? 0f : (float)current / max;
            if (ratio < 0f) ratio = 0f;
            if (ratio > 1f) ratio = 1f;

            var filled = (int)(ratio * BarSegments + 0.5f);
            if (current > 0 && filled == 0) filled = 1; // never render an empty bar while HP > 0
            if (filled > BarSegments) filled = BarSegments;
            var empty = BarSegments - filled;

            var bar = new string(FilledSegment, filled) + new string(EmptySegment, empty);

            // Gradient: green (full) -> yellow (half) -> red (empty).
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

            return ColorToken.Custom(bar, r, g, 0);
        }

        // ---- Lifecycle ----

        /// <summary>
        /// When a player/DM enters an area, show them the trackers of creatures already there.
        /// When a *tracked creature* enters a new area, refresh its bar for that area's observers.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void ApplyTrackersOnAreaEnter()
        {
            var entering = GetEnteringObject();
            var area = OBJECT_SELF;

            // A tracked creature moved into this area: refresh it for everyone here.
            if (_trackers.ContainsKey(entering))
                Refresh(entering);

            // A player/DM arrived: push every tracker in this area to them.
            if (GetIsPC(entering) || GetIsDM(entering))
            {
                foreach (var creature in _trackers.Keys)
                {
                    if (GetArea(creature) == area)
                        ApplyToObserver(entering, creature);
                }
            }
        }

        /// <summary>
        /// When a tracked creature is destroyed, clear its name-plate override and drop its
        /// tracker. Clearing first matters because NWN reuses object ids — a lingering override
        /// could otherwise "ghost" onto a new object that reuses the same id. Uses Remove(),
        /// which no-ops for untracked objects (the common case for this frequent event).
        /// </summary>
        [NWNEventHandler(ScriptName.OnObjectDestroyed)]
        public static void RemoveTrackerOnDestroyed()
        {
            Remove(OBJECT_SELF);
        }

        /// <summary>
        /// When a tracked player logs out, clear their name-plate override and drop their tracker.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void RemoveTrackerOnExit()
        {
            Remove(GetExitingObject());
        }
    }
}
