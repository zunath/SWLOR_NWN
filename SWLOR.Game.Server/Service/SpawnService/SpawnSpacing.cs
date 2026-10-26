using System.Collections.Generic;
using System.Numerics;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service.SpawnService
{
    /// <summary>
    /// Keeps randomly placed spawns apart so that clusters of resource nodes cannot wall off a path.
    /// </summary>
    public static class SpawnSpacing
    {
        /// <summary>
        /// Minimum horizontal distance, in meters, between randomly placed placeable spawns in the same area.
        /// Resource node models block about 2 meters, so this leaves a walkable gap between any two nodes.
        /// </summary>
        public const float MinimumPlaceableDistance = 6.0f;

        /// <summary>
        /// Number of random locations tried before settling for the most open candidate.
        /// </summary>
        public const int MaximumAttempts = 10;

        /// <summary>
        /// Picks a position from the candidate source which is at least <paramref name="minimumDistance"/>
        /// from every occupied position. If no candidate qualifies within <paramref name="maximumAttempts"/>,
        /// the candidate farthest from its nearest occupied position is returned.
        /// </summary>
        /// <param name="nextCandidate">Supplies a new random candidate position on each call.</param>
        /// <param name="occupiedPositions">Positions of spawns already present in the area.</param>
        /// <param name="minimumDistance">Minimum horizontal distance required from every occupied position.</param>
        /// <param name="maximumAttempts">Maximum number of candidates to draw.</param>
        /// <returns>The chosen position.</returns>
        public static Vector3 PickSpacedPosition(
            Func<Vector3> nextCandidate,
            IReadOnlyCollection<Vector3> occupiedPositions,
            float minimumDistance = MinimumPlaceableDistance,
            int maximumAttempts = MaximumAttempts)
        {
            var best = Vector3.Zero;
            var bestDistance = -1f;

            for (var attempt = 0; attempt < Math.Max(1, maximumAttempts); attempt++)
            {
                var candidate = nextCandidate();
                var nearest = GetNearestHorizontalDistance(candidate, occupiedPositions);

                if (nearest >= minimumDistance)
                    return candidate;

                if (nearest > bestDistance)
                {
                    best = candidate;
                    bestDistance = nearest;
                }
            }

            return best;
        }

        private static float GetNearestHorizontalDistance(Vector3 position, IReadOnlyCollection<Vector3> occupiedPositions)
        {
            var nearest = float.MaxValue;
            foreach (var occupied in occupiedPositions)
            {
                var distance = Vector2.Distance(new Vector2(position.X, position.Y), new Vector2(occupied.X, occupied.Y));
                if (distance < nearest)
                    nearest = distance;
            }

            return nearest;
        }
    }
}
