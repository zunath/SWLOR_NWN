namespace SWLOR.Toolset.Domain.GameData.Tilesets
{
    /// <summary>
    /// A per-corner / per-edge requirement on a single grid cell: each corner terrain and edge
    /// crosser is either a required value or null (unconstrained - any value is acceptable). Used to
    /// query <see cref="SetRuleMatcher"/> for the tiles that can legally occupy the cell.
    /// </summary>
    public sealed class TileConstraint
    {
        public string? NorthWest { get; init; }
        public string? NorthEast { get; init; }
        public string? SouthWest { get; init; }
        public string? SouthEast { get; init; }

        public string? NorthEdge { get; init; }
        public string? EastEdge { get; init; }
        public string? SouthEdge { get; init; }
        public string? WestEdge { get; init; }

        public string? Corner(TileCorner corner) => corner switch
        {
            TileCorner.NorthWest => NorthWest,
            TileCorner.NorthEast => NorthEast,
            TileCorner.SouthWest => SouthWest,
            TileCorner.SouthEast => SouthEast,
            _ => null
        };

        public string? Edge(TileEdge edge) => edge switch
        {
            TileEdge.North => NorthEdge,
            TileEdge.East => EastEdge,
            TileEdge.South => SouthEdge,
            TileEdge.West => WestEdge,
            _ => null
        };

        /// <summary>A copy of this constraint with one corner set (used while gathering constraints from placed neighbours).</summary>
        public TileConstraint WithCorner(TileCorner corner, string? value) => new()
        {
            NorthWest = corner == TileCorner.NorthWest ? value : NorthWest,
            NorthEast = corner == TileCorner.NorthEast ? value : NorthEast,
            SouthWest = corner == TileCorner.SouthWest ? value : SouthWest,
            SouthEast = corner == TileCorner.SouthEast ? value : SouthEast,
            NorthEdge = NorthEdge,
            EastEdge = EastEdge,
            SouthEdge = SouthEdge,
            WestEdge = WestEdge
        };
    }

    /// <summary>One tile placement option: a tile index into the tileset and a 0-3 orientation.</summary>
    public readonly record struct TileCandidate(int TileId, int Orientation);

    /// <summary>
    /// Solves which tiles can legally occupy a cell given corner/edge requirements, using the
    /// corner-terrain + edge-crosser + orientation rules validated in <see cref="TileAdjacency"/>
    /// (see <c>SetRuleCorpusTests</c>). This is the WP7.2 engine the paint tools (WP7.3) drive: a
    /// paint fixes some corners, placed neighbours fix others, and this returns the tiles that
    /// satisfy every fixed corner/edge. Underspecified constraints legitimately return many
    /// candidates; the caller chooses among them (e.g. preferring the corpus's most-used tile).
    /// </summary>
    public static class SetRuleMatcher
    {
        /// <summary>
        /// Every (tileId, orientation) in <paramref name="tileset"/> whose world-oriented corner
        /// terrains and edge crossers satisfy <paramref name="constraint"/>. A null corner/edge in
        /// the constraint is unconstrained. Corner terrain must match exactly (case-insensitive);
        /// edge crossers use the blank-tolerant rule (a blank on either side is compatible). Returns
        /// every matching orientation of a tile separately, so a symmetric tile can appear up to four
        /// times. Never throws; an empty result means the cell is unpaintable under the constraint.
        /// </summary>
        public static IReadOnlyList<TileCandidate> FindMatchingTiles(TilesetDefinition tileset, TileConstraint constraint)
        {
            ArgumentNullException.ThrowIfNull(tileset);
            ArgumentNullException.ThrowIfNull(constraint);

            var matches = new List<TileCandidate>();

            for (var tileId = 0; tileId < tileset.Tiles.Count; tileId++)
            {
                var tile = tileset.Tiles[tileId];
                for (var orientation = 0; orientation < 4; orientation++)
                {
                    if (Satisfies(tile, orientation, constraint))
                        matches.Add(new TileCandidate(tileId, orientation));
                }
            }

            return matches;
        }

        /// <summary>
        /// Builds the corner constraint a cell inherits from its already-placed orthogonal
        /// neighbours: each corner is pinned by whichever adjacent tile shares it (a corner not
        /// touched by any placed neighbour stays null/unconstrained, e.g. at the grid border or
        /// beside an empty cell). <paramref name="placedAt"/> returns the tile placed at a grid
        /// (col,row), or null for an empty or out-of-bounds cell. Edge crossers are left
        /// unconstrained here - the blank-tolerant rule means a neighbour's crosser never forces
        /// this cell to carry one.
        /// </summary>
        public static TileConstraint ConstraintFromNeighbours(
            TilesetDefinition tileset, int col, int row, Func<int, int, TileCandidate?> placedAt)
        {
            ArgumentNullException.ThrowIfNull(tileset);
            ArgumentNullException.ThrowIfNull(placedAt);

            string? Shared(int nc, int nr, TileCorner theirCorner)
            {
                if (placedAt(nc, nr) is not { } t || t.TileId < 0 || t.TileId >= tileset.Tiles.Count)
                    return null;
                return TileAdjacency.WorldCornerTerrain(tileset.Tiles[t.TileId], t.Orientation, theirCorner);
            }

            return new TileConstraint
            {
                // Each corner is shared with the tile to its side and the tile above/below; take
                // whichever is present (they agree in consistent data).
                NorthWest = Shared(col - 1, row, TileCorner.NorthEast) ?? Shared(col, row + 1, TileCorner.SouthWest),
                NorthEast = Shared(col + 1, row, TileCorner.NorthWest) ?? Shared(col, row + 1, TileCorner.SouthEast),
                SouthWest = Shared(col - 1, row, TileCorner.SouthEast) ?? Shared(col, row - 1, TileCorner.NorthWest),
                SouthEast = Shared(col + 1, row, TileCorner.SouthWest) ?? Shared(col, row - 1, TileCorner.NorthEast)
            };
        }

        /// <summary>
        /// The tiles that can legally occupy (<paramref name="col"/>, <paramref name="row"/>) given
        /// its placed neighbours, optionally with a paint override that forces specific corner
        /// terrains (a painted corner wins over the neighbour-inherited value). Convenience over
        /// <see cref="ConstraintFromNeighbours"/> + <see cref="FindMatchingTiles"/> for the WP7.3
        /// paint tools.
        /// </summary>
        public static IReadOnlyList<TileCandidate> SolveCell(
            TilesetDefinition tileset, int col, int row, Func<int, int, TileCandidate?> placedAt,
            IReadOnlyDictionary<TileCorner, string>? paintedCorners = null)
        {
            var constraint = ConstraintFromNeighbours(tileset, col, row, placedAt);

            if (paintedCorners != null)
            {
                foreach (var (corner, terrain) in paintedCorners)
                    constraint = constraint.WithCorner(corner, terrain);
            }

            return FindMatchingTiles(tileset, constraint);
        }

        private static bool Satisfies(TileDefinition tile, int orientation, TileConstraint constraint)
        {
            foreach (var corner in AllCorners)
            {
                var required = constraint.Corner(corner);
                if (required != null &&
                    !TileAdjacency.CornerTerrainsMatch(TileAdjacency.WorldCornerTerrain(tile, orientation, corner), required))
                    return false;
            }

            foreach (var edge in AllEdges)
            {
                var required = constraint.Edge(edge);
                if (required != null &&
                    !TileAdjacency.EdgeCrossersMatch(TileAdjacency.WorldEdgeCrosser(tile, orientation, edge), required))
                    return false;
            }

            return true;
        }

        private static readonly TileCorner[] AllCorners =
        {
            TileCorner.NorthWest, TileCorner.NorthEast, TileCorner.SouthWest, TileCorner.SouthEast
        };

        private static readonly TileEdge[] AllEdges =
        {
            TileEdge.North, TileEdge.East, TileEdge.South, TileEdge.West
        };
    }
}
