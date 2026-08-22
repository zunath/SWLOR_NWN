using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.GameData.Tilesets
{
    /// <summary>
    /// Read/write access to an area's tile grid (the .are "Tile_List"), addressed by
    /// (column, row) rather than raw list index. The grid is row-major, width columns per row -
    /// the same layout <see cref="Render.AreaSceneBuilder"/> renders and the corpus stores
    /// (index = row * width + col). Mutations touch only the addressed cell's Tile_ID /
    /// Tile_Orientation / Tile_Height fields in place, so a paint or rotate produces the smallest
    /// possible diff and undoes cleanly; the paint tools drive these under one
    /// DocumentTransaction. Reading is index-safe (out-of-range returns null); writing an
    /// out-of-range cell is a no-op.
    /// </summary>
    public static class AreaTiles
    {
        /// <summary>Aurora's lowest editable tile level; lowering never creates negative terrain.</summary>
        public const int MinimumHeightLevel = 0;

        public static int Width(AreDocument are) => are.Width ?? 0;

        public static int Height(AreDocument are) => are.Height ?? 0;

        /// <summary>The Tile_List index of (col,row), or -1 when the cell is outside the grid.</summary>
        public static int IndexOf(AreDocument are, int col, int row)
        {
            var width = Width(are);
            var height = Height(are);
            if (width <= 0 || height <= 0 || col < 0 || row < 0 || col >= width || row >= height)
                return -1;

            return row * width + col;
        }

        /// <summary>The tile placed at (col,row) as a (TileId, Orientation) pair, or null when the cell is out of range or has no tile struct.</summary>
        public static TileCandidate? At(AreDocument are, int col, int row)
        {
            return StateAt(are, col, row)?.Candidate;
        }

        /// <summary>The tile placement and base elevation at (col,row), or null outside the grid.</summary>
        public static PlacedTileState? StateAt(AreDocument are, int col, int row)
        {
            var idx = IndexOf(are, col, row);
            var tiles = are.Tiles;
            if (idx < 0 || idx >= tiles.Count)
                return null;

            var tile = tiles[idx];
            var id = tile.GetIntOrNull("Tile_ID");
            if (id == null)
                return null;

            return new PlacedTileState(
                id.Value,
                tile.GetIntOrNull("Tile_Orientation") ?? 0,
                tile.GetIntOrNull("Tile_Height") ?? 0);
        }

        /// <summary>A neighbour-lookup closure over this area for <see cref="SetRuleMatcher.SolveCell"/> / <see cref="TilePainter"/>.</summary>
        public static Func<int, int, TileCandidate?> Reader(AreDocument are) => (c, r) => At(are, c, r);

        /// <summary>A height-aware neighbour lookup for terrain solving and rotation validation.</summary>
        public static Func<int, int, PlacedTileState?> StateReader(AreDocument are) => (c, r) => StateAt(are, c, r);

        /// <summary>The height level (Tile_Height) of (col,row), or 0 when out of range.</summary>
        public static int HeightLevelOf(AreDocument are, int col, int row)
        {
            var idx = IndexOf(are, col, row);
            var tiles = are.Tiles;
            if (idx < 0 || idx >= tiles.Count)
                return 0;

            return tiles[idx].GetIntOrNull("Tile_Height") ?? 0;
        }

        /// <summary>
        /// Sets the tile id and orientation of the cell at (col,row) in place, writing each field
        /// only when it actually changes (so an unchanged paint captures no undo edit). No-op for an
        /// out-of-range cell or one with no backing struct. Must be called inside a
        /// DocumentTransaction when the owning document is attached to a session.
        /// </summary>
        public static void SetTile(AreDocument are, int col, int row, int tileId, int orientation)
        {
            var tile = TileStructAt(are, col, row);
            if (tile == null)
                return;

            if ((tile.GetIntOrNull("Tile_ID") ?? int.MinValue) != tileId)
                tile.SetInt("Tile_ID", GffFieldType.Int, tileId);
            if ((tile.GetIntOrNull("Tile_Orientation") ?? int.MinValue) != orientation)
                tile.SetInt("Tile_Orientation", GffFieldType.Int, orientation);
        }

        /// <summary>Sets just the orientation of (col,row) in place (the rotate tool). No-op when unchanged or out of range.</summary>
        public static void SetOrientation(AreDocument are, int col, int row, int orientation)
        {
            var tile = TileStructAt(are, col, row);
            if (tile != null && (tile.GetIntOrNull("Tile_Orientation") ?? int.MinValue) != orientation)
                tile.SetInt("Tile_Orientation", GffFieldType.Int, orientation);
        }

        /// <summary>Sets the height level (Tile_Height) of (col,row) in place (the raise/lower tool). No-op when unchanged or out of range.</summary>
        public static void SetHeightLevel(AreDocument are, int col, int row, int heightLevel)
        {
            var tile = TileStructAt(are, col, row);
            if (tile != null && (tile.GetIntOrNull("Tile_Height") ?? int.MinValue) != heightLevel)
                tile.SetInt("Tile_Height", GffFieldType.Int, heightLevel);
        }

        /// <summary>
        /// Raises or lowers one cell by whole tile levels. Returns false outside the grid or when
        /// lowering would cross <see cref="MinimumHeightLevel"/>.
        /// </summary>
        public static bool TryAdjustHeightLevel(AreDocument are, int col, int row, int delta)
        {
            if (delta == 0 || StateAt(are, col, row) is not { } state)
                return false;

            var adjusted = state.HeightLevel + delta;
            if (adjusted < MinimumHeightLevel)
                return false;

            SetHeightLevel(are, col, row, adjusted);
            return true;
        }

        private static JsonGffStruct? TileStructAt(AreDocument are, int col, int row)
        {
            var idx = IndexOf(are, col, row);
            var tiles = are.Tiles;
            return idx >= 0 && idx < tiles.Count ? tiles[idx] : null;
        }
    }
}
