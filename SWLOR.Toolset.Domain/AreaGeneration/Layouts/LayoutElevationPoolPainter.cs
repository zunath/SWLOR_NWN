#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// Shared post-pass: paints small "depth pools" of a second, hazard/liquid terrain (e.g. tde01's
    /// Lava) strictly inside a room's own OpenTerrain interior, one story below the room's own grade.
    /// This is the two-terrain sibling of <see cref="LayoutElevationPainter"/>'s OpenTerrain
    /// "split-level" blob: it reuses that class's exact verified rectangle/rim/CellResolves machinery
    /// (<see cref="LayoutElevationPainter.TryPlaceRectangle"/>) to raise a Floor rim by one story, then
    /// overwrites a smaller interior sub-rectangle with the pool terrain at the room's ORIGINAL
    /// (unraised) height -- so the pool reads as sunk below its own raised bank.
    ///
    /// Probe evidence (tde01/Dungeon): a Floor rim corner one story above an adjacent Lava corner is
    /// real, well-populated vocabulary -- every rotation of both boundary shapes this pass ever
    /// produces (a single pool corner cut into a Floor rim; two ADJACENT pool corners cut along a
    /// straight rim edge) resolves against the real tileset (see LayoutElevationPoolPainterTests'
    /// reconstruction), as does a fully-interior flat pool cell. Every candidate this pass paints is
    /// re-verified live via TileResolver.HasHeightAwareCandidate before committing -- exactly
    /// LayoutElevationPainter.CellResolves' own convention -- so an unsupported (terrain, height-delta)
    /// combination is never assumed, only ever actually placed after the tileset itself confirms it.
    ///
    /// Runs immediately after LayoutElevationPainter (see MacroLayoutGenerator.Generate) and before
    /// LayoutGroupStamper, for the same "final heights/terrain must be settled before the stamper's
    /// flat-cell guards run" reason LayoutElevationPainter itself documents.
    /// </summary>
    internal static class LayoutElevationPoolPainter
    {
        private const int RaiseDelta = 1;

        // Outer span is in TILES (rim + pool interior + rim, before the 1-corner rim inset on each
        // side); the pool's own interior therefore spans [MinOuterSpan-2 .. MaxOuterSpan-2] tiles.
        // MinOuterSpan=3 is the smallest rectangle that can host a 1-tile pool with a full 1-tile Floor
        // rim on every side.
        /// <summary>
        /// Smallest outer footprint (rim + a 1-tile pool interior + rim) any pool needs, in TILES.
        /// Exposed so DungeonComposition.BuildLayoutParameters can floor a composition's room-size
        /// ceiling high enough to host at least one pool once PoolRegions is requested -- mirroring
        /// how it floors CorridorWidth against Tileset.MinimumOpeningWidth. A room strictly smaller
        /// than MinOuterSpan+2 tiles on some axis can never host a pool at all (TryPaintPool's own
        /// room-scoped margin requirement), regardless of how many attempts are spent.
        /// </summary>
        internal const int MinOuterSpan = 3;
        private const int MaxOuterSpan = 5;
        // Kept smaller than LayoutElevationPainter's own budgets (8/80): each TryPlacePool attempt is
        // strictly more expensive than a plain elevation-blob attempt (a full outer-rectangle
        // TryPlaceRectangle verification PLUS a second interior-boundary CellResolves sweep), and
        // TileResolver.HasHeightAwareCandidate rebuilds its whole candidate lookup fresh on every call
        // -- so a hopeless area (no room ever big enough, see DungeonComposition.BuildLayoutParameters'
        // MaxRoomCornerSize floor) must fail its budget quickly rather than compounding into a
        // multi-second-per-seed cost.
        private const int RoomPlacementAttempts = 4;
        private const int MaxAttemptsPerRegion = 20;

        internal static void Paint(MacroLayout layout, MacroLayoutParameters parameters, TilesetModel tileset, System.Random random)
        {
            if (tileset == null) return;
            if (parameters.PoolRegions <= 0) return;
            if (string.IsNullOrEmpty(parameters.PoolTerrain)) return;
            if (string.IsNullOrEmpty(parameters.OpenTerrain)) return;

            if (!HasPoolVocabulary(tileset, parameters.OpenTerrain, parameters.PoolTerrain)) return;

            // Built ONCE per Paint() call -- see TileResolver.HeightAwareProbeCache's own doc comment.
            // TryPaintIrregularPoolInterior issues one cache probe per grown interior corner, so reusing
            // a single cache here (instead of each CellResolves call rebuilding its own lookup) keeps a
            // multi-region pool pass from recomputing the same tileset-wide lookup dozens of times.
            var cache = TileResolver.BuildHeightAwareProbeCache(tileset);

            var forbidden = BuildForbiddenCorners(layout);
            var touchedThisPass = new HashSet<(int X, int Y)>();

            var painted = 0;
            var attempts = 0;
            var maxAttempts = parameters.PoolRegions * MaxAttemptsPerRegion;

            while (painted < parameters.PoolRegions && attempts < maxAttempts)
            {
                attempts++;
                if (TryPaintPool(layout, parameters, cache, forbidden, touchedThisPass, random))
                    painted++;
            }

            if (painted > 0)
                LayoutCornerUtils.RecomputeFullyOpenRoomTiles(layout, parameters.OpenTerrain);
        }

        /// <summary>Same border/transition-anchor exclusion as LayoutElevationPainter -- pools never touch
        /// the map border ring or a transition anchor's own corners.</summary>
        private static HashSet<(int X, int Y)> BuildForbiddenCorners(MacroLayout layout)
        {
            var corners = layout.Corners;
            var forbidden = new HashSet<(int X, int Y)>();

            for (var x = 0; x <= corners.Width; x++)
            {
                forbidden.Add((x, 0));
                forbidden.Add((x, corners.Height));
            }
            for (var y = 0; y <= corners.Height; y++)
            {
                forbidden.Add((0, y));
                forbidden.Add((corners.Width, y));
            }

            foreach (var transition in layout.Transitions)
            {
                var (tx, ty) = transition.Tile;
                forbidden.Add((tx, ty));
                forbidden.Add((tx + 1, ty));
                forbidden.Add((tx, ty + 1));
                forbidden.Add((tx + 1, ty + 1));
            }

            return forbidden;
        }

        /// <summary>
        /// True when the tileset has, at minimum: a fully-interior flat pool cell (all 4 corners
        /// PoolTerrain, flat), and at least one rotation each of the "corner" (one pool corner cut into
        /// a raised Floor rim) and "edge" (two adjacent pool corners) boundary shapes this pass's
        /// rectangle mechanism can produce. Mirrors LayoutElevationPainter.HasRimVocabulary's own
        /// representative-shape convention -- the authoritative check is always the live per-cell
        /// TileResolver.HasHeightAwareCandidate probe in TryPlacePool, this is only a cheap upfront
        /// short-circuit.
        /// </summary>
        private static bool HasPoolVocabulary(TilesetModel tileset, string openTerrain, string poolTerrain)
        {
            var fullyPool = TileResolver.HasHeightAwareCandidate(
                tileset, poolTerrain, poolTerrain, poolTerrain, poolTerrain, "", "", "", "", 0, 0, 0, 0);
            if (!fullyPool) return false;

            var corner = TileResolver.HasHeightAwareCandidate(
                tileset, openTerrain, openTerrain, openTerrain, poolTerrain, "", "", "", "",
                RaiseDelta, RaiseDelta, RaiseDelta, 0);

            var edge = TileResolver.HasHeightAwareCandidate(
                tileset, openTerrain, openTerrain, poolTerrain, poolTerrain, "", "", "", "",
                RaiseDelta, RaiseDelta, 0, 0);

            return corner && edge;
        }

        private static bool TryPaintPool(
            MacroLayout layout, MacroLayoutParameters parameters, TileResolver.HeightAwareProbeCache cache,
            HashSet<(int X, int Y)> forbidden, HashSet<(int X, int Y)> touchedThisPass, System.Random random)
        {
            var openTerrain = parameters.OpenTerrain;

            var roomOrder = new int[layout.Rooms.Count];
            for (var i = 0; i < roomOrder.Length; i++) roomOrder[i] = i;
            for (var i = roomOrder.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (roomOrder[i], roomOrder[j]) = (roomOrder[j], roomOrder[i]);
            }

            foreach (var roomIndex in roomOrder)
            {
                var room = layout.Rooms[roomIndex];
                if (room.Tiles.Count == 0) continue;
                if (!string.Equals(room.OpenTerrain, openTerrain, System.StringComparison.Ordinal)) continue;

                int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
                foreach (var (tx, ty) in room.Tiles)
                {
                    if (tx < minX) minX = tx;
                    if (tx > maxX) maxX = tx;
                    if (ty < minY) minY = ty;
                    if (ty > maxY) maxY = ty;
                }

                var rx0 = minX;
                var rx1 = maxX + 1;
                var ry0 = minY;
                var ry1 = maxY + 1;

                for (var placementAttempt = 0; placementAttempt < RoomPlacementAttempts; placementAttempt++)
                {
                    var spanX = random.Next(MinOuterSpan, MaxOuterSpan + 1);
                    var spanY = random.Next(MinOuterSpan, MaxOuterSpan + 1);

                    var minX0 = rx0 + 1;
                    var maxX0 = rx1 - 1 - spanX;
                    var minY0 = ry0 + 1;
                    var maxY0 = ry1 - 1 - spanY;
                    if (maxX0 < minX0 || maxY0 < minY0) continue; // room too small for this span

                    var x0 = random.Next(minX0, maxX0 + 1);
                    var y0 = random.Next(minY0, maxY0 + 1);

                    if (TryPlacePool(layout, parameters, cache, forbidden, touchedThisPass, rx0, ry0, rx1, ry1, x0, y0, x0 + spanX, y0 + spanY, random))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Raises the outer rectangle [x0..x1] x [y0..y1] as a plain OpenTerrain elevation blob (reusing
        /// LayoutElevationPainter.TryPlaceRectangle verbatim -- identical padding/forbidden/CellResolves
        /// verification to the existing, already-shipped mechanism), then overwrites the interior
        /// sub-rectangle (inset by exactly 1 corner on every side) with PoolTerrain at height 0, and
        /// re-verifies every cell touching that interior boundary plus THIS ROOM's own open-corner
        /// connectivity before keeping the result. Reverts BOTH the interior overwrite AND the outer
        /// raise together on any failure, so a failed pool attempt never leaves a stray plain elevation
        /// blob behind.
        ///
        /// Connectivity is checked scoped to the room's own [rx0..rx1] x [ry0..ry1] bounding box
        /// (<see cref="IsRoomStillConnected"/>), NOT LayoutCornerUtils.IsSingleComponent over the WHOLE
        /// layout: in Tunnel corridor mode (this pass's real pairing, BaseGameTilesetProfiles.Dungeon x
        /// StandardLayoutProfiles.Complex), every room is already its own permanently-separate island in
        /// the shared-terrain-label graph -- corridors carve solid cells with Corridor/Doorway edge
        /// crossers, never Floor-labeled corners, so two different rooms' Floor territories are NEVER
        /// mutually reachable via label alone regardless of any pool (the whole-layout graph is only
        /// unified via layout.TunnelLinks, checked separately by MacroLayoutGenerator.ValidateInvariants
        /// at the very end). Calling the whole-layout IsSingleComponent here would therefore reject
        /// almost every real Tunnel-mode pool attempt for a reason that has nothing to do with the pool
        /// itself -- empirically confirmed offline: every candidate that passed CellResolves still failed
        /// this exact way before the fix, and once scoped correctly to the room's own bounding box (which
        /// the pool's full 1-tile Floor rim can never disconnect from the room's remaining floor), real
        /// Dungeon/Complex pools place successfully.
        /// </summary>
        private static bool TryPlacePool(
            MacroLayout layout, MacroLayoutParameters parameters, TileResolver.HeightAwareProbeCache cache,
            HashSet<(int X, int Y)> forbidden, HashSet<(int X, int Y)> touchedThisPass,
            int rx0, int ry0, int rx1, int ry1,
            int x0, int y0, int x1, int y1, System.Random random)
        {
            var openTerrain = parameters.OpenTerrain;
            var poolTerrain = parameters.PoolTerrain;
            var corners = layout.Corners;

            if (!LayoutElevationPainter.TryPlaceRectangle(layout, cache, openTerrain, forbidden, touchedThisPass, x0, y0, x1, y1))
                return false;

            var ix0 = x0 + 1;
            var ix1 = x1 - 1;
            var iy0 = y0 + 1;
            var iy1 = y1 - 1;

            if (ix1 <= ix0 || iy1 <= iy0)
            {
                // Structurally unreachable given MinOuterSpan=3 (always leaves >=1 interior tile), kept
                // as a defensive guard: revert the outer raise and report failure like any other miss.
                RevertOuterRaise(corners, touchedThisPass, x0, y0, x1, y1);
                return false;
            }

            var savedLabels = new string[ix1 - ix0 + 1, iy1 - iy0 + 1];
            for (var x = ix0; x <= ix1; x++)
            for (var y = iy0; y <= iy1; y++)
                savedLabels[x - ix0, y - iy0] = corners.Labels[x, y];

            // Grows an irregular accent-terrain shape strictly inside the inset [ix0..ix1] x [iy0..iy1]
            // instead of blanket-filling the whole rectangle -- see TryGrowIrregularPoolInterior's own
            // doc comment. A rectangle-filling pool (accentCount==4 on every interior cell) is still
            // exactly what this produces when every candidate corner happens to resolve (the common
            // case for a small inset), so this is a strict superset of the old behavior, not a
            // regression: whatever shape the tileset can't support, growth simply stops early and
            // leaves those corners as ordinary raised Open floor (already a verified, independent shape).
            var grew = TryGrowIrregularPoolInterior(corners, layout.Crossers, cache, poolTerrain, ix0, iy0, ix1, iy1, random);

            var allResolve = grew.Count > 0;

            if (allResolve && !IsRoomStillConnected(corners, openTerrain, rx0, ry0, rx1, ry1))
                allResolve = false;

            if (!allResolve)
            {
                for (var x = ix0; x <= ix1; x++)
                for (var y = iy0; y <= iy1; y++)
                {
                    corners.Labels[x, y] = savedLabels[x - ix0, y - iy0];
                    corners.Heights[x, y] = 0;
                }

                RevertOuterRaise(corners, touchedThisPass, x0, y0, x1, y1);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Grows an irregular accent-terrain (pool) shape strictly inside [ix0..ix1] x [iy0..iy1] --
        /// the raised-Open inset TryPlacePool's outer rectangle just verified -- one corner at a time,
        /// mirroring LayoutElevationPainter.TryGrowIrregularOpenBlob's own incremental-verify approach:
        /// each candidate corner is tentatively flipped to (poolTerrain, height 0) and only the (up to 4)
        /// cells it touches are re-verified via LayoutElevationPainter.CellResolves before keeping it, so
        /// no whole-footprint re-check is needed after the first corner. This is what lets the resulting
        /// pool boundary include shapes TryPlacePool's old blanket-fill could never produce -- a concave
        /// notch (3 of 4 corners pool, the 4th still the raised rim) wherever the tileset's real
        /// inventory happens to carry that specific "3 accent corners" tile (see
        /// TileCoverageCensusTests.IsPoolBankReachable's own doc comment) -- while a full rectangle fill
        /// remains exactly what this produces whenever every candidate happens to resolve (the common,
        /// small-inset case), so this is a strict superset of the previous behavior.
        ///
        /// Returns every corner successfully flipped (in growth order); an empty list means even the
        /// seed corner failed to resolve, which the caller treats as total pool failure (reverts the
        /// outer raise too) exactly like the old blanket-fill's "no candidate resolves" case.
        /// </summary>
        private static List<(int X, int Y)> TryGrowIrregularPoolInterior(
            CornerTerrainGrid corners, EdgeCrosserGrid crossers, TileResolver.HeightAwareProbeCache cache,
            string poolTerrain, int ix0, int iy0, int ix1, int iy1, System.Random random)
        {
            var region = new List<(int X, int Y)>();

            var seed = (random.Next(ix0, ix1 + 1), random.Next(iy0, iy1 + 1));
            if (!TryFlipCornerToPool(corners, crossers, cache, poolTerrain, seed))
                return region; // empty -- even the seed corner doesn't resolve, a total failure

            region.Add(seed);
            var regionSet = new HashSet<(int X, int Y)> { seed };

            // Upper bound only -- the loop below stops naturally once no candidate resolves, well before
            // this is ever reached for a real, tightly-bounded inset (see MaxOuterSpan).
            var targetSize = (ix1 - ix0 + 1) * (iy1 - iy0 + 1);

            while (region.Count < targetSize)
            {
                var candidates = new List<(int X, int Y)>();
                foreach (var member in region)
                {
                    foreach (var (dx, dy) in LayoutCornerUtils.Ortho4)
                    {
                        var candidate = (member.X + dx, member.Y + dy);
                        if (candidate.Item1 < ix0 || candidate.Item1 > ix1 || candidate.Item2 < iy0 || candidate.Item2 > iy1) continue;
                        if (regionSet.Contains(candidate)) continue;
                        candidates.Add(candidate);
                    }
                }
                if (candidates.Count == 0) break;

                for (var i = candidates.Count - 1; i > 0; i--)
                {
                    var j = random.Next(i + 1);
                    (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                }

                var grew = false;
                foreach (var candidate in candidates)
                {
                    if (TryFlipCornerToPool(corners, crossers, cache, poolTerrain, candidate))
                    {
                        region.Add(candidate);
                        regionSet.Add(candidate);
                        grew = true;
                        break;
                    }
                }
                if (!grew) break; // every remaining candidate this round failed CellResolves -- stop, keep what grew so far
            }

            return region;
        }

        /// <summary>Tentatively flips one inset corner from (Open, raised) to (poolTerrain, 0) and
        /// verifies every cell it touches (up to 4) still resolves; reverts and returns false on any
        /// failure. See <see cref="TryGrowIrregularPoolInterior"/>'s doc comment.</summary>
        private static bool TryFlipCornerToPool(
            CornerTerrainGrid corners, EdgeCrosserGrid crossers, TileResolver.HeightAwareProbeCache cache,
            string poolTerrain, (int X, int Y) corner)
        {
            var savedLabel = corners.Labels[corner.X, corner.Y];
            var savedHeight = corners.Heights[corner.X, corner.Y];

            corners.Labels[corner.X, corner.Y] = poolTerrain;
            corners.Heights[corner.X, corner.Y] = 0;

            var allResolve = true;
            for (var cx = corner.X - 1; cx <= corner.X && allResolve; cx++)
            for (var cy = corner.Y - 1; cy <= corner.Y && allResolve; cy++)
            {
                if (cx < 0 || cy < 0 || cx >= corners.Width || cy >= corners.Height) continue;
                if (!LayoutElevationPainter.CellResolves(corners, crossers, cache, cx, cy)) allResolve = false;
            }

            if (!allResolve)
            {
                corners.Labels[corner.X, corner.Y] = savedLabel;
                corners.Heights[corner.X, corner.Y] = savedHeight;
                return false;
            }

            return true;
        }

        private static void RevertOuterRaise(
            CornerTerrainGrid corners, HashSet<(int X, int Y)> touchedThisPass, int x0, int y0, int x1, int y1)
        {
            for (var x = x0; x <= x1; x++)
            for (var y = y0; y <= y1; y++)
            {
                corners.Heights[x, y] = 0;
                touchedThisPass.Remove((x, y));
            }
        }

        /// <summary>
        /// True when every remaining <paramref name="openTerrain"/> corner strictly inside this room's
        /// own [rx0..rx1] x [ry0..ry1] bounding box is mutually reachable from every other (a
        /// bounding-box-scoped flood fill) -- see TryPlacePool's doc comment for why this must be scoped
        /// to the room rather than the whole layout. A room with zero remaining open corners (never
        /// expected in practice; MinOuterSpan always leaves a full 1-tile rim) trivially passes.
        /// </summary>
        private static bool IsRoomStillConnected(CornerTerrainGrid corners, string openTerrain, int rx0, int ry0, int rx1, int ry1)
        {
            var openCorners = new List<(int X, int Y)>();
            for (var x = rx0; x <= rx1; x++)
            for (var y = ry0; y <= ry1; y++)
            {
                if (corners.Labels[x, y] == openTerrain) openCorners.Add((x, y));
            }

            if (openCorners.Count == 0) return true;

            var visited = new HashSet<(int X, int Y)> { openCorners[0] };
            var frontier = new Queue<(int X, int Y)>();
            frontier.Enqueue(openCorners[0]);

            while (frontier.Count > 0)
            {
                var (cx, cy) = frontier.Dequeue();
                foreach (var (dx, dy) in LayoutCornerUtils.Ortho4)
                {
                    var next = (cx + dx, cy + dy);
                    if (next.Item1 < rx0 || next.Item1 > rx1 || next.Item2 < ry0 || next.Item2 > ry1) continue;
                    if (corners.Labels[next.Item1, next.Item2] != openTerrain) continue;
                    if (!visited.Add(next)) continue;
                    frontier.Enqueue(next);
                }
            }

            return visited.Count == openCorners.Count;
        }
    }
}
