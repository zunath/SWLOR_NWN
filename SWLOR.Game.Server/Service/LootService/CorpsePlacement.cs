using System.Numerics;

namespace SWLOR.Game.Server.Service.LootService
{
    /// <summary>
    /// Picks where a corpse loot container is created for a dead creature.
    /// </summary>
    public static class CorpsePlacement
    {
        /// <summary>
        /// Distance, in meters, the container keeps from the area's outer boundary.
        /// A creature that moves toward a point outside the area stops exactly on the boundary,
        /// where there is no walkmesh and the engine reports a height of -6. A container created
        /// there is drawn below the floor and players cannot reach it.
        /// </summary>
        public const float EdgeMargin = 0.5f;

        /// <summary>
        /// How far the container is sunk below the body's height so it sits on the floor.
        /// </summary>
        public const float SinkDepth = 0.11f;

        /// <summary>
        /// Largest height difference, in meters, between a body near the boundary and the floor just
        /// inside it for the body to count as standing on that floor.
        /// </summary>
        public const float FloorTolerance = 0.5f;

        /// <summary>
        /// Returns the position for a dead creature's loot container. A body standing on the floor keeps
        /// its position. A body on or past the area's boundary, off the walkmesh, is moved just inside
        /// the boundary and placed on the ground height there.
        /// </summary>
        /// <param name="bodyPosition">The dead creature's position.</param>
        /// <param name="areaWidth">The area's width in meters.</param>
        /// <param name="areaHeight">The area's height in meters.</param>
        /// <param name="getGroundHeight">Returns the walkmesh height at a position in the area.</param>
        /// <returns>The container's position.</returns>
        public static Vector3 GetLootPosition(
            Vector3 bodyPosition,
            float areaWidth,
            float areaHeight,
            Func<Vector3, float> getGroundHeight)
        {
            var x = Math.Clamp(bodyPosition.X, EdgeMargin, Math.Max(EdgeMargin, areaWidth - EdgeMargin));
            var y = Math.Clamp(bodyPosition.Y, EdgeMargin, Math.Max(EdgeMargin, areaHeight - EdgeMargin));
            var z = bodyPosition.Z;

            if (x != bodyPosition.X || y != bodyPosition.Y)
            {
                var groundHeight = getGroundHeight(new Vector3(x, y, z));
                if (MathF.Abs(groundHeight - bodyPosition.Z) <= FloorTolerance)
                {
                    x = bodyPosition.X;
                    y = bodyPosition.Y;
                }
                else
                {
                    z = groundHeight;
                }
            }

            return new Vector3(x, y, z - SinkDepth);
        }
    }
}
