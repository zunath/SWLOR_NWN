namespace SWLOR.Toolset.Domain.GameData.Tilesets
{
    /// <summary>One cell the paint tool would rewrite: the grid position and its new tile placement.</summary>
    public readonly record struct TilePaintChange(int Col, int Row, int TileId, int Orientation);

    /// <summary>
    /// The WP7.3 terrain paint engine. A paint "fills" one cell with a chosen terrain (all four
    /// corners) and then re-solves the eight surrounding cells so the boundary blends, driving the
    /// WP7.2 <see cref="SetRuleMatcher"/> throughout. It is a pure function of the current grid,
    /// tileset, and brush - it returns the set of cells that would change (never mutating anything),
    /// so the caller can apply them as a single transaction.
    ///
    /// Two properties make repeated painting well-behaved:
    /// <list type="bullet">
    /// <item><b>Stability:</b> a cell keeps its current tile whenever that tile is still legal under
    /// the new constraints, so a blend only rewrites cells it must - minimal diff.</item>
    /// <item><b>Idempotency:</b> painting the same terrain on the same cell twice is a fixed point;
    /// the second paint returns no changes (verified against the corpus).</item>
    /// </list>
    /// When a solved cell is still underspecified (many legal tiles), selection prefers - in order -
    /// the currently placed tile, then the caller's corpus-frequency ranking (see
    /// <see cref="TileUsageStatistics"/>), then a deterministic lowest-id/lowest-orientation pick, so
    /// output never depends on tileset iteration incidentals.
    /// </summary>
    public static class TilePainter
    {
        /// <summary>
        /// Computes the cells a whole-tile terrain paint at (<paramref name="col"/>,
        /// <paramref name="row"/>) would rewrite. The clicked cell is filled with
        /// <paramref name="terrain"/> (a solid, crosser-free tile when the tileset has one) and its
        /// eight neighbours are re-blended. Returns an empty list for an out-of-range cell, a blank
        /// terrain, or a terrain the tileset cannot present as a full tile.
        /// <paramref name="tileRank"/> maps a tile id to a preference rank (lower = preferred, e.g.
        /// negated corpus frequency); null falls back to lowest-id.
        /// </summary>
        public static IReadOnlyList<TilePaintChange> PaintTerrain(
            TilesetDefinition tileset, int width, int height,
            Func<int, int, TileCandidate?> currentAt,
            int col, int row, string terrain,
            Func<int, int>? tileRank = null)
        {
            ArgumentNullException.ThrowIfNull(tileset);
            ArgumentNullException.ThrowIfNull(currentAt);

            if (string.IsNullOrWhiteSpace(terrain) ||
                col < 0 || row < 0 || col >= width || row >= height)
                return Array.Empty<TilePaintChange>();

            // A working overlay so each neighbour re-solve sees the freshly painted centre (and any
            // neighbour already re-blended this pass) rather than the stale on-disk grid.
            var overlay = new Dictionary<(int, int), TileCandidate>();
            TileCandidate? WorkingAt(int c, int r) =>
                overlay.TryGetValue((c, r), out var v) ? v : currentAt(c, r);

            var changes = new List<TilePaintChange>();

            void Place(int c, int r, TileCandidate chosen)
            {
                var before = WorkingAt(c, r);
                overlay[(c, r)] = chosen;
                if (before is not { } prev || prev != chosen)
                    changes.Add(new TilePaintChange(c, r, chosen.TileId, chosen.Orientation));
            }

            // 1) Centre cell: force every corner to the painted terrain, preferring a crosser-free
            //    (solid) tile so a plain terrain dab never drops a wall/doorway into the fill.
            var centreConstraint = new TileConstraint
            {
                NorthWest = terrain, NorthEast = terrain, SouthWest = terrain, SouthEast = terrain
            };
            var centreCandidates = SetRuleMatcher.FindMatchingTiles(tileset, centreConstraint);
            var centre = SelectCandidate(tileset, centreCandidates, WorkingAt(col, row), tileRank, preferBlankEdges: true);
            if (centre is not { } centreChoice)
                return Array.Empty<TilePaintChange>(); // terrain not presentable as a full tile here

            Place(col, row, centreChoice);

            // 2) Blend the eight-neighbour ring: each re-solves against the (now updated) grid and
            //    keeps its own tile when still legal, so only cells the new boundary forces actually
            //    change.
            for (var dr = -1; dr <= 1; dr++)
            for (var dc = -1; dc <= 1; dc++)
            {
                if (dc == 0 && dr == 0)
                    continue;

                var nc = col + dc;
                var nr = row + dr;
                if (nc < 0 || nr < 0 || nc >= width || nr >= height)
                    continue;
                if (WorkingAt(nc, nr) is null)
                    continue; // never fill a cell that has no tile yet

                var candidates = SetRuleMatcher.FindMatchingTiles(
                    tileset, ConstraintFromVertices(tileset, nc, nr, currentAt, overlay));
                var choice = SelectCandidate(tileset, candidates, WorkingAt(nc, nr), tileRank, preferBlankEdges: false);
                if (choice is { } chosen)
                    Place(nc, nr, chosen);
                // No legal blend (choice == null): leave the neighbour as-is rather than clearing it.
            }

            return changes;
        }

        /// <summary>
        /// The tile that best fills a whole cell with <paramref name="terrain"/> (all four corners),
        /// preferring a crosser-free tile, then the caller's ranking, then lowest id - or null when
        /// the tileset has no such tile. Used by the new-area wizard to pick its blank-canvas fill.
        /// "Solid" here means uniform terrain with no edge features, NOT necessarily walkable - an
        /// interior tileset's fill terrain is typically solid rock (see <see cref="DefaultFillTerrain"/>).
        /// </summary>
        public static TileCandidate? FindSolidTile(TilesetDefinition tileset, string terrain, Func<int, int>? tileRank = null)
        {
            ArgumentNullException.ThrowIfNull(tileset);
            if (string.IsNullOrWhiteSpace(terrain))
                return null;

            var constraint = new TileConstraint
            {
                NorthWest = terrain, NorthEast = terrain, SouthWest = terrain, SouthEast = terrain
            };
            var candidates = SetRuleMatcher.FindMatchingTiles(tileset, constraint);
            return SelectCandidate(tileset, candidates, null, tileRank, preferBlankEdges: true);
        }

        /// <summary>
        /// The tileset's declared blank-canvas terrain: its Floor or Default surface when that names
        /// a fillable terrain, otherwise the first terrain the tileset can present as a full solid
        /// tile. Null when nothing fills (degenerate tileset).
        ///
        /// This is what a NEW area of the tileset is made of, which is not the same as "walkable
        /// ground": exterior tilesets declare ground here (tms01 Floor=Grass), interior ones declare
        /// solid rock (tib01 Floor=Wall, whose walkable terrain is Room). Both are correct - an
        /// interior area is meant to start solid and have its rooms painted out of it.
        /// </summary>
        public static string? DefaultFillTerrain(TilesetDefinition tileset, Func<int, int>? tileRank = null)
        {
            ArgumentNullException.ThrowIfNull(tileset);

            foreach (var candidate in PreferredTerrainOrder(tileset))
            {
                if (!string.IsNullOrWhiteSpace(candidate) && FindSolidTile(tileset, candidate, tileRank) != null)
                    return candidate;
            }

            return null;
        }

        /// <summary>The tileset's fillable terrains (each has at least one solid tile), Floor/Default first, for a paint palette.</summary>
        public static IReadOnlyList<string> FillableTerrains(TilesetDefinition tileset, Func<int, int>? tileRank = null)
        {
            ArgumentNullException.ThrowIfNull(tileset);

            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var candidate in PreferredTerrainOrder(tileset))
            {
                if (string.IsNullOrWhiteSpace(candidate) || !seen.Add(candidate))
                    continue;
                if (FindSolidTile(tileset, candidate, tileRank) != null)
                    result.Add(candidate);
            }

            return result;
        }

        private static IEnumerable<string> PreferredTerrainOrder(TilesetDefinition tileset)
        {
            if (!string.IsNullOrWhiteSpace(tileset.Floor))
                yield return tileset.Floor;
            if (!string.IsNullOrWhiteSpace(tileset.Default))
                yield return tileset.Default;
            foreach (var terrain in tileset.Terrains)
                yield return terrain.Name;
        }

        /// <summary>
        /// The corner constraint for one cell, derived from every cell that touches each of its four
        /// corner vertices.
        ///
        /// A corner vertex is shared by up to four cells, and
        /// <see cref="SetRuleMatcher.ConstraintFromNeighbours"/> resolves it as
        /// "horizontal neighbour ?? vertical neighbour" - correct over consistent corpus data, where
        /// every cell around a vertex names the same terrain, but wrong mid-paint: the grid is
        /// deliberately inconsistent for a moment, and a stale horizontal neighbour would mask the
        /// cell we just painted. So this consults all three other cells at each vertex and lets a
        /// cell already decided <b>this pass</b> (present in <paramref name="overlay"/>) win, falling
        /// back to the first still-unpainted neighbour otherwise. Because each cell solved later
        /// matches whatever was decided before it, the pass ends corner-consistent - which is what
        /// makes a repeated paint a fixed point.
        /// </summary>
        private static TileConstraint ConstraintFromVertices(
            TilesetDefinition tileset, int col, int row,
            Func<int, int, TileCandidate?> currentAt,
            IReadOnlyDictionary<(int, int), TileCandidate> overlay)
        {
            string? TerrainAt(TileCandidate tile, TileCorner theirCorner) =>
                tile.TileId >= 0 && tile.TileId < tileset.Tiles.Count
                    ? TileAdjacency.WorldCornerTerrain(tileset.Tiles[tile.TileId], tile.Orientation, theirCorner)
                    : null;

            string? Corner(TileCorner corner)
            {
                string? stale = null;
                foreach (var (dc, dr, theirCorner) in VertexNeighbours(corner))
                {
                    var key = (col + dc, row + dr);
                    if (overlay.TryGetValue(key, out var fresh))
                        return TerrainAt(fresh, theirCorner);

                    if (stale == null && currentAt(key.Item1, key.Item2) is { } placed)
                        stale = TerrainAt(placed, theirCorner);
                }

                return stale;
            }

            return new TileConstraint
            {
                NorthWest = Corner(TileCorner.NorthWest),
                NorthEast = Corner(TileCorner.NorthEast),
                SouthWest = Corner(TileCorner.SouthWest),
                SouthEast = Corner(TileCorner.SouthEast)
            };
        }

        /// <summary>
        /// The three other cells meeting at one of this cell's corner vertices, as
        /// (column offset, row offset, the corner THEY present at that same vertex): the two
        /// orthogonal neighbours flanking the corner plus the diagonal one opposite it.
        /// </summary>
        private static (int Dc, int Dr, TileCorner Corner)[] VertexNeighbours(TileCorner corner) => corner switch
        {
            TileCorner.NorthWest => new[]
            {
                (-1, 0, TileCorner.NorthEast), (0, 1, TileCorner.SouthWest), (-1, 1, TileCorner.SouthEast)
            },
            TileCorner.NorthEast => new[]
            {
                (1, 0, TileCorner.NorthWest), (0, 1, TileCorner.SouthEast), (1, 1, TileCorner.SouthWest)
            },
            TileCorner.SouthWest => new[]
            {
                (-1, 0, TileCorner.SouthEast), (0, -1, TileCorner.NorthWest), (-1, -1, TileCorner.NorthEast)
            },
            _ => new[]
            {
                (1, 0, TileCorner.SouthWest), (0, -1, TileCorner.NorthEast), (1, -1, TileCorner.NorthWest)
            }
        };

        /// <summary>
        /// Picks one candidate: the current tile when it is still legal (stability/idempotency),
        /// otherwise - optionally restricted to crosser-free tiles - the lowest-ranked then
        /// lowest-id/lowest-orientation option. Null only when there are no candidates.
        /// </summary>
        private static TileCandidate? SelectCandidate(
            TilesetDefinition tileset, IReadOnlyList<TileCandidate> candidates,
            TileCandidate? current, Func<int, int>? tileRank, bool preferBlankEdges)
        {
            if (candidates.Count == 0)
                return null;

            if (current is { } cur && candidates.Contains(cur))
                return cur;

            IEnumerable<TileCandidate> pool = candidates;
            if (preferBlankEdges)
            {
                var blank = candidates.Where(c => AllEdgesBlank(tileset.Tiles[c.TileId])).ToList();
                if (blank.Count > 0)
                    pool = blank;
            }

            return pool
                .OrderBy(c => tileRank?.Invoke(c.TileId) ?? c.TileId)
                .ThenBy(c => c.TileId)
                .ThenBy(c => c.Orientation)
                .First();
        }

        private static bool AllEdgesBlank(TileDefinition tile) =>
            string.IsNullOrEmpty(tile.Top) && string.IsNullOrEmpty(tile.Right) &&
            string.IsNullOrEmpty(tile.Bottom) && string.IsNullOrEmpty(tile.Left);
    }
}
