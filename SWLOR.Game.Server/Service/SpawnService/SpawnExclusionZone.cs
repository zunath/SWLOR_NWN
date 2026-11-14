using System.Collections.Generic;
using System.Numerics;

namespace SWLOR.Game.Server.Service.SpawnService
{
    /// <summary>
    /// The ground one player city keeps clear of new spawns: a buffer around every qualifying structure and, when the
    /// city has a City Hall, the Settled zone around it. Distances are horizontal, in meters.
    /// <see cref="SpawnExclusionRules"/> decides whether a position falls inside it.
    /// </summary>
    public class SpawnExclusionZone
    {
        /// <summary>
        /// A zone that excludes nothing.
        /// </summary>
        public static readonly SpawnExclusionZone None = new(new List<Vector2>(), null, 0f);

        /// <summary>
        /// Creates a zone.
        /// </summary>
        /// <param name="structurePositions">Horizontal positions of the qualifying structures.</param>
        /// <param name="settledCenter">The City Hall position, or null when the city has none.</param>
        /// <param name="settledRadius">The radius of the Settled zone around <paramref name="settledCenter"/>.</param>
        public SpawnExclusionZone(
            IReadOnlyList<Vector2> structurePositions,
            Vector2? settledCenter,
            float settledRadius)
        {
            StructurePositions = structurePositions;
            SettledCenter = settledCenter;
            SettledRadius = settledRadius;
        }

        /// <summary>
        /// Horizontal positions of the qualifying structures.
        /// </summary>
        public IReadOnlyList<Vector2> StructurePositions { get; }

        /// <summary>
        /// The center of the Settled zone (City Hall), or null when the city has none.
        /// </summary>
        public Vector2? SettledCenter { get; }

        /// <summary>
        /// The radius of the Settled zone.
        /// </summary>
        public float SettledRadius { get; }
    }
}
