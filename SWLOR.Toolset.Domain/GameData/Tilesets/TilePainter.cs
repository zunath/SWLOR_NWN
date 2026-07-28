namespace SWLOR.Toolset.Domain.GameData.Tilesets
{
    /// <summary>One cell the paint tool would rewrite: the grid position and its new tile placement.</summary>
    public readonly record struct TilePaintChange(int Col, int Row, int TileId, int Orientation);

    /// <summary>
    /// The terrain paint engine, driving the <see cref="SetRuleMatcher"/> throughout. Two paint
    /// models share its machinery, both pure functions of the current grid, tileset, and brush
    /// (each returns the set of cells that would change, never mutating anything, so the caller
    /// applies them as one transaction):
    /// <list type="bullet">
    /// <item><see cref="PaintTerrainVertex"/> - the editor's brush, matching the reference
    /// toolset: one grid VERTEX takes the terrain and only the up-to-four cells sharing it
    /// re-solve.</item>
    /// <item><see cref="PaintTerrain"/> - a whole-cell fill (all four corners) with an
    /// eight-neighbour blend, kept for programmatic fills and the corpus idempotency gate.</item>
    /// </list>
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
        /// Returns whether rotating one populated cell to <paramref name="orientation"/> preserves
        /// its SET corner-terrain and edge-crosser boundaries with every orthogonal neighbour.
        /// Unknown tile ids are rejected. This lets the rotate tool reject an unsafe quarter turn
        /// atomically instead of leaving an invalid transition in the area.
        /// </summary>
        public static bool CanRotateTile(
            TilesetDefinition tileset,
            Func<int, int, TileCandidate?> currentAt,
            int col,
            int row,
            int orientation)
        {
            ArgumentNullException.ThrowIfNull(currentAt);
            PlacedTileState? StateAt(int c, int r) =>
                currentAt(c, r) is { } tile
                    ? new PlacedTileState(tile.TileId, tile.Orientation, 0)
                    : null;
            return CanRotateTile(tileset, StateAt, col, row, orientation);
        }

        public static bool CanRotateTile(
            TilesetDefinition tileset,
            Func<int, int, PlacedTileState?> currentAt,
            int col,
            int row,
            int orientation)
        {
            ArgumentNullException.ThrowIfNull(tileset);
            ArgumentNullException.ThrowIfNull(currentAt);

            if (currentAt(col, row) is not { } placed ||
                placed.TileId < 0 ||
                placed.TileId >= tileset.Tiles.Count)
                return false;

            var candidate = new TileCandidate(placed.TileId, ((orientation % 4) + 4) % 4);
            var candidateDefinition = tileset.Tiles[candidate.TileId];
            var neighbours = new (TileEdge Edge, int Dc, int Dr)[]
            {
                (TileEdge.North, 0, 1),
                (TileEdge.East, 1, 0),
                (TileEdge.South, 0, -1),
                (TileEdge.West, -1, 0)
            };

            foreach (var (edge, dc, dr) in neighbours)
            {
                if (currentAt(col + dc, row + dr) is not { } neighbour)
                    continue;
                if (neighbour.TileId < 0 || neighbour.TileId >= tileset.Tiles.Count)
                    return false;

                var opposite = TileAdjacency.OppositeEdge(edge);
                var neighbourDefinition = tileset.Tiles[neighbour.TileId];
                var (nearCorner, farCorner) = TileAdjacency.SharedCorners(edge);
                var (oppositeNearCorner, oppositeFarCorner) = TileAdjacency.SharedCorners(opposite);

                if (!TileAdjacency.CornerTerrainsMatch(
                        TileAdjacency.WorldCornerTerrain(candidateDefinition, candidate.Orientation, nearCorner),
                        TileAdjacency.WorldCornerTerrain(neighbourDefinition, neighbour.Orientation, oppositeNearCorner)) ||
                    placed.HeightLevel + TileAdjacency.WorldCornerHeight(
                        candidateDefinition, candidate.Orientation, nearCorner) !=
                    neighbour.HeightLevel + TileAdjacency.WorldCornerHeight(
                        neighbourDefinition, neighbour.Orientation, oppositeNearCorner) ||
                    !TileAdjacency.CornerTerrainsMatch(
                        TileAdjacency.WorldCornerTerrain(candidateDefinition, candidate.Orientation, farCorner),
                        TileAdjacency.WorldCornerTerrain(neighbourDefinition, neighbour.Orientation, oppositeFarCorner)) ||
                    placed.HeightLevel + TileAdjacency.WorldCornerHeight(
                        candidateDefinition, candidate.Orientation, farCorner) !=
                    neighbour.HeightLevel + TileAdjacency.WorldCornerHeight(
                        neighbourDefinition, neighbour.Orientation, oppositeFarCorner) ||
                    !TileAdjacency.EdgeCrossersMatch(
                        TileAdjacency.WorldEdgeCrosser(candidateDefinition, candidate.Orientation, edge),
                        TileAdjacency.WorldEdgeCrosser(neighbourDefinition, neighbour.Orientation, opposite)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Computes the cells a whole-tile terrain paint at (<paramref name="col"/>,
        /// <paramref name="row"/>) would rewrite. The clicked cell is filled with
        /// <paramref name="terrain"/> (a solid, crosser-free tile when the tileset has one) and its
        /// eight neighbours are re-blended. Returns an empty list for an out-of-range cell, a blank
        /// terrain, a terrain the tileset cannot present as a full tile, or a boundary whose
        /// populated neighbours cannot all be solved.
        /// <paramref name="tileRank"/> maps a tile id to a preference rank (lower = preferred, e.g.
        /// negated corpus frequency); null falls back to lowest-id.
        /// </summary>
        public static IReadOnlyList<TilePaintChange> PaintTerrain(
            TilesetDefinition tileset, int width, int height,
            Func<int, int, TileCandidate?> currentAt,
            int col, int row, string terrain,
            Func<int, int>? tileRank = null)
        {
            ArgumentNullException.ThrowIfNull(currentAt);
            PlacedTileState? StateAt(int c, int r) =>
                currentAt(c, r) is { } tile
                    ? new PlacedTileState(tile.TileId, tile.Orientation, 0)
                    : null;
            return PaintTerrain(tileset, width, height, StateAt, col, row, terrain, tileRank);
        }

        public static IReadOnlyList<TilePaintChange> PaintTerrain(
            TilesetDefinition tileset, int width, int height,
            Func<int, int, PlacedTileState?> currentAt,
            int col, int row, string terrain,
            Func<int, int>? tileRank = null)
        {
            ArgumentNullException.ThrowIfNull(tileset);
            ArgumentNullException.ThrowIfNull(currentAt);

            if (string.IsNullOrWhiteSpace(terrain) ||
                col < 0 || row < 0 || col >= width || row >= height)
                return Array.Empty<TilePaintChange>();

            // Every cell this paint may rewrite. Edges BETWEEN these cells are jointly mutable -
            // both sides get re-solved this pass, so neither side's stale pre-paint crosser is a
            // hard constraint on the other. Edges to cells outside the set stay hard, since those
            // tiles will not change.
            var touched = new HashSet<(int, int)> { (col, row) };
            for (var dr = -1; dr <= 1; dr++)
            for (var dc = -1; dc <= 1; dc++)
            {
                var nc = col + dc;
                var nr = row + dr;
                if ((dc != 0 || dr != 0) && nc >= 0 && nr >= 0 && nc < width && nr < height &&
                    currentAt(nc, nr) is not null)
                {
                    touched.Add((nc, nr));
                }
            }

            // First attempt: prefer keeping each stale crosser between touched cells, so a paint
            // that CAN preserve a dock or doorway does (minimal diff). When that greedy preference
            // steers a later cell into a dead end - the valid final blend needed both sides to drop
            // or change the crosser together - retry with the preference off, letting the touched
            // set settle on a mutually compatible (typically blank) crosser set. Only if both
            // attempts fail is the paint genuinely impossible here.
            var solved = TrySolvePaint(tileset, width, height, currentAt, col, row, terrain, tileRank,
                             touched, preferStaleCrossers: true)
                         ?? TrySolvePaint(tileset, width, height, currentAt, col, row, terrain, tileRank,
                             touched, preferStaleCrossers: false);
            return solved ?? (IReadOnlyList<TilePaintChange>)Array.Empty<TilePaintChange>();
        }

        /// <summary>
        /// One greedy solve pass over the centre and its ring, or null when any cell has no legal
        /// candidate. A null result rejects the entire pure paint operation: applying only the
        /// centre (or a partially solved ring) would leave mismatched terrain at a shared vertex,
        /// which is an invalid area even though every individual tile id is valid. The caller
        /// applies the result as one transaction, so null is the atomic "cannot be painted here"
        /// outcome (after the retry in <see cref="PaintTerrain(TilesetDefinition,int,int,Func{int,int,PlacedTileState?},int,int,string,Func{int,int}?)"/>).
        /// </summary>
        private static List<TilePaintChange>? TrySolvePaint(
            TilesetDefinition tileset, int width, int height,
            Func<int, int, PlacedTileState?> currentAt,
            int col, int row, string terrain,
            Func<int, int>? tileRank,
            IReadOnlySet<(int, int)> touched,
            bool preferStaleCrossers)
        {
            // A working overlay so each neighbour re-solve sees the freshly painted centre (and any
            // neighbour already re-blended this pass) rather than the stale on-disk grid.
            var overlay = new Dictionary<(int, int), PlacedTileState>();
            PlacedTileState? WorkingAt(int c, int r) =>
                overlay.TryGetValue((c, r), out var v) ? v : currentAt(c, r);

            var changes = new List<TilePaintChange>();

            void Place(int c, int r, TileCandidate chosen)
            {
                var before = WorkingAt(c, r);
                var placed = new PlacedTileState(chosen.TileId, chosen.Orientation, before?.HeightLevel ?? 0);
                overlay[(c, r)] = placed;
                if (before is not { } prev || prev.Candidate != chosen)
                    changes.Add(new TilePaintChange(c, r, chosen.TileId, chosen.Orientation));
            }

            // 1) Centre cell: force every corner to the painted terrain, preferring a crosser-free
            //    (solid) tile so a plain terrain dab never drops a wall/doorway into the fill.
            var centreConstraint = ConstraintFromVertices(tileset, col, row, currentAt, overlay)
                .WithCorner(TileCorner.NorthWest, terrain)
                .WithCorner(TileCorner.NorthEast, terrain)
                .WithCorner(TileCorner.SouthWest, terrain)
                .WithCorner(TileCorner.SouthEast, terrain);
            var centreCandidates = WithMatchingCrossers(
                tileset, SetRuleMatcher.FindMatchingTiles(tileset, centreConstraint),
                col, row, currentAt, overlay, touched, preferStaleCrossers);
            var centre = SelectCandidate(
                tileset, centreCandidates, WorkingAt(col, row)?.Candidate, tileRank, preferBlankEdges: true);
            if (centre is not { } centreChoice)
                return null; // terrain not presentable as a full tile here

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

                var candidates = WithMatchingCrossers(
                    tileset,
                    SetRuleMatcher.FindMatchingTiles(tileset, ConstraintFromVertices(tileset, nc, nr, currentAt, overlay)),
                    nc, nr, currentAt, overlay, touched, preferStaleCrossers);
                var choice = SelectCandidate(
                    tileset, candidates, WorkingAt(nc, nr)?.Candidate, tileRank, preferBlankEdges: false);
                if (choice is { } chosen)
                    Place(nc, nr, chosen);
                else
                    return null;
            }

            return changes;
        }

        /// <summary>
        /// Computes the cells a VERTEX terrain paint would rewrite - the way the reference toolset
        /// paints: the click names a 10m grid vertex, that one vertex's terrain becomes
        /// <paramref name="terrain"/>, and ONLY the (up to) four cells sharing the vertex are
        /// re-solved against it. Verified against Aurora live: painting Gentle Dunes at one ztd01
        /// vertex rewrote exactly the four surrounding cells to the same corner-transition tile at
        /// four orientations, each transition corner facing the painted vertex - no wider ring.
        /// </summary>
        /// <remarks>
        /// Returns an empty list when any touched cell has no legal tile - the whole paint is
        /// refused atomically, and silently, which is also what the reference does (painting a
        /// terrain the tileset cannot blend produces no change and no error). A vertex ranges over
        /// 0..<paramref name="width"/> columns and 0..<paramref name="height"/> rows inclusive;
        /// edge and corner vertices simply touch fewer cells. Cells keep their current tile
        /// whenever it is still legal under the repainted vertex, so re-painting a vertex with its
        /// own terrain is a fixed point.
        /// </remarks>
        public static IReadOnlyList<TilePaintChange> PaintTerrainVertex(
            TilesetDefinition tileset, int width, int height,
            Func<int, int, PlacedTileState?> currentAt,
            int vertexColumn, int vertexRow, string terrain,
            Func<int, int>? tileRank = null)
        {
            ArgumentNullException.ThrowIfNull(tileset);
            ArgumentNullException.ThrowIfNull(currentAt);

            return SolveTerrainVertex(tileset, width, height, currentAt, vertexColumn, vertexRow, terrain, tileRank)
                   ?? (IReadOnlyList<TilePaintChange>)Array.Empty<TilePaintChange>();
        }

        /// <summary>The vertex-paint solve: null when refused, otherwise the (possibly empty) change set.</summary>
        private static List<TilePaintChange>? SolveTerrainVertex(
            TilesetDefinition tileset, int width, int height,
            Func<int, int, PlacedTileState?> currentAt,
            int vertexColumn, int vertexRow, string terrain,
            Func<int, int>? tileRank)
        {
            if (string.IsNullOrWhiteSpace(terrain) ||
                vertexColumn < 0 || vertexRow < 0 || vertexColumn > width || vertexRow > height)
                return null;

            // The four cells sharing vertex (vc, vr), with the corner each presents AT that vertex.
            // Rows grow north (+y), so the cell north-east of the vertex holds it as its SW corner.
            var touched = new (int Col, int Row, TileCorner Corner)[]
            {
                (vertexColumn - 1, vertexRow - 1, TileCorner.NorthEast),
                (vertexColumn, vertexRow - 1, TileCorner.NorthWest),
                (vertexColumn - 1, vertexRow, TileCorner.SouthEast),
                (vertexColumn, vertexRow, TileCorner.SouthWest)
            };

            // Same two-attempt shape as PaintTerrain: edges BETWEEN the touched cells are jointly
            // mutable, so a stale pre-paint crosser between them is a kept-when-possible
            // preference, retried without it when the valid final blend needs both sides to drop
            // or change it together.
            var touchedSet = new HashSet<(int, int)>();
            foreach (var (col, row, _) in touched)
            {
                if (col >= 0 && row >= 0 && col < width && row < height && currentAt(col, row) is not null)
                    touchedSet.Add((col, row));
            }

            return TrySolveVertexPaint(tileset, width, height, currentAt, touched, touchedSet,
                       terrain, tileRank, preferStaleCrossers: true)
                   ?? TrySolveVertexPaint(tileset, width, height, currentAt, touched, touchedSet,
                       terrain, tileRank, preferStaleCrossers: false);
        }

        /// <summary>One greedy vertex-paint pass, or null when any touched cell has no legal tile.</summary>
        private static List<TilePaintChange>? TrySolveVertexPaint(
            TilesetDefinition tileset, int width, int height,
            Func<int, int, PlacedTileState?> currentAt,
            IReadOnlyList<(int Col, int Row, TileCorner Corner)> touched,
            IReadOnlySet<(int, int)> touchedSet,
            string terrain,
            Func<int, int>? tileRank,
            bool preferStaleCrossers)
        {
            var overlay = new Dictionary<(int, int), PlacedTileState>();
            PlacedTileState? WorkingAt(int c, int r) =>
                overlay.TryGetValue((c, r), out var v) ? v : currentAt(c, r);

            var changes = new List<TilePaintChange>();

            foreach (var (col, row, corner) in touched)
            {
                if (col < 0 || row < 0 || col >= width || row >= height)
                    continue;
                if (WorkingAt(col, row) is null)
                    continue; // never fill a cell that has no tile yet

                var constraint = ConstraintFromVertices(tileset, col, row, currentAt, overlay)
                    .WithCorner(corner, terrain);
                var candidates = WithMatchingCrossers(
                    tileset, SetRuleMatcher.FindMatchingTiles(tileset, constraint),
                    col, row, currentAt, overlay, touchedSet, preferStaleCrossers);
                candidates = PreferNoNewCrossers(tileset, candidates, WorkingAt(col, row));
                var choice = SelectCandidate(
                    tileset, candidates, WorkingAt(col, row)?.Candidate, tileRank, preferBlankEdges: false);

                if (choice is not { } chosen)
                    return null; // atomic, silent refusal

                var before = WorkingAt(col, row);
                overlay[(col, row)] = new PlacedTileState(chosen.TileId, chosen.Orientation, before?.HeightLevel ?? 0);
                if (before is not { } prev || prev.Candidate != chosen)
                    changes.Add(new TilePaintChange(col, row, chosen.TileId, chosen.Orientation));
            }

            return changes;
        }

        /// <summary>
        /// Computes the cells a CROSSER paint (road, bridge, wall, ...) would rewrite - the
        /// reference toolset's model, verified against it live: the click names one grid EDGE, and
        /// exactly the (up to) two cells sharing that edge are re-solved so each carries
        /// <paramref name="crosser"/> on the shared edge. Corner terrains are untouched; the other
        /// edges of each cell keep the strict symmetric-crosser rule against their neighbours,
        /// which is what turns repeated dabs into connected runs - painting a second edge of the
        /// same cell re-solves it into the corner/junction piece (measured on ztd01: two road dabs
        /// produced two single-edge stubs and one two-edge corner tile).
        /// </summary>
        /// <remarks>
        /// A VERTICAL edge (<paramref name="verticalEdge"/> true) lies at x = <paramref name="edgeColumn"/>
        /// tiles, between cells (<paramref name="edgeColumn"/>-1, <paramref name="edgeRow"/>) and
        /// (<paramref name="edgeColumn"/>, <paramref name="edgeRow"/>); a horizontal edge lies at
        /// y = <paramref name="edgeRow"/> tiles, between cells (<paramref name="edgeColumn"/>,
        /// <paramref name="edgeRow"/>-1) and (<paramref name="edgeColumn"/>, <paramref name="edgeRow"/>).
        /// A border edge touches one cell, which is how a road runs off the map. Painting a blank
        /// <paramref name="crosser"/> ("") is the eraser: it requires the shared edge to carry
        /// nothing, dissolving a crosser back to plain ground. Refusal is silent and atomic, and a
        /// cell keeps its tile when it already satisfies the paint, so repainting is a fixed point.
        /// </remarks>
        public static IReadOnlyList<TilePaintChange> PaintCrosserEdge(
            TilesetDefinition tileset, int width, int height,
            Func<int, int, PlacedTileState?> currentAt,
            int edgeColumn, int edgeRow, bool verticalEdge, string crosser,
            Func<int, int>? tileRank = null)
        {
            ArgumentNullException.ThrowIfNull(tileset);
            ArgumentNullException.ThrowIfNull(currentAt);
            ArgumentNullException.ThrowIfNull(crosser);

            return SolveCrosserEdge(tileset, width, height, currentAt, edgeColumn, edgeRow, verticalEdge, crosser, tileRank)
                   ?? (IReadOnlyList<TilePaintChange>)Array.Empty<TilePaintChange>();
        }

        /// <summary>
        /// Whether a crosser paint at this edge would be accepted - the answer the paint cursor's
        /// green/red colour shows, distinguished from <see cref="PaintCrosserEdge"/> returning an
        /// empty list, which also happens for an accepted paint that changes nothing (repainting an
        /// existing road is valid and a no-op, and its cursor is green).
        /// </summary>
        public static bool CanPaintCrosserEdge(
            TilesetDefinition tileset, int width, int height,
            Func<int, int, PlacedTileState?> currentAt,
            int edgeColumn, int edgeRow, bool verticalEdge, string crosser,
            Func<int, int>? tileRank = null)
        {
            ArgumentNullException.ThrowIfNull(tileset);
            ArgumentNullException.ThrowIfNull(currentAt);
            ArgumentNullException.ThrowIfNull(crosser);

            return SolveCrosserEdge(tileset, width, height, currentAt, edgeColumn, edgeRow, verticalEdge, crosser, tileRank) != null;
        }

        /// <summary>
        /// The crosser-paint solve: null when refused, otherwise the (possibly empty) change set.
        /// Two attempts, mirroring the vertex brush: the strict pass holds every non-painted edge to
        /// exact symmetry with its neighbour - which is what promotes a stub to a corner piece when
        /// a second edge of the same cell is painted - and the tolerant retry falls back to the
        /// engine's blank-tolerant edge rule, so a legacy one-sided crosser on a neighbouring cell
        /// (the corpus genuinely has them) cannot make a paintable edge unpaintable.
        /// </summary>
        private static List<TilePaintChange>? SolveCrosserEdge(
            TilesetDefinition tileset, int width, int height,
            Func<int, int, PlacedTileState?> currentAt,
            int edgeColumn, int edgeRow, bool verticalEdge, string crosser,
            Func<int, int>? tileRank)
        {
            // Bounds: a vertical edge ranges over columns 0..width and rows 0..height-1; a
            // horizontal edge over columns 0..width-1 and rows 0..height.
            if (verticalEdge
                    ? edgeColumn < 0 || edgeColumn > width || edgeRow < 0 || edgeRow >= height
                    : edgeColumn < 0 || edgeColumn >= width || edgeRow < 0 || edgeRow > height)
                return null;

            // The two cells sharing the edge, with the edge each presents at it.
            var touched = verticalEdge
                ? new (int Col, int Row, TileEdge Edge)[]
                {
                    (edgeColumn - 1, edgeRow, TileEdge.East),
                    (edgeColumn, edgeRow, TileEdge.West)
                }
                : new (int Col, int Row, TileEdge Edge)[]
                {
                    (edgeColumn, edgeRow - 1, TileEdge.North),
                    (edgeColumn, edgeRow, TileEdge.South)
                };

            return TrySolveCrosserPaint(tileset, width, height, currentAt, touched, crosser, tileRank, strictEdges: true)
                   ?? TrySolveCrosserPaint(tileset, width, height, currentAt, touched, crosser, tileRank, strictEdges: false);
        }

        /// <summary>One greedy crosser-paint pass, or null when any touched cell has no legal tile.</summary>
        private static List<TilePaintChange>? TrySolveCrosserPaint(
            TilesetDefinition tileset, int width, int height,
            Func<int, int, PlacedTileState?> currentAt,
            IReadOnlyList<(int Col, int Row, TileEdge Edge)> touched,
            string crosser,
            Func<int, int>? tileRank,
            bool strictEdges)
        {
            var overlay = new Dictionary<(int, int), PlacedTileState>();
            PlacedTileState? WorkingAt(int c, int r) =>
                overlay.TryGetValue((c, r), out var v) ? v : currentAt(c, r);

            var changes = new List<TilePaintChange>();

            foreach (var (col, row, paintedEdge) in touched)
            {
                if (col < 0 || row < 0 || col >= width || row >= height)
                    continue;
                if (WorkingAt(col, row) is null)
                    continue;

                var constraint = ConstraintFromVertices(tileset, col, row, currentAt, overlay);
                var candidates = WithRequiredEdges(
                    tileset, SetRuleMatcher.FindMatchingTiles(tileset, constraint),
                    col, row, currentAt, overlay, paintedEdge, crosser, strictEdges);
                // The painted edge is exempt - it is the crosser being asked for; this only stops
                // the OTHER edges gaining walls the builder did not paint.
                candidates = PreferNoNewCrossers(tileset, candidates, WorkingAt(col, row), paintedEdge);
                var choice = SelectCandidate(
                    tileset, candidates, WorkingAt(col, row)?.Candidate, tileRank, preferBlankEdges: false);

                if (choice is not { } chosen)
                    return null; // atomic, silent refusal

                var before = WorkingAt(col, row);
                overlay[(col, row)] = new PlacedTileState(chosen.TileId, chosen.Orientation, before?.HeightLevel ?? 0);
                if (before is not { } prev || prev.Candidate != chosen)
                    changes.Add(new TilePaintChange(col, row, chosen.TileId, chosen.Orientation));
            }

            return changes;
        }

        /// <summary>
        /// Whether a vertex terrain paint would be accepted - the paint cursor's green/red verdict,
        /// distinguished from <see cref="PaintTerrainVertex"/>'s empty list, which an accepted
        /// no-change repaint also returns.
        /// </summary>
        public static bool CanPaintTerrainVertex(
            TilesetDefinition tileset, int width, int height,
            Func<int, int, PlacedTileState?> currentAt,
            int vertexColumn, int vertexRow, string terrain,
            Func<int, int>? tileRank = null)
        {
            ArgumentNullException.ThrowIfNull(tileset);
            ArgumentNullException.ThrowIfNull(currentAt);

            return SolveTerrainVertex(tileset, width, height, currentAt, vertexColumn, vertexRow, terrain, tileRank) != null;
        }

        /// <summary>
        /// <see cref="WithMatchingCrossers"/> with one edge's requirement overridden by the paint:
        /// the painted edge must carry exactly <paramref name="paintedCrosser"/> (blank meaning
        /// "must carry nothing" - the eraser), regardless of what the neighbour across it holds
        /// right now - that neighbour is the other cell of the same paint and is about to agree.
        /// With <paramref name="strictEdges"/> the other edges must mirror their neighbours exactly
        /// (the pass that promotes a stub to a corner when its neighbour now carries the crosser);
        /// without it they follow the engine's blank-tolerant rule, so a legacy one-sided crosser
        /// beside the paint cannot make it unsolvable. Among the survivors, tiles carrying no
        /// crosser on UNCONSTRAINED edges (grid border, empty neighbour) are preferred, so a road
        /// stub never drags a wall off the map with it.
        /// </summary>
        private static IReadOnlyList<TileCandidate> WithRequiredEdges(
            TilesetDefinition tileset, IReadOnlyList<TileCandidate> candidates,
            int col, int row,
            Func<int, int, PlacedTileState?> currentAt,
            IReadOnlyDictionary<(int, int), PlacedTileState> overlay,
            TileEdge paintedEdge, string paintedCrosser, bool strictEdges)
        {
            string? Required(TileEdge edge, int dc, int dr)
            {
                if (edge == paintedEdge)
                    return paintedCrosser;

                var key = (col + dc, row + dr);
                var neighbour = overlay.TryGetValue(key, out var fresh) ? fresh : currentAt(key.Item1, key.Item2);
                if (neighbour is not { } tile || tile.TileId < 0 || tile.TileId >= tileset.Tiles.Count)
                    return null;

                return TileAdjacency.WorldEdgeCrosser(
                    tileset.Tiles[tile.TileId], tile.Orientation, TileAdjacency.OppositeEdge(edge)) ?? string.Empty;
            }

            var requirements = new (TileEdge Edge, string? Crosser)[]
            {
                (TileEdge.North, Required(TileEdge.North, 0, 1)),
                (TileEdge.East, Required(TileEdge.East, 1, 0)),
                (TileEdge.South, Required(TileEdge.South, 0, -1)),
                (TileEdge.West, Required(TileEdge.West, -1, 0))
            };

            bool EdgeIs(TileCandidate candidate, TileEdge edge, string required) => string.Equals(
                TileAdjacency.WorldEdgeCrosser(tileset.Tiles[candidate.TileId], candidate.Orientation, edge) ?? string.Empty,
                required,
                StringComparison.OrdinalIgnoreCase);

            bool EdgeSatisfies(TileCandidate candidate, TileEdge edge, string required) =>
                edge == paintedEdge || strictEdges
                    ? EdgeIs(candidate, edge, required)
                    : TileAdjacency.EdgeCrossersMatch(
                        TileAdjacency.WorldEdgeCrosser(tileset.Tiles[candidate.TileId], candidate.Orientation, edge),
                        required);

            var kept = candidates.Where(candidate => requirements.All(r =>
                r.Crosser == null || EdgeSatisfies(candidate, r.Edge, r.Crosser))).ToList();

            return Narrow(kept, candidate => requirements.All(r =>
                r.Crosser != null || EdgeIs(candidate, r.Edge, string.Empty))).ToList();
        }

        /// <summary>
        /// The crossers this tileset can actually paint: each has at least one tile carrying it on
        /// some edge, in declaration order. For the paint palette, beside <see cref="FillableTerrains"/>.
        /// </summary>
        public static IReadOnlyList<string> PaintableCrossers(TilesetDefinition tileset)
        {
            ArgumentNullException.ThrowIfNull(tileset);

            var carried = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tile in tileset.Tiles)
            {
                foreach (var edge in new[] { tile.Top, tile.Right, tile.Bottom, tile.Left })
                {
                    if (!string.IsNullOrWhiteSpace(edge))
                        carried.Add(edge);
                }
            }

            return tileset.Crossers
                .Select(crosser => crosser.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name) && carried.Contains(name))
                .ToList();
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
            Func<int, int, PlacedTileState?> currentAt,
            IReadOnlyDictionary<(int, int), PlacedTileState> overlay)
        {
            (string Terrain, int Height)? CornerAt(PlacedTileState tile, TileCorner theirCorner)
            {
                if (tile.TileId < 0 || tile.TileId >= tileset.Tiles.Count)
                    return null;

                var definition = tileset.Tiles[tile.TileId];
                return (
                    TileAdjacency.WorldCornerTerrain(definition, tile.Orientation, theirCorner),
                    tile.HeightLevel + TileAdjacency.WorldCornerHeight(definition, tile.Orientation, theirCorner));
            }

            (string Terrain, int Height)? Corner(TileCorner corner)
            {
                (string Terrain, int Height)? stale = null;
                foreach (var (dc, dr, theirCorner) in VertexNeighbours(corner))
                {
                    var key = (col + dc, row + dr);
                    if (overlay.TryGetValue(key, out var fresh))
                        return CornerAt(fresh, theirCorner);

                    if (stale == null && currentAt(key.Item1, key.Item2) is { } placed)
                        stale = CornerAt(placed, theirCorner);
                }

                return stale;
            }

            var northWest = Corner(TileCorner.NorthWest);
            var northEast = Corner(TileCorner.NorthEast);
            var southWest = Corner(TileCorner.SouthWest);
            var southEast = Corner(TileCorner.SouthEast);
            return new TileConstraint
            {
                HeightLevel = currentAt(col, row)?.HeightLevel ?? 0,
                NorthWest = northWest?.Terrain,
                NorthEast = northEast?.Terrain,
                SouthWest = southWest?.Terrain,
                SouthEast = southEast?.Terrain,
                NorthWestHeight = northWest?.Height,
                NorthEastHeight = northEast?.Height,
                SouthWestHeight = southWest?.Height,
                SouthEastHeight = southEast?.Height
            };
        }

        /// <summary>
        /// Keeps only candidates whose edge crossers EXACTLY match those of the already-placed
        /// neighbour on each side (blank included).
        ///
        /// A crosser is a structure that spans a tile boundary - a dock, bridge, corridor, doorway -
        /// so both tiles must declare it or neither does. <see cref="TileAdjacency.EdgeCrossersMatch"/>
        /// is deliberately blank-TOLERANT, because a handful of real corpus boundaries genuinely have
        /// a crosser on one side only, and the corpus validation gate has to accept those. Generation
        /// is held to the stricter rule the corpus overwhelmingly follows: symmetric or nothing
        /// (Corridor 3136 matched vs 0 blank, Dunes/Routes/Slope/Trench/Road/Alley/Bridge 100%
        /// matched, Doorway 93%). Being permissive here produced half a dock jutting into open water
        /// with nothing on the far side.
        ///
        /// The rule is HARD for a neighbour already decided this pass (in the overlay) or outside
        /// the touched set entirely - those tiles will not change, so their crossers are facts. A
        /// neighbour in the touched set but not yet re-solved carries only its stale pre-paint
        /// crosser, and filtering hard against it wrongly refused paints whose valid final blend
        /// needed both touched cells to drop or change the crosser together; those edges instead
        /// become a preference (kept when possible, never emptying the pool), and whichever side
        /// solves first binds the other through the overlay.
        /// </summary>
        private static IReadOnlyList<TileCandidate> WithMatchingCrossers(
            TilesetDefinition tileset, IReadOnlyList<TileCandidate> candidates,
            int col, int row,
            Func<int, int, PlacedTileState?> currentAt,
            IReadOnlyDictionary<(int, int), PlacedTileState> overlay,
            IReadOnlySet<(int, int)>? touched = null,
            bool preferStaleCrossers = true)
        {
            (string? Crosser, bool IsHard) Required(TileEdge edge, int dc, int dr)
            {
                var key = (col + dc, row + dr);
                var isFresh = overlay.TryGetValue(key, out var fresh);
                var neighbour = isFresh ? fresh : currentAt(key.Item1, key.Item2);
                if (neighbour is not { } tile || tile.TileId < 0 || tile.TileId >= tileset.Tiles.Count)
                    return (null, true); // nothing placed there yet - this edge is free

                var crosser = TileAdjacency.WorldEdgeCrosser(
                    tileset.Tiles[tile.TileId], tile.Orientation, TileAdjacency.OppositeEdge(edge)) ?? string.Empty;
                var isHard = isFresh || touched == null || !touched.Contains(key);
                return (crosser, isHard);
            }

            var requirements = new (TileEdge Edge, (string? Crosser, bool IsHard) Requirement)[]
            {
                (TileEdge.North, Required(TileEdge.North, 0, 1)),
                (TileEdge.East, Required(TileEdge.East, 1, 0)),
                (TileEdge.South, Required(TileEdge.South, 0, -1)),
                (TileEdge.West, Required(TileEdge.West, -1, 0))
            };

            bool Matches(TileCandidate candidate, TileEdge edge, string crosser) =>
                string.Equals(
                    TileAdjacency.WorldEdgeCrosser(
                        tileset.Tiles[candidate.TileId], candidate.Orientation, edge) ?? string.Empty,
                    crosser,
                    StringComparison.OrdinalIgnoreCase);

            var kept = candidates.Where(candidate => requirements.All(r =>
                r.Requirement.Crosser == null ||
                !r.Requirement.IsHard ||
                Matches(candidate, r.Edge, r.Requirement.Crosser))).ToList();

            if (preferStaleCrossers && requirements.Any(r => r.Requirement is { Crosser: not null, IsHard: false }))
            {
                return Narrow(kept, candidate => requirements.All(r =>
                    r.Requirement.Crosser == null ||
                    Matches(candidate, r.Edge, r.Requirement.Crosser))).ToList();
            }

            return kept;
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
                pool = Narrow(pool, c => AllEdgesBlank(tileset.Tiles[c.TileId]));

            // Prefer unobstructed ground. A terrain can be satisfied by many tiles that differ only
            // in the scenery built on them - tcn01 has 244 crosser-free all-Cobble tiles, of which
            // id 0 carries a building wall - so without this the fill is chosen by tile id and an
            // area comes out as a field of walls.
            pool = Narrow(pool, c => IsOpenGround(tileset.Tiles[c.TileId]));

            return pool
                .OrderBy(c => tileRank?.Invoke(c.TileId) ?? c.TileId)
                .ThenBy(c => c.TileId)
                .ThenBy(c => c.Orientation)
                .First();
        }

        /// <summary>Applies a preference: narrows the pool only when something actually matches, so a preference never empties it.</summary>
        private static IEnumerable<TileCandidate> Narrow(
            IEnumerable<TileCandidate> pool, Func<TileCandidate, bool> preferred)
        {
            var kept = pool.Where(preferred).ToList();
            return kept.Count > 0 ? kept : pool;
        }

        /// <summary>
        /// Prefers candidates that put no crosser on an edge the cell does not already have one on -
        /// a terrain dab must not build walls, corridors or doorways the builder did not ask for.
        /// </summary>
        /// <remarks>
        /// The edge rule the engine matches with (<see cref="TileAdjacency.EdgeCrossersMatch"/>) is
        /// blank-tolerant, so a tile carrying a wall passes beside a neighbour carrying none - which
        /// is right for validation and wrong for selection: left to the ranking, an interior paint
        /// happily picked wall-and-corridor pieces to satisfy a plain floor corner, and walls
        /// appeared around every dab. Crossers already on the cell are untouched, so a floor painted
        /// beside a corridor still meets it; this only bars NEW ones. A preference, not a filter -
        /// where every legal tile carries a crosser, the cell still solves.
        /// </remarks>
        private static IReadOnlyList<TileCandidate> PreferNoNewCrossers(
            TilesetDefinition tileset, IReadOnlyList<TileCandidate> candidates, PlacedTileState? current,
            TileEdge? exemptEdge = null)
        {
            if (current is not { } placed || placed.TileId < 0 || placed.TileId >= tileset.Tiles.Count)
                return candidates;

            var currentDefinition = tileset.Tiles[placed.TileId];
            var edges = new[] { TileEdge.North, TileEdge.East, TileEdge.South, TileEdge.West };

            return Narrow(candidates, candidate => edges.All(edge =>
            {
                if (edge == exemptEdge)
                    return true; // this edge is the crosser being painted, not an unasked-for one

                var already = TileAdjacency.WorldEdgeCrosser(currentDefinition, placed.Orientation, edge);
                if (!string.IsNullOrEmpty(already))
                    return true; // the cell already carries one here - keeping it is not "new"

                var proposed = TileAdjacency.WorldEdgeCrosser(
                    tileset.Tiles[candidate.TileId], candidate.Orientation, edge);
                return string.IsNullOrEmpty(proposed);
            })).ToList();
        }

        /// <summary>
        /// Whether a tile is fully open ground, from its .set PathNode code. "A" is the open,
        /// unobstructed layout; the other letters describe tiles whose geometry blocks movement
        /// (walls, corners, dead ends). Verified against the corpus: across 422 hand-built areas and
        /// ~99k placed tiles, "A" is 46.7% of everything placed and the dominant tile in 202 areas -
        /// far more than any other code - which is what a plain floor being the bulk of most areas
        /// looks like.
        /// </summary>
        private static bool IsOpenGround(TileDefinition tile) =>
            tile.PathNode.Trim().Equals("A", StringComparison.OrdinalIgnoreCase);

        private static bool AllEdgesBlank(TileDefinition tile) =>
            string.IsNullOrEmpty(tile.Top) && string.IsNullOrEmpty(tile.Right) &&
            string.IsNullOrEmpty(tile.Bottom) && string.IsNullOrEmpty(tile.Left);
    }
}
