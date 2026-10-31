using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Pure rules for escape pods (plan section 9, Death): who a destroyed ship ejects and where they go.
    /// </summary>
    public static class EscapePodRules
    {
        /// <summary>
        /// Everyone aboard who is ejected alive, each once: the pilot first, then the station crew and viewport
        /// watchers, then the passengers in the interior. NPCs are not part of the manifest.
        /// </summary>
        public static List<T> BuildManifest<T>(T pilot, IEnumerable<T> stationCrew, IEnumerable<T> passengers)
        {
            var manifest = new List<T>();

            void Add(T rider)
            {
                if (!manifest.Contains(rider))
                    manifest.Add(rider);
            }

            Add(pilot);
            foreach (var rider in stationCrew ?? Enumerable.Empty<T>())
                Add(rider);
            foreach (var rider in passengers ?? Enumerable.Empty<T>())
                Add(rider);

            return manifest;
        }

        /// <summary>
        /// Picks the dock the pods land at: the dock the ship is towed to when it can be reached, otherwise the
        /// last NPC dock of the ship (a player starport interior may not be loaded), otherwise nothing.
        /// </summary>
        public static EscapePodDestinationType ChooseDestination(bool dockReachable, bool safeDockReachable)
        {
            if (dockReachable)
                return EscapePodDestinationType.Dock;

            return safeDockReachable ? EscapePodDestinationType.SafeDock : EscapePodDestinationType.None;
        }

        /// <summary>The hit points an ejected player has when the pod opens: half their maximum, as after a respawn.</summary>
        public static int GetEjectionHitPoints(int maxHitPoints)
        {
            return maxHitPoints <= 1 ? 1 : maxHitPoints / 2;
        }
    }
}
