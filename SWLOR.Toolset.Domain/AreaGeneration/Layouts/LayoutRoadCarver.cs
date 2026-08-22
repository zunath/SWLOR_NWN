#nullable disable
using System.Collections.Generic;
using System.Linq;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// Shared street-lane post-pass: carves one-cell-wide Road edge-crosser lanes ("Routes" on fcx01)
    /// connecting transition anchors and room centers through open space, closing the gap user feedback
    /// flagged directly -- generated city areas read as "open rooftop platforms, mostly bare tiles...
    /// disconnected road decals crossing empty plazas" because the fcx01 tileset's real street-marking
    /// tile family (TILE207-216, verified via RoadVocabularyCheck) was never wired into any generation
    /// mechanism at all (see BaseGameTilesetProfiles.FutCity's own doc comment / TileCoverageCensusTests'
    /// former PilotAlternateVocabCrossers "Routes" entry) -- hand-built fcx01 areas (pw_ar_narpromena
    /// etc.) instead organize their dense group/kiosk decoration around real carved road networks.
    ///
    /// Runs BEFORE LayoutGroupStamper in MacroLayoutGenerator's post-pass pipeline (reordered from an
    /// earlier "after Stamp" design -- see below), immediately after the height/relief passes settle
    /// final corner terrain. Evidence from 15 hand-built fcx01 areas with a real street network shows
    /// cities are streets-first: 61% of building
    /// groups sit directly on a road edge, and buildings cluster ALONG carved streets rather than
    /// streets being threaded through the gaps between wherever buildings happened to land. Carving
    /// first, from transition anchors and room centers through open space nothing has claimed yet,
    /// reproduces that: a lane runs directly between anchors (nothing is pinned yet -- Stamp is the
    /// only pass that writes PinnedTiles), and LayoutGroupStamper.TryPlaceOpenSetPiece then PREFERS a
    /// site whose footprint fronts one of these already-carved lanes (see
    /// LayoutGroupStamper.IsOpenSetPieceSiteValid's roadAdjacent out param), falling back to any other
    /// valid site when no road-adjacent one exists. Because a road never repaints corner terrain (it
    /// stays this composition's own OpenTerrain the whole time), a stamped footprint could otherwise
    /// silently erase a carved lane's crosser edges (WriteMember rewrites every edge of its own
    /// footprint cells) -- IsOpenSetPieceSiteValid guards this explicitly by rejecting any footprint
    /// cell that already carries a Road edge, so the "a Road cell is never a stamped tile" invariant
    /// (see RoadCarverTests) now holds from the STAMPING side instead of the carving side. Any building
    /// that still lands without touching a road (an unlucky shuffle draw, or a room with no adjacent
    /// lane) gets a short connector spur afterward -- see <see cref="CarveSpurs"/>, which runs
    /// immediately after Stamp in MacroLayoutGenerator.Generate.
    ///
    /// Historical note (why this used to run last): the original design ran this pass AFTER
    /// LayoutGroupStamper and consulted its PinnedTiles as an "occupied by a building" signal so a lane
    /// could route around already-stamped footprints. That produced hand-built-LOOKING streets (routed
    /// between buildings) but not the hand-built CAUSAL structure (buildings fronting streets) the
    /// measured evidence above shows -- and it starved outright once SetPieceRoomCornerFloor-sized
    /// rooms began hosting stamped towers, because most straight anchor-pair runs crossed a pinned
    /// footprint and failed validation (measured on fcx01/Complex at size 20: road-edge share fell to
    /// 0.020 against the hand-built reference's higher share). The BFS-over-Manhattan routing fix below
    /// (<see cref="TryBuildPath"/>) predates this reorder and is kept for the same reason it was added:
    /// other post-passes (LayoutFenceCarver, LayoutElevationPainter/PoolPainter/ReliefPainter,
    /// CorridorStub/CorridorStubChain pins already written earlier in Stamp for Tunnel-mode
    /// compositions) can still leave non-Manhattan-shaped obstacles between two anchors even with
    /// buildings out of the way.
    ///
    /// Never blocks movement (unlike LayoutFenceCarver's Fence edges) -- a road crosser tile is always
    /// pathnode A / fully walkable (verified: fcx01 TILE207-216) -- so this pass needs no walkability
    /// commit/verify/revert dance. Every lane's two endpoints are real anchors (a transition's already-
    /// anchored open Tile, or a room's own CenterTile) and every intermediate cell shares a physical
    /// edge with its neighbor in the chain (EdgeCrosserGrid.SetEdge, the same shared-storage mechanism
    /// LayoutAccentChannelCarver's Bridge span/LayoutFenceCarver's Fence run use), so a committed lane is
    /// by construction one connected path between two real anchors -- no separate connectivity check is
    /// needed the way LayoutFenceCarver's cell-reachability revert is for its (movement-blocking) lines.
    /// Validated fully (bounds, open terrain, unpinned, no conflicting crosser already on a touched edge)
    /// BEFORE any grid mutation, mirroring LayoutFenceCarver.TryBuildChain's own pre-validate shape --
    /// an invalid attempt is simply skipped, never partially written.
    /// </summary>
    internal static class LayoutRoadCarver
    {
        private const int MaxAttempts = 500;

        internal static void CarveRoads(
            MacroLayout layout, MacroLayoutParameters parameters, TilesetModel tileset, System.Random random)
        {
            if (parameters.RoadLanes <= 0) return;
            if (tileset == null) return;

            var road = parameters.RoadCrosser;
            if (string.IsNullOrEmpty(road)) return;

            var open = parameters.OpenTerrain;
            if (string.IsNullOrEmpty(open)) return;

            // Zero-config capability probe, mirroring LayoutAccentChannelCarver.CanCarve/
            // LayoutFenceCarver.CarveFencesForTerrain's own shape-probe-before-carving pattern: a
            // composition that declares RoadCrosser but whose tileset's real inventory is missing one
            // of the five required shapes never carves anything, rather than committing a lane
            // TileResolver could never place a tile for.
            if (!RoadVocabularyCheck.SupportsRoads(tileset, open, road)) return;

            var corners = layout.Corners;
            var crossers = layout.Crossers;
            var width = corners.Width;
            var height = corners.Height;

            // Anchors: every already-anchored transition's open interior tile, plus every room's own
            // representative center tile EXCEPT rooms big enough to plausibly host this composition's
            // smallest configured SetPiece group (see IsBuildingCandidateRoom below).
            //
            // Since roads now carve BEFORE Stamp (see this class's own doc comment), a "primary street"
            // lane blindly targeting every room's center would very often consume that same room's own
            // interior right before Stamp needs it whole for a footprint + 1-tile margin -- measured:
            // with EVERY room center included, fcx01/Complex size-20 group-tile share collapsed to
            // 0.001, a 20x drop from the pre-reorder 0.0217-0.0227 (see RoadAdjacentDecorationTests.
            // Stamp_MultiTileTowers_MeetGroupShareBand). But excluding EVERY room center unconditionally
            // over-corrects the other way: at smaller sizes (e.g. this composition's own width-16
            // decoration-adjacency fixture) rooms are already too small for any SetPiece footprint to
            // fit regardless -- there is no competition there, and losing those anchors starves the
            // street network for no benefit, measured pulling the street-furniture road-adjacency
            // fraction from 0.944-0.945 down to 0.667-0.673 against the 0.70 floor (see
            // RoadAdjacentDecorationTests.Plan_StreetlightKioskDecoration_MeetsRoadAdjacencyBand). Only
            // a room actually large enough to matter is excluded; a room that could never host a
            // building anyway keeps contributing to the street network exactly as before. A room excluded
            // here still gets its own street connection from CarveSpurs after Stamp, which targets the
            // STAMPED BUILDING'S OWN footprint ring rather than blindly claiming the room's center before
            // anything is placed -- the "streets first, buildings front them, spurs are the driveways"
            // shape the task's design calls for. Distinct + deterministically ordered (transitions in
            // their own list order, then eligible rooms by Id) so the same seed always offers the same
            // anchor set in the same order.
            var smallestFootprintArea = SmallestConfiguredFootprintArea(tileset, parameters);

            bool IsBuildingCandidateRoom(LayoutRoom room) =>
                smallestFootprintArea > 0 && room.Tiles.Count >= smallestFootprintArea;

            var anchors = layout.Transitions.Select(t => t.Tile)
                .Concat(layout.Rooms.OrderBy(r => r.Id).Where(r => !IsBuildingCandidateRoom(r)).Select(r => r.CenterTile))
                .Distinct()
                .ToList();

            if (anchors.Count < 2) return;

            // NOTE: an earlier version of this pass also forbade TryBuildPath from crossing any OTHER
            // room besides the current lane's own two endpoints, layered on top of the anchor exclusion
            // above. Measured to have zero effect on the regression the anchor exclusion above actually
            // fixes (group-tile share stayed at 0.001 with or without it), and it broke connectivity
            // outright on layout styles with no free "corridor" cells at all -- PackedRooms (walls-
            // shared rooms joined only by 1-tile door gaps: every cell belongs to SOME room, so two
            // non-adjacent rooms' transitions could never find a legal route once passing through any
            // third room was forbidden) measured 0/10 seeds with any Road edge at size 20, and a Tunnel-
            // mode composition whose two transitions land in non-adjacent rooms hit the same wall.
            // Removed; the anchor exclusion above is what actually matters here.
            var placed = 0;
            var attempts = 0;

            while (placed < parameters.RoadLanes && attempts < MaxAttempts)
            {
                attempts++;

                var i = random.Next(anchors.Count);
                var j = random.Next(anchors.Count);
                if (i == j) continue;

                var a = anchors[i];
                var b = anchors[j];

                if (!TryBuildPath(corners, layout, width, height, open, a, b,
                        parameters.StraightStreetRouting, out var path)) continue;
                if (!IsPathClear(layout, crossers, road, path)) continue;

                CommitPath(crossers, road, path);
                placed++;
            }

            CarvePlazaRingStreets(layout, parameters, road, open);
        }

        /// <summary>
        /// Smallest room tile count that qualifies for a Tunnel-mode plaza perimeter ring street, and
        /// the per-area ring cap. See <see cref="CarvePlazaRingStreets"/>.
        /// </summary>
        private const int PlazaRingMinRoomTiles = 49;
        private const int PlazaRingMaxPerArea = 1;

        /// <summary>
        /// Discrete-room (Halls/Complex-family) road-density gap closer. On a RoomsAndCorridors-style
        /// city composition, nearly every room center is excluded from the anchor pool as a building
        /// candidate (correctly -- see CarveRoads' own anchor note), so the anchor pool degenerates to
        /// the transition tiles and the street network is almost entirely CarveSpurs-grown -- measured
        /// on fcx01 at 32x32 (20 seeds/district, July 2026 city-density pass): futcity_plaza/Complex
        /// road share 0.0855 vs futcity/Packed 0.157 vs hand-built 0.102. (PackedRooms compositions
        /// spur-grow a dense network anyway because their whole grid is one connected open surface;
        /// RoomsAndCorridors' rooms connect only through narrow lanes, so spur growth stays sparse.)
        /// Re-anchoring big-room centers was tried first and reverted: it closed only a third of the
        /// gap (0.0855 -> 0.0918) while the through-plaza lanes consumed stamp sites (futcity group
        /// share fell 0.150 -> 0.124 -- the priority-1 metric).
        ///
        /// Instead, the LARGEST plaza room (per <see cref="PlazaRingMaxPerArea"/>) gets a street ring
        /// around its perimeter tile loop: the outermost room tiles a stamped footprint's own
        /// extended-rectangle requirement could never use as footprint cells anyway (they are the
        /// margin), so a ring adds street density AND road-adjacent stamp-site preference without
        /// consuming a single interior site -- measured: futcity_plaza/Complex road share 0.0855 ->
        /// 0.1016 (hand-built 0.102) with group share within seed noise (0.0704 -> 0.0675), and the
        /// packed pairing byte-identical (non-RoomsAndCorridors styles skip this entirely). Gated on
        /// STYLE, not CorridorMode: fcx01's Complex pairing is tunnel-vocabulary-downgraded to
        /// OpenLane by MacroLayoutGenerator before this pass ever runs, so a CorridorMode gate would
        /// never fire for the exact composition this exists for. Rectangular-loop rooms only
        /// (RoomsAndCorridors rooms are carved as rectangles); a room whose ring fails any cell/edge
        /// validation is skipped whole, never partially carved, matching this class's
        /// pre-validate-then-commit convention.
        /// </summary>
        private static void CarvePlazaRingStreets(
            MacroLayout layout, MacroLayoutParameters parameters, string road, string open)
        {
            if (parameters.Style != DungeonLayoutStyle.RoomsAndCorridors) return;

            var corners = layout.Corners;
            var crossers = layout.Crossers;
            var width = corners.Width;
            var height = corners.Height;
            var placedRings = 0;

            foreach (var room in layout.Rooms
                         .Where(r => !r.IsSetPiece && r.Tiles.Count >= PlazaRingMinRoomTiles)
                         .OrderByDescending(r => r.Tiles.Count)
                         .ThenBy(r => r.Id))
            {
                if (placedRings >= PlazaRingMaxPerArea) break;
                if (!TryBuildRectangularRing(room, layout, corners, width, height, open, out var ring)) continue;

                // Ring edges: consecutive loop cells plus the closing edge. All-or-nothing validation.
                var clear = true;
                for (var i = 0; i < ring.Count && clear; i++)
                {
                    var slot = SlotTowards(ring[i], ring[(i + 1) % ring.Count]);
                    var existing = crossers.GetEdge(ring[i].X, ring[i].Y, slot);
                    if (existing.Length != 0 && !string.Equals(existing, road, System.StringComparison.OrdinalIgnoreCase))
                        clear = false;
                }

                if (!clear) continue;

                for (var i = 0; i < ring.Count; i++)
                {
                    var slot = SlotTowards(ring[i], ring[(i + 1) % ring.Count]);
                    crossers.SetEdge(ring[i].X, ring[i].Y, slot, road);
                }

                placedRings++;
            }
        }

        /// <summary>
        /// Builds a room's perimeter tile loop in walk order (south edge west-to-east, east edge
        /// south-to-north, north edge east-to-west, west edge north-to-south). Fails unless the room
        /// is a full unclipped rectangle of fully-open, unpinned tiles at least 3x3 (so the loop is a
        /// real cycle with an interior) -- Tunnel-mode rooms are carved as rectangles, and anything
        /// already clipped (an earlier pass painted/pinned into it) is skipped rather than half-rung.
        /// </summary>
        private static bool TryBuildRectangularRing(
            LayoutRoom room, MacroLayout layout, CornerTerrainGrid corners, int width, int height, string open,
            out List<(int X, int Y)> ring)
        {
            ring = null;

            var minX = int.MaxValue; var maxX = int.MinValue;
            var minY = int.MaxValue; var maxY = int.MinValue;
            foreach (var (x, y) in room.Tiles)
            {
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            var spanX = maxX - minX + 1;
            var spanY = maxY - minY + 1;
            if (spanX < 3 || spanY < 3) return false;
            if (spanX * spanY != room.Tiles.Count) return false; // clipped/non-rectangular

            var tileSet = new HashSet<(int X, int Y)>(room.Tiles);
            var loop = new List<(int X, int Y)>();
            for (var x = minX; x <= maxX; x++) loop.Add((x, minY));
            for (var y = minY + 1; y <= maxY; y++) loop.Add((maxX, y));
            for (var x = maxX - 1; x >= minX; x--) loop.Add((x, maxY));
            for (var y = maxY - 1; y >= minY + 1; y--) loop.Add((minX, y));

            foreach (var cell in loop)
            {
                if (!tileSet.Contains(cell)) return false;
                if (!InBounds(cell, width, height)) return false;
                if (layout.PinnedTiles.ContainsKey(cell)) return false;
                if (!LayoutCornerUtils.IsTileFullyOpen(corners, cell.X, cell.Y, open)) return false;
            }

            ring = loop;
            return true;
        }

        /// <summary>
        /// Smallest (Rows+2)*(Columns+2) -- footprint plus the 1-tile margin ring
        /// IsOpenSetPieceSiteValid's own "extended" rectangle requires -- among this composition's
        /// configured SetPiece groups that actually exist in the real tileset data. 0 when no
        /// configured group resolves (no SetPieces at all, or every configured name is unknown to this
        /// tileset) -- callers must treat 0 as "no room is ever a building candidate," matching the
        /// pre-SetPieces-configured behavior (every room center stays a road anchor).
        /// </summary>
        private static int SmallestConfiguredFootprintArea(TilesetModel tileset, MacroLayoutParameters parameters)
        {
            var smallest = 0;
            if (tileset == null || parameters.SetPieces == null) return smallest;

            foreach (var groupName in parameters.SetPieces.Keys)
            {
                TileGroupRecord match = null;
                foreach (var candidate in tileset.Groups)
                {
                    if (string.Equals(candidate.Name, groupName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        match = candidate;
                        break;
                    }
                }
                if (match == null || match.Rows <= 0 || match.Columns <= 0) continue;

                var area = (match.Rows + 2) * (match.Columns + 2);
                if (smallest == 0 || area < smallest) smallest = area;
            }

            return smallest;
        }

        /// <summary>
        /// Neighbor step order for the searches below: Right, Left, Top, Bottom -- fixed so the search
        /// is fully deterministic given the same grid state and anchor pair (the only randomness in
        /// this pass is CarveRoads' anchor-pair draw).
        /// </summary>
        private static readonly (int Dx, int Dy)[] StepOrder = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        /// <summary>
        /// Builds a SHORTEST, then FEWEST-TURNS connected chain of cells from <paramref name="a"/> to
        /// <paramref name="b"/> over cells that are in-bounds, fully open (a road never repaints
        /// terrain -- see the class doc comment), and unpinned -- so a lane threads BETWEEN stamped
        /// building footprints the way hand-built fcx01 streets do, instead of demanding one clear
        /// right-angle Manhattan run. (The original Manhattan-only construction starved once
        /// SetPieceRoomCornerFloor-sized rooms began hosting stamped towers: measured on fcx01/Complex
        /// at size 20 with buildings stamping, road-edge share fell to 0.020 against the hand-built
        /// fcx01 reference's 0.102, because most straight anchor-pair runs crossed a pinned footprint
        /// and failed validation.)
        ///
        /// FEWEST TURNS among the shortest paths (street-coherence pass, gated on
        /// <see cref="MacroLayoutParameters.StraightStreetRouting"/>): the legacy plain BFS commits
        /// whichever shortest path its expansion order happens to reach first, which on an
        /// open plaza is a diagonal STAIRCASE of one-cell zigzags -- measured turn-tile share on the
        /// user-reviewed delivered city areas ran 16-29% of road cells against the hand-built city
        /// band's 0-15% (r17_road_audit.py), and every staircase corner renders a bent road tile, the
        /// reviewed "no coherent street network readable from above". Minimizing direction changes as
        /// a secondary cost (path LENGTH stays primary, so lane cell counts -- and every road-share
        /// band mined on them -- are unchanged) commits the straight avenue + single L-corner shape
        /// hand-built streets use. Deterministic: states expand in a fixed heap order keyed
        /// (distance, turns, insertion sequence) with neighbors offered in <see cref="StepOrder"/>,
        /// and a shortest path never revisits a cell, so the committed chain is simple -- each cell
        /// carries at most 2 of this lane's own edges; junction shapes still only arise where two
        /// DIFFERENT lanes legitimately meet, exactly as before. Compositions that never declare
        /// StraightStreetRouting keep the legacy BFS geometry byte-for-byte (non-city road
        /// tilesets -- forest/desert trails -- are deliberately untouched by the city
        /// street-coherence pass). Fails when either anchor is out of bounds, not fully
        /// open/unpinned, or no connected open route exists.
        ///
        /// Not room-scoped (see CarveRoads' own note on a room-membership restriction tried and
        /// reverted here): protecting building-candidate rooms from road through-traffic is handled
        /// entirely by CarveRoads' anchor selection (excluding building-candidate room centers from the
        /// anchor pool) plus LayoutGroupStamper.IsOpenSetPieceSiteValid's own footprint/margin rejection
        /// of any cell that already carries a Road edge -- a per-cell room-ownership gate here was
        /// measured to add nothing on top of that and broke connectivity outright on layout styles with
        /// no free "corridor" cells (PackedRooms).
        /// </summary>
        private static bool TryBuildPath(
            CornerTerrainGrid corners, MacroLayout layout, int width, int height, string open,
            (int X, int Y) a, (int X, int Y) b, bool straightRouting, out List<(int X, int Y)> path)
        {
            path = null;
            if (a == b) return false;
            if (!InBounds(a, width, height) || !InBounds(b, width, height)) return false;

            bool IsTraversable((int X, int Y) cell) =>
                InBounds(cell, width, height) &&
                !layout.PinnedTiles.ContainsKey(cell) &&
                LayoutCornerUtils.IsTileFullyOpen(corners, cell.X, cell.Y, open);

            if (!IsTraversable(a) || !IsTraversable(b)) return false;

            if (straightRouting)
                return TryMinTurnSearch(IsTraversable, new List<(int X, int Y)> { a },
                    cell => cell == b, out path) && path.Count >= 2;

            // Legacy first-found BFS (kept verbatim for every composition that never declares
            // StraightStreetRouting -- their lane geometry stays byte-identical).
            var cameFrom = new Dictionary<(int X, int Y), (int X, int Y)> { [a] = a };
            var queue = new Queue<(int X, int Y)>();
            queue.Enqueue(a);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == b) break;

                foreach (var (dx, dy) in StepOrder)
                {
                    var next = (X: current.X + dx, Y: current.Y + dy);
                    if (cameFrom.ContainsKey(next) || !IsTraversable(next)) continue;
                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }

            if (!cameFrom.ContainsKey(b)) return false;

            var chain = new List<(int X, int Y)>();
            for (var cell = b; cell != a; cell = cameFrom[cell])
                chain.Add(cell);
            chain.Add(a);
            chain.Reverse();

            if (chain.Count < 2) return false;

            path = chain;
            return true;
        }

        /// <summary>
        /// Shared shortest-then-fewest-turns search core for <see cref="TryBuildPath"/> (single
        /// start, fixed goal cell) and <see cref="TryBuildNearestRoadPath"/> (multi-source, first
        /// goal-predicate hit). State space is (cell, incoming direction); cost is lexicographic
        /// (steps, direction changes) with a monotonically increasing insertion sequence as the
        /// final tie-break, so expansion order -- and therefore the committed chain -- is fully
        /// deterministic for a given grid state and start list. Start cells enter with no incoming
        /// direction (the first step costs no turn).
        /// </summary>
        private static bool TryMinTurnSearch(
            System.Func<(int X, int Y), bool> isTraversable, List<(int X, int Y)> starts,
            System.Func<(int X, int Y), bool> isGoal, out List<(int X, int Y)> path)
        {
            path = null;

            // State key: (cell, dirIndex); dirIndex 4 = "no incoming direction" (a start state).
            var best = new Dictionary<((int X, int Y) Cell, int Dir), (int Dist, int Turns)>();
            var parent = new Dictionary<((int X, int Y) Cell, int Dir), ((int X, int Y) Cell, int Dir)>();
            var heap = new SortedSet<(int Dist, int Turns, long Seq, (int X, int Y) Cell, int Dir)>();
            long seq = 0;

            foreach (var s in starts)
            {
                var key = (s, 4);
                if (best.ContainsKey(key)) continue;
                best[key] = (0, 0);
                heap.Add((0, 0, seq++, s, 4));
            }

            while (heap.Count > 0)
            {
                var state = heap.Min;
                heap.Remove(state);
                var (dist, turns, _, cell, dir) = state;
                if (best[(cell, dir)] != (dist, turns)) continue; // stale entry

                if (isGoal(cell))
                {
                    var chain = new List<(int X, int Y)>();
                    var cursor = (Cell: cell, Dir: dir);
                    while (true)
                    {
                        chain.Add(cursor.Cell);
                        if (!parent.TryGetValue(cursor, out var up)) break;
                        cursor = up;
                    }

                    chain.Reverse();
                    path = chain;
                    return true;
                }

                for (var i = 0; i < StepOrder.Length; i++)
                {
                    var (dx, dy) = StepOrder[i];
                    var next = (X: cell.X + dx, Y: cell.Y + dy);
                    if (!isTraversable(next)) continue;

                    var nextTurns = turns + (dir != 4 && dir != i ? 1 : 0);
                    var nextKey = (next, i);
                    if (best.TryGetValue(nextKey, out var seen) &&
                        (seen.Dist < dist + 1 || (seen.Dist == dist + 1 && seen.Turns <= nextTurns)))
                        continue;

                    best[nextKey] = (dist + 1, nextTurns);
                    parent[nextKey] = (cell, dir);
                    heap.Add((dist + 1, nextTurns, seq++, next, i));
                }
            }

            return false;
        }

        private static bool InBounds((int X, int Y) cell, int width, int height) =>
            cell.X >= 0 && cell.Y >= 0 && cell.X < width && cell.Y < height;

        /// <summary>
        /// True when every cell in <paramref name="path"/> is unpinned (not already claimed by a
        /// LayoutGroupStamper set piece / GroupExitPlanner exit -- exit pins don't exist yet at this
        /// point in the pipeline, but a defensive check costs nothing) and every edge the path will
        /// write is either blank or already carries this same road crosser (a legitimate T/X junction
        /// with an earlier lane), never a different crosser some other post-pass already claimed.
        /// </summary>
        private static bool IsPathClear(
            MacroLayout layout, EdgeCrosserGrid crossers, string road, List<(int X, int Y)> path)
        {
            foreach (var cell in path)
            {
                if (layout.PinnedTiles.ContainsKey(cell)) return false;
            }

            for (var i = 0; i + 1 < path.Count; i++)
            {
                var slot = SlotTowards(path[i], path[i + 1]);
                var existing = crossers.GetEdge(path[i].X, path[i].Y, slot);
                if (existing.Length != 0 && !string.Equals(existing, road, System.StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static void CommitPath(EdgeCrosserGrid crossers, string road, List<(int X, int Y)> path)
        {
            for (var i = 0; i + 1 < path.Count; i++)
            {
                var slot = SlotTowards(path[i], path[i + 1]);
                crossers.SetEdge(path[i].X, path[i].Y, slot, road);
            }
        }

        /// <summary>The EdgeSlot on cell <paramref name="from"/> facing its path-adjacent neighbor
        /// <paramref name="to"/> (always exactly one grid step away, by construction of TryBuildPath).</summary>
        private static int SlotTowards((int X, int Y) from, (int X, int Y) to)
        {
            if (to.X == from.X + 1) return EdgeSlot.Right;
            if (to.X == from.X - 1) return EdgeSlot.Left;
            if (to.Y == from.Y + 1) return EdgeSlot.Top;
            return EdgeSlot.Bottom;
        }

        // ---------------- CarveSpurs (post-Stamp fallback frontage connector) ----------------

        /// <summary>
        /// Runs immediately after LayoutGroupStamper.Stamp (see MacroLayoutGenerator.Generate's
        /// ordering): for every stamped OpenSetPiece building footprint (see
        /// MacroLayout.StampedOpenSetPieceFootprints) whose 1-cell margin doesn't already touch a
        /// carved Road edge, carves a short connector spur from the footprint's own margin to the
        /// nearest reachable Road cell, using the same crosser-chain mechanism as CarveRoads. Most
        /// buildings never need this -- TryPlaceOpenSetPiece already PREFERS a road-adjacent site when
        /// one is available (see LayoutGroupStamper.IsOpenSetPieceSiteValid) -- this is the fallback for
        /// the rest (no road-adjacent site existed for that group, or the room has no nearby lane).
        /// Best-effort like every other post-pass here: a footprint with no open, unpinned path to any
        /// Road cell (e.g. a fully enclosed WallRoom-only room) is silently left unconnected rather than
        /// failing generation.
        /// </summary>
        internal static void CarveSpurs(MacroLayout layout, MacroLayoutParameters parameters, TilesetModel tileset, System.Random random)
        {
            if (tileset == null) return;
            if (layout.StampedOpenSetPieceFootprints.Count == 0) return;

            var road = parameters.RoadCrosser;
            if (string.IsNullOrEmpty(road)) return;

            var open = parameters.OpenTerrain;
            if (string.IsNullOrEmpty(open)) return;

            if (!RoadVocabularyCheck.SupportsRoads(tileset, open, road)) return;

            var corners = layout.Corners;
            var crossers = layout.Crossers;
            var width = corners.Width;
            var height = corners.Height;

            foreach (var footprint in layout.StampedOpenSetPieceFootprints)
            {
                var footprintSet = new HashSet<(int X, int Y)>(footprint);
                var ring = BuildRing(footprintSet, width, height);
                if (ring.Count == 0) continue;

                // Already fronted (either TryPlaceOpenSetPiece's own preference found a road-adjacent
                // site, or an earlier spur in this same pass connected a neighboring footprint's road
                // right up against this one) -- nothing to do.
                if (ring.Any(cell => HasRoadEdge(crossers, cell, road))) continue;

                var starts = ring.Where(cell => IsSpurTraversable(layout, corners, open, cell, width, height))
                    .OrderBy(c => c.Y).ThenBy(c => c.X)
                    .ToList();
                if (starts.Count == 0) continue;

                if (!TryBuildNearestRoadPath(corners, layout, crossers, width, height, open, road, starts,
                        parameters.StraightStreetRouting, out var path))
                    continue;

                if (!IsPathClear(layout, crossers, road, path))
                    continue;

                CommitPath(crossers, road, path);
            }
        }

        /// <summary>In-bounds cells directly outside <paramref name="footprint"/> (4-connected) -- the
        /// footprint's own street frontage.</summary>
        private static List<(int X, int Y)> BuildRing(HashSet<(int X, int Y)> footprint, int width, int height)
        {
            var ring = new HashSet<(int X, int Y)>();
            foreach (var cell in footprint)
            {
                foreach (var (dx, dy) in StepOrder)
                {
                    var neighbor = (X: cell.X + dx, Y: cell.Y + dy);
                    if (footprint.Contains(neighbor)) continue;
                    if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= width || neighbor.Y >= height) continue;
                    ring.Add(neighbor);
                }
            }
            return ring.ToList();
        }

        private static bool HasRoadEdge(EdgeCrosserGrid crossers, (int X, int Y) cell, string road)
        {
            for (var slot = 0; slot < 4; slot++)
            {
                if (string.Equals(crossers.GetEdge(cell.X, cell.Y, slot), road, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>Same traversability rule TryBuildPath uses (in-bounds, fully open, unpinned) --
        /// a spur must never cross another building's footprint (pinned by Stamp) or repaint terrain.</summary>
        private static bool IsSpurTraversable(MacroLayout layout, CornerTerrainGrid corners, string open, (int X, int Y) cell, int width, int height)
        {
            return InBounds(cell, width, height) &&
                   !layout.PinnedTiles.ContainsKey(cell) &&
                   LayoutCornerUtils.IsTileFullyOpen(corners, cell.X, cell.Y, open);
        }

        /// <summary>
        /// Multi-source search from any of <paramref name="starts"/> to the nearest traversable
        /// cell already carrying a Road edge. Under
        /// <see cref="MacroLayoutParameters.StraightStreetRouting"/> it is the same
        /// shortest-then-fewest-turns core TryBuildPath uses (a spur is a short straight driveway,
        /// never a staircase); otherwise the legacy multi-source BFS, kept verbatim so
        /// non-declaring compositions stay byte-identical. Deterministic given the grid state:
        /// <paramref name="starts"/> is pre-sorted by the caller so the seed order never depends
        /// on HashSet enumeration order.
        /// </summary>
        private static bool TryBuildNearestRoadPath(
            CornerTerrainGrid corners, MacroLayout layout, EdgeCrosserGrid crossers, int width, int height,
            string open, string road, List<(int X, int Y)> starts, bool straightRouting,
            out List<(int X, int Y)> path)
        {
            if (straightRouting)
            {
                if (!TryMinTurnSearch(
                        cell => IsSpurTraversable(layout, corners, open, cell, width, height),
                        starts,
                        cell => HasRoadEdge(crossers, cell, road),
                        out path))
                    return false;

                // Defensive: a start cell is never a road cell itself (guarded by the ring check
                // in CarveSpurs), so a real spur always has at least 2 cells.
                return path.Count >= 2;
            }

            path = null;

            var cameFrom = new Dictionary<(int X, int Y), (int X, int Y)>();
            var queue = new Queue<(int X, int Y)>();
            foreach (var s in starts)
            {
                if (cameFrom.ContainsKey(s)) continue;
                cameFrom[s] = s;
                queue.Enqueue(s);
            }

            (int X, int Y)? found = null;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (HasRoadEdge(crossers, current, road)) { found = current; break; }

                foreach (var (dx, dy) in StepOrder)
                {
                    var next = (X: current.X + dx, Y: current.Y + dy);
                    if (cameFrom.ContainsKey(next)) continue;
                    if (!IsSpurTraversable(layout, corners, open, next, width, height)) continue;
                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }

            if (!found.HasValue) return false;

            var chain = new List<(int X, int Y)>();
            var cell = found.Value;
            while (cameFrom[cell] != cell)
            {
                chain.Add(cell);
                cell = cameFrom[cell];
            }
            chain.Add(cell);
            chain.Reverse();

            if (chain.Count < 2) return false; // defensive: a start cell is never a road cell itself (guarded by the ring check in CarveSpurs)

            path = chain;
            return true;
        }
    }
}
