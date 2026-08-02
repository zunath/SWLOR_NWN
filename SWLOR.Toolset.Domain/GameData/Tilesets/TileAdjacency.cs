namespace SWLOR.Toolset.Domain.GameData.Tilesets
{
    /// <summary>A tile's four world-facing edges (after its placement orientation is applied).</summary>
    public enum TileEdge { North, East, South, West }

    /// <summary>A tile's four world-facing corners (after its placement orientation is applied).</summary>
    public enum TileCorner { NorthEast, NorthWest, SouthWest, SouthEast }

    /// <summary>
    /// Maps a placed tile's local corner terrains and edge crossers to their WORLD orientation, and
    /// tests whether two adjacent tiles are consistent. A tile's <c>Tile_Orientation</c> (0-3) is a
    /// counter-clockwise quarter turn about the tile centre - the same convention
    /// <see cref="Render.AreaSceneBuilder"/> uses to place the tile model - with north = +Y and
    /// east = +X. In the unrotated tile, TopLeft is the NW corner, TopRight the NE, BottomLeft the
    /// SW, BottomRight the SE; the Top edge faces north, Right east, Bottom south, Left west.
    ///
    /// <para>
    /// Validated against the full 438-area corpus (see <c>SetRuleCorpusTests</c>): under this
    /// mapping every adjacent tile pair shares matching corner terrain in 99.971% of ~392k shared
    /// corners - the only exceptions are the <c>fcx01</c> tileset's special "holes" gap terrain
    /// abutting cobble - and compatible edge crossers everywhere (a crosser is only required to
    /// match when both sides declare one; a blank side is always compatible, since a crosser such
    /// as a wall or doorway is placed by one tile and the neighbour need not repeat it).
    /// </para>
    /// </summary>
    public static class TileAdjacency
    {
        /// <summary>The world-orientation terrain at one corner of a tile placed with the given orientation (0-3).</summary>
        public static string WorldCornerTerrain(TileDefinition tile, int orientation, TileCorner corner)
        {
            return Normalize(CornerAngle(corner) - orientation * 90) switch
            {
                45 => tile.TopRight,   // NE
                135 => tile.TopLeft,   // NW
                225 => tile.BottomLeft, // SW
                315 => tile.BottomRight, // SE
                _ => ""
            };
        }

        /// <summary>The .set corner height presented at one world corner after placement rotation.</summary>
        public static int WorldCornerHeight(TileDefinition tile, int orientation, TileCorner corner)
        {
            return Normalize(CornerAngle(corner) - orientation * 90) switch
            {
                45 => tile.TopRightHeight,
                135 => tile.TopLeftHeight,
                225 => tile.BottomLeftHeight,
                315 => tile.BottomRightHeight,
                _ => 0
            };
        }

        /// <summary>The world-orientation crosser on one edge of a tile placed with the given orientation (0-3).</summary>
        public static string WorldEdgeCrosser(TileDefinition tile, int orientation, TileEdge edge)
        {
            return Normalize(EdgeAngle(edge) - orientation * 90) switch
            {
                0 => tile.Right,   // E
                90 => tile.Top,    // N
                180 => tile.Left,  // W
                270 => tile.Bottom, // S
                _ => ""
            };
        }

        /// <summary>The world-corner that faces the neighbour across an edge, from the neighbour's side. Its terrain must match this tile's corner on the same edge.</summary>
        public static (TileCorner Near, TileCorner Far) SharedCorners(TileEdge edge) => edge switch
        {
            // East edge: this tile's NE/SE corners meet the neighbour's NW/SW corners.
            TileEdge.East => (TileCorner.NorthEast, TileCorner.SouthEast),
            TileEdge.West => (TileCorner.NorthWest, TileCorner.SouthWest),
            // North edge: this tile's NW/NE corners meet the neighbour's SW/SE corners.
            TileEdge.North => (TileCorner.NorthWest, TileCorner.NorthEast),
            TileEdge.South => (TileCorner.SouthWest, TileCorner.SouthEast),
            _ => (TileCorner.NorthEast, TileCorner.SouthEast)
        };

        /// <summary>The edge on the far tile that abuts <paramref name="edge"/> on the near tile (opposite compass direction).</summary>
        public static TileEdge OppositeEdge(TileEdge edge) => edge switch
        {
            TileEdge.North => TileEdge.South,
            TileEdge.South => TileEdge.North,
            TileEdge.East => TileEdge.West,
            TileEdge.West => TileEdge.East,
            _ => edge
        };

        /// <summary>Shared-corner terrains are consistent when they name the same terrain (case-insensitive).</summary>
        public static bool CornerTerrainsMatch(string? a, string? b) =>
            string.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Shared-edge crossers are consistent when they name the same crosser (case-insensitive) OR
        /// either side is blank - a crosser is placed by one tile and the neighbour need not repeat
        /// it (validated: every corpus edge "mismatch" is a blank-vs-crosser pairing).
        /// </summary>
        public static bool EdgeCrossersMatch(string? a, string? b)
        {
            var x = a ?? "";
            var y = b ?? "";
            return x.Length == 0 || y.Length == 0 || string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
        }

        private static int CornerAngle(TileCorner c) => c switch
        {
            TileCorner.NorthEast => 45,
            TileCorner.NorthWest => 135,
            TileCorner.SouthWest => 225,
            TileCorner.SouthEast => 315,
            _ => 0
        };

        private static int EdgeAngle(TileEdge e) => e switch
        {
            TileEdge.East => 0,
            TileEdge.North => 90,
            TileEdge.West => 180,
            TileEdge.South => 270,
            _ => 0
        };

        private static int Normalize(int angle) => ((angle % 360) + 360) % 360;
    }
}
