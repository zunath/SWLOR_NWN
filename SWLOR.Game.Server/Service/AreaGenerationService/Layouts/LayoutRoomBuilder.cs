using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AreaGenerationService.Layouts
{
    /// <summary>A rectangle in corner coordinates (inclusive on both ends), shared by rect-based room styles.</summary>
    internal readonly struct RoomRect
    {
        public RoomRect(int x0, int y0, int x1, int y1)
        {
            X0 = x0;
            Y0 = y0;
            X1 = x1;
            Y1 = y1;
        }

        public int X0 { get; }
        public int Y0 { get; }
        public int X1 { get; }
        public int Y1 { get; }

        public int CornerWidth => X1 - X0 + 1;
        public int CornerHeight => Y1 - Y0 + 1;
    }

    /// <summary>Builds <see cref="LayoutRoom"/> metadata (tiles + center tile) from carved open space.</summary>
    internal static class LayoutRoomBuilder
    {
        /// <summary>
        /// Builds a room from a rectangle already carved fully open in <paramref name="corners"/>.
        /// Tiles = every fully-open tile inside the rect; center = the fully-open tile nearest the
        /// rect's geometric center.
        /// </summary>
        internal static LayoutRoom BuildFromRect(int id, RoomRect rect, CornerTerrainGrid corners, string openTerrain)
        {
            var tiles = new List<(int X, int Y)>();
            for (var tx = rect.X0; tx < rect.X1; tx++)
            {
                for (var ty = rect.Y0; ty < rect.Y1; ty++)
                {
                    if (LayoutCornerUtils.IsTileFullyOpen(corners, tx, ty, openTerrain))
                        tiles.Add((tx, ty));
                }
            }

            var centerX = (rect.X0 + rect.X1 - 1) / 2.0;
            var centerY = (rect.Y0 + rect.Y1 - 1) / 2.0;
            var centerTile = PickNearestTile(tiles, centerX, centerY);

            return new LayoutRoom
            {
                Id = id,
                Role = RoomRole.Standard,
                CenterTile = centerTile,
                Tiles = tiles
            };
        }

        /// <summary>
        /// Builds a room by BFS-flooding fully-open tiles outward from a seed tile, capped at
        /// <paramref name="maxTiles"/> and skipping tiles already claimed by another room. The seed
        /// tile becomes the room's center.
        /// </summary>
        internal static LayoutRoom BuildFromSeed(
            int id, (int X, int Y) seedTile, CornerTerrainGrid corners, string openTerrain,
            int maxTiles, HashSet<(int X, int Y)> claimed)
        {
            var tiles = new List<(int X, int Y)>();
            var enqueued = new HashSet<(int X, int Y)> { seedTile };
            var queue = new Queue<(int X, int Y)>();
            queue.Enqueue(seedTile);

            while (queue.Count > 0 && tiles.Count < maxTiles)
            {
                var current = queue.Dequeue();
                if (claimed.Contains(current)) continue;
                if (!LayoutCornerUtils.IsTileFullyOpen(corners, current.X, current.Y, openTerrain)) continue;

                tiles.Add(current);
                claimed.Add(current);

                foreach (var (dx, dy) in LayoutCornerUtils.Ortho4)
                {
                    var next = (current.X + dx, current.Y + dy);
                    if (!enqueued.Add(next)) continue;
                    queue.Enqueue(next);
                }
            }

            return new LayoutRoom
            {
                Id = id,
                Role = RoomRole.Standard,
                CenterTile = seedTile,
                Tiles = tiles
            };
        }

        private static (int X, int Y) PickNearestTile(List<(int X, int Y)> tiles, double centerX, double centerY)
        {
            var best = tiles.Count > 0 ? tiles[0] : (0, 0);
            var bestDist = double.MaxValue;

            foreach (var t in tiles)
            {
                var dx = t.X - centerX;
                var dy = t.Y - centerY;
                var dist = dx * dx + dy * dy;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = t;
                }
            }

            return best;
        }
    }
}
