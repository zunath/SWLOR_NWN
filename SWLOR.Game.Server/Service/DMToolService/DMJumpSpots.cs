using System.Collections.Generic;
using System.Numerics;

namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// The points a DM jump tries, in order. The engine cancels a jump, without telling anyone, when it finds no
    /// safe point within 20 meters of the target, so a jump needs other points to fall back on.
    /// </summary>
    public static class DMJumpSpots
    {
        private const float TileSize = 10f;

        // Cells no wider than this keep every point within the engine's search radius of a cell's center.
        private const float GridSpacing = 2 * TileSize;

        private const float NearbyDistance = 1.5f;
        private const int NearbyDirections = 8;

        /// <summary>
        /// Returns the center of an area, where the native chooser lands, then points covering the rest of it.
        /// </summary>
        /// <param name="widthInTiles">The area's width in tiles.</param>
        /// <param name="heightInTiles">The area's height in tiles.</param>
        public static IEnumerable<Vector2> GetAreaSpots(int widthInTiles, int heightInTiles)
        {
            var width = widthInTiles * TileSize;
            var height = heightInTiles * TileSize;
            yield return new Vector2(width / 2f, height / 2f);

            var columns = (int)MathF.Ceiling(width / GridSpacing);
            var rows = (int)MathF.Ceiling(height / GridSpacing);
            for (var column = 0; column < columns; column++)
            {
                for (var row = 0; row < rows; row++)
                {
                    yield return new Vector2((column + 0.5f) * width / columns, (row + 0.5f) * height / rows);
                }
            }
        }

        /// <summary>
        /// Returns points just around a target. Closed doors stand where the engine refuses to land, but the floor
        /// on either side of them is fine.
        /// </summary>
        /// <param name="center">The target's position.</param>
        public static IEnumerable<Vector2> GetSpotsAround(Vector2 center)
        {
            for (var direction = 0; direction < NearbyDirections; direction++)
            {
                var angle = direction * 2f * MathF.PI / NearbyDirections;
                yield return center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * NearbyDistance;
            }
        }
    }
}
