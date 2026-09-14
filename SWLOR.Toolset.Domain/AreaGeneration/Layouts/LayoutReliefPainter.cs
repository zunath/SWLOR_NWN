#nullable disable
using System;
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// Shared "terrain relief" post-pass: per-corner perturb-and-verify height painting. Runs after
    /// LayoutElevationPainter (uniform raised blobs) and LayoutElevationPoolPainter (sunken accent
    /// pools) have settled their regions, and perturbs INDIVIDUAL corners -- open terrain, accent
    /// terrain, and an optional declared "slope blend" terrain alike -- one at a time, keeping a
    /// mutation only when every tile cell that corner touches still resolves against the REAL
    /// composed tileset (TileResolver's height-aware lookup, probed through a build-once
    /// HeightAwareProbeCache). This is the project's established tentative-commit/verify/revert
    /// pattern taken to its finest granularity, and it is the mechanism that reaches the per-corner
    /// INDEPENDENT (terrain, height) content no uniform region-growth pass can produce:
    ///
    ///   - same-terrain diagonal saddles (tde01 TILE503: all-Floor, opposite corners raised) -- a
    ///     single 4-connected grown region can never leave exactly two diagonal corners raised, but
    ///     two independent single-corner perturbations can;
    ///   - accent banks at mixed grades (tde01 TILE548-family: two Lava corners at two different
    ///     heights within one cell) -- region growth always raises a terrain patch to ONE height;
    ///   - raised accent corners inside an otherwise-flat pool (tde01 TILE506: three Floor corners at
    ///     grade, one Lava corner raised);
    ///   - blend-terrain slopes (tdm01's GentleSlope/GentleDesert/GentleOrganic): individual open
    ///     corners flipped to the tileset's declared blend terrain at either grade, rendering a
    ///     gradual walkable slope instead of a sheer step (see MacroLayoutParameters.
    ///     ReliefBlendTerrain).
    ///
    /// Every proposal is one of exactly three shapes, all verified live before keeping:
    ///   1. HEIGHT: toggle one corner's height between the room's grade (0) and one story up (1).
    ///      Never affects the open-corner connectivity graph (LayoutCornerUtils keys purely off
    ///      terrain LABELS -- the same argument LayoutElevationPainter's split-level blobs already
    ///      rely on), so no connectivity re-check is needed.
    ///   2. BLEND: flip one corner's label between OpenTerrain and the declared blend terrain,
    ///      keeping its height. This DOES remove/add the corner from the open graph, so each kept
    ///      flip additionally re-verifies the room's own bounding-box-scoped open-corner
    ///      connectivity (mirroring LayoutElevationPoolPainter.IsRoomStillConnected's scoping
    ///      argument) and reverts when broken.
    ///   3. LANE: splice a short straight run of ramp/slope edge crossers (the composition's
    ///      declared RampCrosser, canonical "Ramp" by default) across cells that carry height
    ///      content, batch-written then batch-verified exactly like LayoutElevationPainter.
    ///      TryCommitRampLane -- batch semantics are load-bearing: a lane's interior cell carries
    ///      TWO ramp edges at once, and several real tilesets (tdm01's Slope family) have no
    ///      one-edge intermediate tile for that cell, so writing edges one at a time could never
    ///      verify its way there.
    ///
    /// Proposal targeting is deliberately biased toward "interesting" corners -- those adjacent to
    /// existing height content or an accent/blend label boundary -- because the exempt height-tile
    /// census clusters (see TileCoverageCensusTests.IsTerrainReliefReachable) live exactly on those
    /// boundaries: mixed open/accent cells at mixed grades. A uniform-random corner in a big flat
    /// room floor would mostly propose isolated bumps the tileset supports anyway, so the bias costs
    /// nothing in safety (every proposal is still probe-verified) and concentrates the budget where
    /// the unreached vocabulary actually is.
    ///
    /// Runs after the pool painter and before LayoutGroupStamper (see MacroLayoutGenerator.Generate):
    /// final heights must be settled before the stamper's flat-cell guards run, and the stamper's own
    /// ReliefPiece kind (see LayoutGroupStamper) searches the grid THIS pass painted for cells whose
    /// (terrain, height) field matches a configured non-flat 1x1 group piece (e.g. tde01's baked-mesh
    /// "Ramp - Straight"/"Ramp - Corner, *" pieces).
    /// </summary>
    internal static class LayoutReliefPainter
    {
        private const int RaiseDelta = 1;
        // Per-region budgets: each corner proposal costs at most 4 cache probes (one per touched
        // cell), each lane proposal at most MaxLaneCells probes -- bounded well below the elevation
        // painter's own per-region budget, and the cache is built once per Paint() call (see
        // TileResolver.HeightAwareProbeCache).
        private const int ProposalsPerRegion = 48;
        private const int LaneProposalsPerRegion = 8;
        private const int MaxLaneCells = 4;

        internal static void Paint(MacroLayout layout, MacroLayoutParameters parameters, TilesetModel tileset, System.Random random)
        {
            if (tileset == null) return;
            if (parameters.ReliefRegions <= 0) return;

            var open = parameters.OpenTerrain;
            if (string.IsNullOrEmpty(open)) return;

            var blend = parameters.ReliefBlendTerrain ?? string.Empty;
            var allowedLabels = BuildAllowedLabels(parameters, blend);

            var cache = TileResolver.BuildHeightAwareProbeCache(tileset);

            // Capability gate: the smallest mutations this pass ever proposes are a lone raised open
            // corner and a lone open->blend flat flip. A tileset supporting neither has no per-corner
            // relief vocabulary at all -- fully inert, zero corners touched, zero extra RNG draws
            // beyond this point (mirrors LayoutElevationPainter's HasRimVocabulary short-circuit).
            var canRaiseOpen = TileResolver.HasHeightAwareCandidate(
                cache, open, open, open, open, "", "", "", "", 0, 0, 0, RaiseDelta);
            var canBlend = blend.Length != 0 && TileResolver.HasHeightAwareCandidate(
                cache, open, open, open, blend, "", "", "", "", 0, 0, 0, 0);
            if (!canRaiseOpen && !canBlend) return;

            var rampCrosser = LayoutElevationPainter.RampCrosserFor(parameters);
            var forbidden = BuildForbiddenCorners(layout);

            var roomOrder = new int[layout.Rooms.Count];
            for (var i = 0; i < roomOrder.Length; i++) roomOrder[i] = i;
            for (var i = roomOrder.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (roomOrder[i], roomOrder[j]) = (roomOrder[j], roomOrder[i]);
            }

            var painted = 0;
            foreach (var roomIndex in roomOrder)
            {
                if (painted >= parameters.ReliefRegions) break;

                var room = layout.Rooms[roomIndex];
                if (room.Tiles.Count == 0) continue;
                if (!string.Equals(room.OpenTerrain, open, StringComparison.Ordinal)) continue;

                if (PaintRoomRelief(layout, parameters, cache, allowedLabels, blend, rampCrosser, forbidden, room, random))
                    painted++;
            }

            if (painted > 0)
                LayoutCornerUtils.RecomputeFullyOpenRoomTiles(layout, open);
        }

        /// <summary>Labels whose corners this pass may mutate: the layout's open terrain, every
        /// distinct accent-family terrain the earlier passes may have painted (accent patches,
        /// channel bands, pool interiors), and the declared blend terrain.</summary>
        private static HashSet<string> BuildAllowedLabels(MacroLayoutParameters parameters, string blend)
        {
            var labels = new HashSet<string>(StringComparer.Ordinal) { parameters.OpenTerrain };
            if (!string.IsNullOrEmpty(parameters.AccentTerrain)) labels.Add(parameters.AccentTerrain);
            if (!string.IsNullOrEmpty(parameters.ChannelTerrain)) labels.Add(parameters.ChannelTerrain);
            if (!string.IsNullOrEmpty(parameters.PoolTerrain)) labels.Add(parameters.PoolTerrain);
            if (blend.Length != 0) labels.Add(blend);
            return labels;
        }

        /// <summary>Same "never mutate" corners as the other height passes: the border ring and every
        /// transition anchor tile's four corners (Entrance/Exit tiles stay flat, ground level).</summary>
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
        /// Runs one room's relief budget: corner proposals first (heights and blend flips), then lane
        /// proposals over whatever height steps now exist. Returns true when at least one mutation was
        /// kept -- the caller counts that as one painted region.
        /// </summary>
        private static bool PaintRoomRelief(
            MacroLayout layout, MacroLayoutParameters parameters, TileResolver.HeightAwareProbeCache cache,
            HashSet<string> allowedLabels, string blend, string rampCrosser,
            HashSet<(int X, int Y)> forbidden, LayoutRoom room, System.Random random)
        {
            var corners = layout.Corners;

            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var (tx, ty) in room.Tiles)
            {
                if (tx < minX) minX = tx;
                if (tx > maxX) maxX = tx;
                if (ty < minY) minY = ty;
                if (ty > maxY) maxY = ty;
            }

            // Corner bounds: tiles [minX..maxX] -> corners [minX..maxX+1]; mutations stay strictly
            // inside with a 1-corner margin (mirrors LayoutElevationPainter.TryGrowIrregularOpenBlob's
            // inset, keeping every touched cell's outer corners ordinary untouched territory).
            var rx0 = minX;
            var rx1 = maxX + 1;
            var ry0 = minY;
            var ry1 = maxY + 1;
            var lo = (X: rx0 + 1, Y: ry0 + 1);
            var hi = (X: rx1 - 1, Y: ry1 - 1);
            if (hi.X < lo.X || hi.Y < lo.Y) return false;

            var accepted = 0;

            for (var proposal = 0; proposal < ProposalsPerRegion; proposal++)
            {
                var corner = PickCorner(corners, allowedLabels, forbidden, lo, hi, random);
                if (corner == null) continue;
                var (cx, cy) = corner.Value;

                var label = corners.Labels[cx, cy];
                var height = corners.Heights[cx, cy];

                // Enumerate the legal mutations for this corner, then roll one. HEIGHT toggles apply
                // to every allowed label; BLEND flips only between the open terrain and the declared
                // blend terrain (at the corner's current height -- a flipped corner's height can then
                // be toggled by a later HEIGHT proposal, which is exactly how blend corners reach
                // both grades).
                var canToggleHeight = height == 0 || height == RaiseDelta;
                var isOpen = string.Equals(label, parameters.OpenTerrain, StringComparison.Ordinal);
                var isBlend = blend.Length != 0 && string.Equals(label, blend, StringComparison.Ordinal);
                var canFlipBlend = blend.Length != 0 && (isOpen || isBlend);

                bool tryBlendFlip;
                if (canToggleHeight && canFlipBlend)
                    tryBlendFlip = random.Next(2) == 0;
                else if (canFlipBlend)
                    tryBlendFlip = true;
                else if (canToggleHeight)
                    tryBlendFlip = false;
                else
                    continue;

                if (tryBlendFlip)
                {
                    var newLabel = isOpen ? blend : parameters.OpenTerrain;
                    if (TryMutateCorner(layout, cache, cx, cy, newLabel, height) &&
                        VerifyOpenConnectivity(layout, parameters.OpenTerrain, rx0, ry0, rx1, ry1, cx, cy, newLabel, height, label))
                    {
                        accepted++;
                    }
                }
                else
                {
                    var newHeight = height == 0 ? RaiseDelta : 0;
                    if (TryMutateCorner(layout, cache, cx, cy, label, newHeight))
                        accepted++;
                }
            }

            if (accepted > 0)
            {
                for (var lane = 0; lane < LaneProposalsPerRegion; lane++)
                    TrySpliceReliefLane(layout, cache, rampCrosser, minX, minY, maxX, maxY, random);
            }

            return accepted > 0;
        }

        /// <summary>
        /// Picks a mutation target corner, biased (roughly half the rolls) toward "interesting"
        /// corners -- those orthogonally adjacent to existing height content or to a different
        /// allowed label (an accent/blend boundary) -- with a plain uniform interior roll otherwise.
        /// See the class doc comment for why the bias is where the unreached vocabulary lives.
        /// </summary>
        private static (int X, int Y)? PickCorner(
            CornerTerrainGrid corners, HashSet<string> allowedLabels, HashSet<(int X, int Y)> forbidden,
            (int X, int Y) lo, (int X, int Y) hi, System.Random random)
        {
            var preferInteresting = random.Next(2) == 0;

            if (preferInteresting)
            {
                var interesting = new List<(int X, int Y)>();
                for (var x = lo.X; x <= hi.X; x++)
                for (var y = lo.Y; y <= hi.Y; y++)
                {
                    if (forbidden.Contains((x, y))) continue;
                    var label = corners.Labels[x, y];
                    if (!allowedLabels.Contains(label)) continue;

                    var isInteresting = corners.Heights[x, y] != 0;
                    if (!isInteresting)
                    {
                        foreach (var (dx, dy) in LayoutCornerUtils.Ortho4)
                        {
                            var nx = x + dx;
                            var ny = y + dy;
                            if (nx < 0 || ny < 0 || nx > corners.Width || ny > corners.Height) continue;
                            if (corners.Heights[nx, ny] != 0 ||
                                (corners.Labels[nx, ny] != label && allowedLabels.Contains(corners.Labels[nx, ny])))
                            {
                                isInteresting = true;
                                break;
                            }
                        }
                    }

                    if (isInteresting) interesting.Add((x, y));
                }

                if (interesting.Count > 0)
                    return interesting[random.Next(interesting.Count)];
                // fall through to a uniform roll -- a fully flat, single-terrain room has no
                // interesting corners yet, and the first accepted bump is what seeds them.
            }

            var rx = random.Next(lo.X, hi.X + 1);
            var ry = random.Next(lo.Y, hi.Y + 1);
            if (forbidden.Contains((rx, ry))) return null;
            if (!allowedLabels.Contains(corners.Labels[rx, ry])) return null;
            return (rx, ry);
        }

        /// <summary>
        /// Tentatively writes (label, height) to one corner and verifies every cell it touches (up to
        /// 4) still resolves; reverts and returns false on any failure. The incremental-soundness
        /// argument is LayoutElevationPainter.TryRaiseCorner's own: a cell's resolution depends only
        /// on its own 4 corners' (terrain, height) and its own edges, so mutating one corner can only
        /// ever change the cells that corner itself touches.
        /// </summary>
        private static bool TryMutateCorner(
            MacroLayout layout, TileResolver.HeightAwareProbeCache cache, int cx, int cy, string label, int height)
        {
            var corners = layout.Corners;
            var savedLabel = corners.Labels[cx, cy];
            var savedHeight = corners.Heights[cx, cy];
            if (savedLabel == label && savedHeight == height) return false;

            corners.Labels[cx, cy] = label;
            corners.Heights[cx, cy] = height;

            var allResolve = true;
            for (var x = cx - 1; x <= cx && allResolve; x++)
            for (var y = cy - 1; y <= cy && allResolve; y++)
            {
                if (x < 0 || y < 0 || x >= corners.Width || y >= corners.Height) continue;
                if (!LayoutElevationPainter.CellResolves(corners, layout.Crossers, cache, x, y)) allResolve = false;
            }

            if (!allResolve)
            {
                corners.Labels[cx, cy] = savedLabel;
                corners.Heights[cx, cy] = savedHeight;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Post-commit connectivity guard for a label flip (see class doc comment): every remaining
        /// open-terrain corner inside the room's own bounding box must stay mutually reachable.
        /// Reverts the just-committed flip and returns false when broken. Heights never enter this
        /// check -- the open graph keys off labels alone.
        /// </summary>
        private static bool VerifyOpenConnectivity(
            MacroLayout layout, string openTerrain, int rx0, int ry0, int rx1, int ry1,
            int cx, int cy, string committedLabel, int committedHeight, string previousLabel)
        {
            var corners = layout.Corners;

            var openCorners = new List<(int X, int Y)>();
            for (var x = rx0; x <= rx1; x++)
            for (var y = ry0; y <= ry1; y++)
            {
                if (corners.Labels[x, y] == openTerrain) openCorners.Add((x, y));
            }

            var connected = true;
            if (openCorners.Count > 0)
            {
                var visited = new HashSet<(int X, int Y)> { openCorners[0] };
                var frontier = new Queue<(int X, int Y)>();
                frontier.Enqueue(openCorners[0]);

                while (frontier.Count > 0)
                {
                    var (fx, fy) = frontier.Dequeue();
                    foreach (var (dx, dy) in LayoutCornerUtils.Ortho4)
                    {
                        var next = (fx + dx, fy + dy);
                        if (next.Item1 < rx0 || next.Item1 > rx1 || next.Item2 < ry0 || next.Item2 > ry1) continue;
                        if (corners.Labels[next.Item1, next.Item2] != openTerrain) continue;
                        if (!visited.Add(next)) continue;
                        frontier.Enqueue(next);
                    }
                }

                connected = visited.Count == openCorners.Count;
            }

            if (!connected)
            {
                corners.Labels[cx, cy] = previousLabel;
                // committedHeight was the corner's height both before and after a blend flip (flips
                // never change height), so restoring the label alone fully reverts the mutation.
                corners.Heights[cx, cy] = committedHeight;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Proposes one straight ramp/slope lane: N consecutive cells (2..MaxLaneCells) along one
        /// axis inside the room's own tile bounds, writing the N-1 shared interior edges to
        /// <paramref name="rampCrosser"/> and then verifying every lane cell in one batch -- exactly
        /// LayoutElevationPainter.TryCommitRampLane's batch-write/batch-verify/batch-revert shape (see
        /// the class doc comment for why batch semantics are load-bearing). Requires at least one lane
        /// cell to carry height content (a lane across dead-flat floor proposes nothing the plain
        /// resolver doesn't already do, and no real tileset carries flat ramp-crossed tiles), and
        /// every lane cell to be currently crosser-free so the revert path is exact.
        /// </summary>
        private static void TrySpliceReliefLane(
            MacroLayout layout, TileResolver.HeightAwareProbeCache cache, string rampCrosser,
            int minTileX, int minTileY, int maxTileX, int maxTileY, System.Random random)
        {
            var corners = layout.Corners;
            var crossers = layout.Crossers;

            var alongY = random.Next(2) == 0;
            var count = random.Next(2, MaxLaneCells + 1);

            int startX, startY;
            if (alongY)
            {
                if (maxTileY - minTileY + 1 < count) return;
                startX = random.Next(minTileX, maxTileX + 1);
                startY = random.Next(minTileY, maxTileY - count + 2);
            }
            else
            {
                if (maxTileX - minTileX + 1 < count) return;
                startX = random.Next(minTileX, maxTileX - count + 2);
                startY = random.Next(minTileY, maxTileY + 1);
            }

            var cells = new List<(int X, int Y)>();
            for (var i = 0; i < count; i++)
                cells.Add(alongY ? (startX, startY + i) : (startX + i, startY));

            var anyHeight = false;
            foreach (var (cx, cy) in cells)
            {
                if (cx < 0 || cy < 0 || cx >= corners.Width || cy >= corners.Height) return;
                for (var slot = 0; slot < 4; slot++)
                {
                    // A lane cell may already carry THIS pass's own ramp crosser (a previously-spliced
                    // lane -- two lanes crossing at one cell is real tileset vocabulary, e.g. tdm01's
                    // 4-Slope junction tiles), but never any other crosser family (fences, corridors,
                    // bridges -- foreign features this pass must not disturb).
                    var existing = crossers.GetEdge(cx, cy, slot);
                    if (existing.Length != 0 && !string.Equals(existing, rampCrosser, StringComparison.OrdinalIgnoreCase))
                        return;
                }

                if (corners.Heights[cx, cy] != 0 || corners.Heights[cx + 1, cy] != 0 ||
                    corners.Heights[cx, cy + 1] != 0 || corners.Heights[cx + 1, cy + 1] != 0)
                {
                    anyHeight = true;
                }
            }
            if (!anyHeight) return;

            // Batch-write the N-1 shared interior edges (writing cell i's Top/Right edge also writes
            // cell i+1's Bottom/Left edge -- EdgeCrosserGrid stores one value per SHARED edge), saving
            // each written slot's previous value so the revert path is exact even when this lane
            // crosses an earlier one.
            var innerSlot = alongY ? EdgeSlot.Top : EdgeSlot.Right;
            var savedEdges = new string[count - 1];
            for (var i = 0; i < count - 1; i++)
            {
                var (cx, cy) = cells[i];
                savedEdges[i] = crossers.GetEdge(cx, cy, innerSlot);
                crossers.SetEdge(cx, cy, innerSlot, rampCrosser);
            }

            var allResolve = true;
            foreach (var (cx, cy) in cells)
            {
                if (!LayoutElevationPainter.CellResolves(corners, crossers, cache, cx, cy)) { allResolve = false; break; }
            }

            if (!allResolve)
            {
                for (var i = 0; i < count - 1; i++)
                {
                    var (cx, cy) = cells[i];
                    crossers.SetEdge(cx, cy, innerSlot, savedEdges[i]);
                }
            }
        }
    }
}
