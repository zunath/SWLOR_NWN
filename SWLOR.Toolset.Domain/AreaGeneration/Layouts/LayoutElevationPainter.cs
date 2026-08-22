#nullable disable
using System;
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// Shared elevation post-pass: raises small blob-shaped regions of same-terrain corners by one
    /// story, using ONLY corner-height blending (CornerTerrainGrid.Heights / TileResolver's
    /// height-aware lookup, see HeightResolutionTests) -- never a discrete "Stairs Up/Down" baked-mesh
    /// group tile, which is a different, unrelated NWN tileset mechanism this pass does not touch.
    ///
    /// Tries two independent terrain targets, in order, both gated on a live shape probe
    /// (TileResolver.HasHeightAwareCandidate) against the REAL composed tileset rather than any
    /// hand-maintained per-tileset list:
    ///   1. SolidTerrain blobs ("elevation blobs" proper): raises interior wall/rock territory, e.g. a
    ///      standing pillar or raised dais of solid ground. Zero connectivity risk by construction --
    ///      SolidTerrain corners are never part of the open-corner graph LayoutCornerUtils walks.
    ///   2. OpenTerrain blobs ("split-level" rooms): raises a patch of a room's own walkable floor,
    ///      strictly inside its open territory (rim verification means it can never reach a room's
    ///      wall boundary in practice -- see the class-level probe notes below). Needs no TunnelLink:
    ///      LayoutCornerUtils' connectivity graph keys purely off terrain LABEL, which this pass never
    ///      changes, so a raised patch stays part of the same connected open component its surrounding
    ///      floor already belongs to.
    ///
    /// Probe evidence (tde01/Dungeon, 1092 tiles, 323 non-flat): Wall (SolidTerrain) NEVER carries a
    /// nonzero corner height anywhere in this tileset's real inventory -- every raised tile's height
    /// content sits on Floor and the accent-pool terrains (Lava/Water/Sewer/Ice/Pit), never on Wall --
    /// so mechanism 1 is structurally INERT for tde01 (shape probe correctly finds no rim vocabulary
    /// and this pass paints zero solid blobs there), while mechanism 2 has real, verified vocabulary:
    /// six ungrouped, blank-edge, all-Floor tiles (TILE500/501/623/737/868/1002) whose normalized
    /// corner-height deltas are exactly a raised rectangle's two rim shapes -- one corner raised (a
    /// blob's convex outer corner) or two ADJACENT corners raised to the same delta (a blob's straight
    /// edge) -- plus the interior of any raised patch resolves for free using the tileset's ordinary
    /// flat Floor tile pool, placed at a nonzero Tile_Height by TileResolver's existing
    /// delta-profile-normalization (see TileResolver class doc; no special "plateau top" tile is
    /// needed). tde01 additionally has 32 ungrouped "Ramp" edge-crosser tiles (TILE560-562 pure-Floor,
    /// plus per-liquid families) sharing the EXACT two-adjacent-corners-raised rim shape above, just
    /// with a Ramp crosser on the axis perpendicular to the height transition -- see
    /// MacroLayoutParameters.ElevationRamps/TryAddRampLane, which optionally splices a Ramp lane into
    /// one straight rim edge of a placed OpenTerrain blob so the raised patch is walkable up to, not
    /// just steppable. The 1x1-GROUPed "Ramp - Straight/Corner, *" pieces remain unused (they are
    /// non-flat, so LayoutGroupStamper's TryClassify rejects them outright -- a genuinely separate,
    /// still-unclaimed mechanism, not this pass's concern).
    ///
    /// Runs after LayoutFenceCarver (so it can see fence crossers and avoid them) and before
    /// LayoutGroupStamper (see MacroLayoutGenerator.Generate): the stamper's own flat-cell guards
    /// (TileDoorGeometry.IsFlatCell, checked throughout LayoutGroupStamper) read
    /// CornerTerrainGrid.Heights directly, so as long as this pass has already written final heights
    /// before the stamper runs, those guards correctly refuse to stamp a set piece onto a
    /// now-raised cell with zero extra code on the stamper's side.
    /// </summary>
    internal static class LayoutElevationPainter
    {
        private const int RaiseDelta = 1;

        // Span is in TILES (the raised rectangle is span+1 corners on that axis); the padded footprint
        // needing verification is span+2 corners on each axis (see TryPaintBlobRegion). A 1x1-tile
        // raised patch (span=1) needs only a 3-corner room/pocket on each axis (matching
        // MinRoomCornerSize's own floor of 3), the smallest useful "split-level" a room can offer once
        // a 1-corner Floor margin is reserved on every side (Wall never carries height in tde01 -- see
        // class doc -- so that margin can never itself be Wall).
        private const int MinBlobSpan = 1;
        private const int MaxBlobSpan = 3;
        private const int MaxPlacementAttempts = 60;
        private const int RoomPlacementAttempts = 8;
        private const int MaxAttemptsPerRegion = 80;

        internal static void Paint(MacroLayout layout, MacroLayoutParameters parameters, TilesetModel tileset, System.Random random)
        {
            if (tileset == null) return;
            if (parameters.ElevationRegions <= 0) return;

            var corners = layout.Corners;

            // Once-per-resolve shape probes (cheap; TileResolver builds the lookup fresh each call, but
            // this runs at most twice per generation attempt, not per cell).
            var solidRimOk = HasRimVocabulary(tileset, parameters.SolidTerrain);
            var openRimOk = HasRimVocabulary(tileset, parameters.OpenTerrain);
            if (!solidRimOk && !openRimOk) return; // fully inert -- no elevation vocabulary at all

            // Built ONCE per Paint() call and reused for every placement attempt's CellResolves calls:
            // TileResolver.HasHeightAwareCandidate(TilesetModel, ...) rebuilds its whole candidate
            // lookup from scratch on every invocation, so probing it per-cell (a rectangle attempt) or,
            // worse, per-corner-growth-step (TryGrowIrregularOpenBlob below) would recompute the same
            // lookup dozens of times per attempt -- see TileResolver.HeightAwareProbeCache's own doc
            // comment.
            var cache = TileResolver.BuildHeightAwareProbeCache(tileset);

            var forbidden = BuildForbiddenCorners(layout, corners);
            var touchedThisPass = new HashSet<(int X, int Y)>();

            var painted = 0;
            var attempts = 0;
            var maxAttempts = parameters.ElevationRegions * MaxAttemptsPerRegion;

            while (painted < parameters.ElevationRegions && attempts < maxAttempts)
            {
                attempts++;

                if (solidRimOk && TryPaintSolidBlob(layout, cache, parameters.SolidTerrain, forbidden, touchedThisPass, random))
                {
                    painted++;
                    continue;
                }

                if (openRimOk && TryPaintOpenRoomBlob(layout, parameters, cache, parameters.OpenTerrain, forbidden, touchedThisPass, random))
                {
                    painted++;
                }
            }

            if (painted > 0)
                LayoutCornerUtils.RecomputeFullyOpenRoomTiles(layout, parameters.OpenTerrain);
        }

        /// <summary>
        /// Corners this pass must never raise: the border ring (kept flat in v1 -- see class doc) and
        /// every transition anchor tile's four corners (Entrance/Exit tiles must stay flat, ground
        /// level). Deliberately does NOT separately forbid every room's CenterTile: LayoutAccentPainter
        /// forbids repainting a center tile's TERRAIN LABEL (so it can never become inaccessible accent
        /// terrain) -- an unrelated concern to a height-only change. A room hosting a transition already
        /// has its CenterTile protected via the transition-anchor rule above; a room's own geometric
        /// center is otherwise not special to this pass, and for typical small dungeon rooms (a 5-corner
        /// room's only interior raise-candidate positions ARE its center tile's own corners -- there is
        /// no other interior position to offer) excluding it would make the mechanism structurally
        /// unable to ever paint at all on realistic room sizes.
        /// </summary>
        private static HashSet<(int X, int Y)> BuildForbiddenCorners(MacroLayout layout, CornerTerrainGrid corners)
        {
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
        /// True when <paramref name="terrain"/> has at least the two rim delta shapes a rectangular
        /// blob needs: one corner raised by <see cref="RaiseDelta"/> (a blob's convex outer corner) and
        /// two ADJACENT corners raised (a blob's straight edge), both as flat, ungrouped, blank-edge,
        /// uniform-terrain tiles. Mirrors TileCoverageCensusTests' ElevationBlob classification.
        /// </summary>
        private static bool HasRimVocabulary(TilesetModel tileset, string terrain)
        {
            if (string.IsNullOrEmpty(terrain)) return false;

            var oneCorner = TileResolver.HasHeightAwareCandidate(
                tileset, terrain, terrain, terrain, terrain, "", "", "", "",
                0, 0, RaiseDelta, 0);

            var twoAdjacent = TileResolver.HasHeightAwareCandidate(
                tileset, terrain, terrain, terrain, terrain, "", "", "", "",
                0, RaiseDelta, RaiseDelta, 0);

            return oneCorner && twoAdjacent;
        }

        /// <summary>
        /// Picks a random axis-aligned rectangle of same-terrain, currently-flat corners ANYWHERE on
        /// the grid -- appropriate for SolidTerrain, whose territory isn't scoped to any single room.
        /// See TryPlaceRectangle for the shape rationale and commit/verify/revert mechanics.
        /// </summary>
        private static bool TryPaintSolidBlob(
            MacroLayout layout, TileResolver.HeightAwareProbeCache cache, string terrain,
            HashSet<(int X, int Y)> forbidden, HashSet<(int X, int Y)> touchedThisPass, System.Random random)
        {
            var corners = layout.Corners;

            for (var placementAttempt = 0; placementAttempt < MaxPlacementAttempts; placementAttempt++)
            {
                var spanX = random.Next(MinBlobSpan, MaxBlobSpan + 1);
                var spanY = random.Next(MinBlobSpan, MaxBlobSpan + 1);

                // x0/y0 is the raised rectangle's own low corner; the padded footprint extends one
                // corner further in every direction (see TryPlaceRectangle), so x0/y0 must leave room
                // for that margin too.
                var maxX0 = corners.Width - spanX - 1;
                var maxY0 = corners.Height - spanY - 1;
                if (maxX0 < 1 || maxY0 < 1) return false; // grid too small for this span -- never safe

                var x0 = random.Next(1, maxX0 + 1);
                var y0 = random.Next(1, maxY0 + 1);

                if (TryPlaceRectangle(layout, cache, terrain, forbidden, touchedThisPass, x0, y0, x0 + spanX, y0 + spanY))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Scopes the random rectangle search to a single room's own footprint (derived from its
        /// LayoutRoom.Tiles bounding box) rather than blind whole-grid guessing -- OpenTerrain territory
        /// is fragmented across many small, individually-walled rooms (Tunnel-mode corridors carve
        /// through solid territory, not open lanes), so a whole-grid random guess would spend nearly
        /// every attempt landing partly or fully outside any single room's open floor. Tries every room
        /// (in a randomized order so no single room is favored) before giving up, since most rooms at a
        /// typical MinRoomCornerSize/MaxRoomCornerSize range are too small to host any span at all --
        /// see TryPlaceRectangle's margin requirement.
        /// </summary>
        private static bool TryPaintOpenRoomBlob(
            MacroLayout layout, MacroLayoutParameters parameters, TileResolver.HeightAwareProbeCache cache, string terrain,
            HashSet<(int X, int Y)> forbidden, HashSet<(int X, int Y)> touchedThisPass, System.Random random)
        {
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
                if (!string.Equals(room.OpenTerrain, terrain, StringComparison.Ordinal)) continue;

                int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
                foreach (var (tx, ty) in room.Tiles)
                {
                    if (tx < minX) minX = tx;
                    if (tx > maxX) maxX = tx;
                    if (ty < minY) minY = ty;
                    if (ty > maxY) maxY = ty;
                }

                // Room corner bounds: tiles span [minX..maxX] x [minY..maxY], so corners span
                // [minX..maxX+1] x [minY..maxY+1].
                var rx0 = minX;
                var rx1 = maxX + 1;
                var ry0 = minY;
                var ry1 = maxY + 1;

                // Alternates between the two independent strategies each attempt: a plain rectangle
                // (fast, and the only shape that ever needs a Ramp lane spliced in -- see below) and an
                // irregular corner-by-corner grown blob (TryGrowIrregularOpenBlob), which can additionally
                // reach concave "3 corners raised" notches and other non-rectangular rim shapes a real
                // tileset happens to carry (e.g. tde01/tdm01's Floor family -- see
                // TileCoverageCensusTests.IsElevationBlobReachable's own concave-shape doc comment).
                // Irregular growth is strictly more expensive per attempt (one cache probe per grown
                // corner instead of one batch probe for the whole rectangle), so it only gets a fraction
                // of this room's attempt budget, not all of it.
                for (var placementAttempt = 0; placementAttempt < RoomPlacementAttempts; placementAttempt++)
                {
                    var tryIrregular = placementAttempt % 2 == 1;
                    if (tryIrregular)
                    {
                        if (TryGrowIrregularOpenBlob(layout, cache, terrain, forbidden, touchedThisPass, rx0, ry0, rx1, ry1, random))
                            return true;
                        continue;
                    }

                    var spanX = random.Next(MinBlobSpan, MaxBlobSpan + 1);
                    var spanY = random.Next(MinBlobSpan, MaxBlobSpan + 1);

                    // The raised rectangle must sit strictly inside the room, inset by exactly 1 corner
                    // from the room's own boundary on every side (that inset ring IS the rim -- see
                    // TryPlaceRectangle), so it must fit entirely within [rx0+1 .. rx1-1] on each axis.
                    var minX0 = rx0 + 1;
                    var maxX0 = rx1 - 1 - spanX;
                    var minY0 = ry0 + 1;
                    var maxY0 = ry1 - 1 - spanY;
                    if (maxX0 < minX0 || maxY0 < minY0) continue; // room too small for this span

                    var x0 = random.Next(minX0, maxX0 + 1);
                    var y0 = random.Next(minY0, maxY0 + 1);

                    if (TryPlaceRectangle(layout, cache, terrain, forbidden, touchedThisPass, x0, y0, x0 + spanX, y0 + spanY))
                    {
                        // Best-effort, purely additive: a Ramp lane is a bonus on top of an already-
                        // successfully-placed blob, never a precondition for it. Only attempted for
                        // OpenTerrain (the only blob kind with any real vocabulary in the one tileset
                        // (tde01) that has Ramp vocabulary at all -- see TryAddRampLane's own live probe,
                        // which no-ops harmlessly on every other tileset/terrain pairing).
                        if (parameters.ElevationRamps)
                            TryAddRampLane(layout, cache, RampCrosserFor(parameters), x0, y0, x0 + spanX, y0 + spanY, random);

                        return true;
                    }
                }
            }

            return false;
        }

        // Irregular growth footprint bounds, in CORNERS: small enough to stay cheap (each accepted
        // corner costs up to 4 cache probes; MaxIrregularCorners * MaxGrowStepAttempts bounds the worst
        // case for a single TryGrowIrregularOpenBlob call), large enough to reach the concave "3 of 4
        // corners raised" notch shape (needs at least 3 accepted corners: two adjacent-to-the-seed plus
        // the seed itself, arranged in an L).
        private const int MinIrregularCorners = 1;
        private const int MaxIrregularCorners = 6;
        private const int MaxGrowStepAttempts = 12;
        private const int MaxSeedAttempts = 6;

        /// <summary>
        /// Grows a connected, irregular set of raised corners strictly inside a room's own bounding box
        /// (same 1-corner margin as <see cref="TryPlaceRectangle"/>'s rim requirement), one corner at a
        /// time: seed a random interior corner, then repeatedly try a random orthogonal neighbor of the
        /// current region, tentatively raising it and re-verifying only the (up to 4) cells THAT corner
        /// touches via <see cref="CellResolves"/> before keeping it. This is sound incrementally --
        /// a cell's resolution depends only on its own 4 corners' current (terrain, height), so adding
        /// one corner can only ever change the cells that corner itself touches, never a cell touching
        /// none of the just-added corners -- so no whole-footprint re-verification is needed the way
        /// <see cref="TryPlaceRectangle"/>'s batch rectangle commit needs one.
        ///
        /// Unlike TryPlaceRectangle's forced-rectangle boundary (always exactly the two verified rim
        /// shapes), an irregular region's own boundary can include a concave notch (a rim cell whose
        /// OTHER three corners all happen to be region members) wherever the tileset's real inventory
        /// happens to carry that specific "3 corners raised" tile -- CellResolves' live probe is the
        /// only thing standing between a candidate corner and rejection, so no shape is ever assumed,
        /// only ever actually placed after the real tileset confirms it.
        /// </summary>
        private static bool TryGrowIrregularOpenBlob(
            MacroLayout layout, TileResolver.HeightAwareProbeCache cache, string terrain,
            HashSet<(int X, int Y)> forbidden, HashSet<(int X, int Y)> touchedThisPass,
            int rx0, int ry0, int rx1, int ry1, System.Random random)
        {
            var corners = layout.Corners;
            var crossers = layout.Crossers;

            // Strictly inside the room, 1-corner margin on every side (mirrors TryPaintOpenRoomBlob's
            // rectangle inset -- that margin ring is what makes every touched cell's OTHER corners
            // ordinary flat open-terrain, exactly like the rectangle case's padding ring).
            var minX = rx0 + 1;
            var maxX = rx1 - 1;
            var minY = ry0 + 1;
            var maxY = ry1 - 1;
            if (maxX < minX || maxY < minY) return false;

            (int X, int Y)? seed = null;
            for (var seedAttempt = 0; seedAttempt < MaxSeedAttempts && seed == null; seedAttempt++)
            {
                var sx = random.Next(minX, maxX + 1);
                var sy = random.Next(minY, maxY + 1);
                if (IsCornerSafe(corners, touchedThisPass, terrain, sx, sy) && !forbidden.Contains((sx, sy)))
                    seed = (sx, sy);
            }
            if (seed == null) return false;

            var region = new List<(int X, int Y)> { seed.Value };
            var regionSet = new HashSet<(int X, int Y)> { seed.Value };

            if (!TryRaiseCorner(corners, crossers, cache, seed.Value))
                return false; // the lone seed corner itself doesn't resolve -- never expected given HasRimVocabulary, but defensive

            var targetSize = random.Next(MinIrregularCorners, MaxIrregularCorners + 1);

            for (var step = 0; step < MaxGrowStepAttempts && region.Count < targetSize; step++)
            {
                var candidates = new List<(int X, int Y)>();
                foreach (var member in region)
                {
                    foreach (var (dx, dy) in LayoutCornerUtils.Ortho4)
                    {
                        var candidate = (member.X + dx, member.Y + dy);
                        if (candidate.Item1 < minX || candidate.Item1 > maxX || candidate.Item2 < minY || candidate.Item2 > maxY) continue;
                        if (regionSet.Contains(candidate)) continue;
                        if (forbidden.Contains(candidate)) continue;
                        if (!IsCornerSafe(corners, touchedThisPass, terrain, candidate.Item1, candidate.Item2)) continue;
                        candidates.Add(candidate);
                    }
                }
                if (candidates.Count == 0) break;

                Shuffle(candidates, random);

                var grew = false;
                foreach (var candidate in candidates)
                {
                    if (TryRaiseCorner(corners, crossers, cache, candidate))
                    {
                        region.Add(candidate);
                        regionSet.Add(candidate);
                        grew = true;
                        break;
                    }
                }
                if (!grew) break; // every remaining candidate this round failed CellResolves -- stop, keep what grew so far
            }

            foreach (var member in region)
                touchedThisPass.Add(member);

            return true; // region.Count is always >= 1 (the seed) once we reach here
        }

        /// <summary>
        /// Tentatively raises a single corner by <see cref="RaiseDelta"/> and verifies every cell that
        /// corner touches (up to 4) still resolves; reverts and returns false on any failure. See
        /// <see cref="TryGrowIrregularOpenBlob"/>'s doc comment for why only the touching cells (not the
        /// whole footprint) need re-checking.
        /// </summary>
        private static bool TryRaiseCorner(
            CornerTerrainGrid corners, EdgeCrosserGrid crossers, TileResolver.HeightAwareProbeCache cache, (int X, int Y) corner)
        {
            corners.Heights[corner.X, corner.Y] = RaiseDelta;

            var allResolve = true;
            for (var cx = corner.X - 1; cx <= corner.X && allResolve; cx++)
            for (var cy = corner.Y - 1; cy <= corner.Y && allResolve; cy++)
            {
                if (cx < 0 || cy < 0 || cx >= corners.Width || cy >= corners.Height) continue;
                if (!CellResolves(corners, crossers, cache, cx, cy)) allResolve = false;
            }

            if (!allResolve)
            {
                corners.Heights[corner.X, corner.Y] = 0;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Splices a Ramp edge-crosser lane into ONE straight rim edge of an already-placed raised
        /// rectangle [x0..x1] x [y0..y1] (see TryPaintOpenRoomBlob), turning that edge's plain "two
        /// adjacent corners raised" rim cells into their Ramp-crossed twin (tde01 TILE560-562: the
        /// identical corner/height shape, plus a Ramp edge) so the raised patch is walkable up to from
        /// ground level rather than only reachable by the engine's own auto-step. Purely an
        /// EdgeCrosserGrid rewrite -- no corner terrain or height change, so this can never affect
        /// whether the underlying blob itself is valid.
        ///
        /// A rim edge of tile-length N needs at least 2 cells: the shared boundary between any two
        /// consecutive lane cells carries "Ramp" (giving each of them one Ramp edge facing its
        /// neighbor), while the two end cells' OUTWARD edges (facing the rectangle's own corner cells,
        /// which have no Ramp vocabulary of their own -- see class doc) stay blank. A length-1 edge has
        /// no such interior boundary to carry the shared edge without touching a corner cell, so it is
        /// skipped (silently -- this is a bonus, not a requirement).
        ///
        /// Tries all 4 candidate edges (in a random order) and commits the first whose entire lane
        /// re-verifies live via TileResolver.HasHeightAwareCandidate (the authoritative check, exactly
        /// CellResolves' own convention) with its Ramp edges in place; reverts and tries the next edge
        /// on any failure. Records a single TunnelLink spanning the lane's low-to-high corners so
        /// link-aware connectivity/geodesic passes have real data for this connection, even though nothing
        /// about a same-terrain-label OpenTerrain blob ever actually requires one for connectivity today
        /// (see class doc) -- forward-looking parity with outdoor Slope-tileset use, where a Ramp lane
        /// may one day be the only walkable link between two otherwise-severed regions.
        /// </summary>
        /// <summary>Effective ramp-lane crosser name: the composition's own declared alternate (e.g.
        /// tdm01's "Slope") or the canonical "Ramp" when none is declared -- shared with
        /// LayoutReliefPainter's lane proposals so both mechanisms splice the same vocabulary.</summary>
        internal static string RampCrosserFor(MacroLayoutParameters parameters)
        {
            return string.IsNullOrEmpty(parameters.RampCrosser) ? "Ramp" : parameters.RampCrosser;
        }

        private static void TryAddRampLane(
            MacroLayout layout, TileResolver.HeightAwareProbeCache cache, string rampCrosser, int x0, int y0, int x1, int y1, System.Random random)
        {
            // Each candidate: the column/row of rim CELLS just outside the raised rectangle on that
            // side, whether the lane runs along Y (east/west edges, Ramp on Top/Bottom) or along X
            // (north/south edges, Ramp on Left/Right), and a representative (ground corner, raised
            // corner) pair for the TunnelLink bookkeeping (see method doc).
            var candidates = new List<(int CellX, int CellY, bool AlongY, int Count, (int X, int Y) GroundCorner, (int X, int Y) RaisedCorner)>
            {
                // East edge: cells at cell-x = x1, cell-y in [y0..y1-1]; raised column is x1 (west side of the cell), ground is x1+1 (east side).
                (x1, y0, true, y1 - y0, (x1 + 1, y0), (x1, y0)),
                // West edge: cells at cell-x = x0-1, cell-y in [y0..y1-1]; raised column is x0 (east side of the cell), ground is x0-1 (west side).
                (x0 - 1, y0, true, y1 - y0, (x0 - 1, y0), (x0, y0)),
                // North edge: cells at cell-y = y1, cell-x in [x0..x1-1]; raised row is y1 (south side of the cell), ground is y1+1 (north side).
                (x0, y1, false, x1 - x0, (x0, y1 + 1), (x0, y1)),
                // South edge: cells at cell-y = y0-1, cell-x in [x0..x1-1]; raised row is y0 (north side of the cell), ground is y0-1 (south side).
                (x0, y0 - 1, false, x1 - x0, (x0, y0 - 1), (x0, y0)),
            };

            Shuffle(candidates, random);

            foreach (var (cellX, cellY, alongY, count, groundCorner, raisedCorner) in candidates)
            {
                if (count < 2) continue; // no interior boundary to carry the shared Ramp edge

                if (TryCommitRampLane(layout, cache, rampCrosser, cellX, cellY, alongY, count, groundCorner, raisedCorner))
                    return; // one lane per blob is plenty; first success wins
            }
        }

        private static bool TryCommitRampLane(
            MacroLayout layout, TileResolver.HeightAwareProbeCache cache, string rampCrosser,
            int cellX, int cellY, bool alongY, int count,
            (int X, int Y) groundCorner, (int X, int Y) raisedCorner)
        {
            var corners = layout.Corners;
            var crossers = layout.Crossers;

            // The N cells of this lane, in order along its own axis.
            var cells = new List<(int X, int Y)>();
            for (var i = 0; i < count; i++)
                cells.Add(alongY ? (cellX, cellY + i) : (cellX + i, cellY));

            // Every lane cell must currently be crosser-free (a fresh rim cell this pass itself just
            // painted, never shared with an unrelated feature) -- defensive; true by construction since
            // these cells were only ever touched (if at all) by this same TryPlaceRectangle call.
            foreach (var (cx, cy) in cells)
            {
                for (var slot = 0; slot < 4; slot++)
                {
                    if (crossers.GetEdge(cx, cy, slot).Length != 0) return false;
                }
            }

            var innerSlot = alongY ? EdgeSlot.Top : EdgeSlot.Right;

            // Write the N-1 shared interior boundaries to Ramp (the two true end-facing edges, at the
            // start of cells[0] and the end of cells[^1], stay blank -- they face the rectangle's own
            // corner cells, which have no Ramp vocabulary). Setting cell i's own Top (or Right) edge
            // also writes cell i+1's Bottom (or Left) edge -- EdgeCrosserGrid stores one value per
            // SHARED edge (see its doc comment) -- so no separate write is needed for the neighbor side.
            for (var i = 0; i < count - 1; i++)
            {
                var (cx, cy) = cells[i];
                crossers.SetEdge(cx, cy, innerSlot, rampCrosser);
            }

            var allResolve = true;
            foreach (var (cx, cy) in cells)
            {
                if (!CellResolves(corners, crossers, cache, cx, cy)) { allResolve = false; break; }
            }

            if (!allResolve)
            {
                foreach (var (cx, cy) in cells)
                {
                    crossers.SetEdge(cx, cy, EdgeSlot.Top, string.Empty);
                    crossers.SetEdge(cx, cy, EdgeSlot.Right, string.Empty);
                    crossers.SetEdge(cx, cy, EdgeSlot.Bottom, string.Empty);
                    crossers.SetEdge(cx, cy, EdgeSlot.Left, string.Empty);
                }
                return false;
            }

            // Bookkeeping only (see method doc): record a link spanning the lane's ground-side corner
            // to its raised-side corner.
            layout.TunnelLinks.Add(new TunnelLink
            {
                CornerA = groundCorner,
                CornerB = raisedCorner,
                Length = 1
            });

            return true;
        }

        /// <summary>
        /// Tentatively raises the rectangle [x0..x1] x [y0..y1] (a filled rectangle rather than an
        /// irregular blob is deliberate: its boundary against untouched background is ALWAYS exactly
        /// the two rim shapes HasRimVocabulary verified -- one convex corner raised at each of the
        /// rectangle's 4 corners, two adjacent corners raised along each straight edge -- never a
        /// concave 3-corner notch this tileset's real inventory doesn't cover), then verifies EVERY
        /// tile cell touching the raised footprint (the rectangle plus its 1-corner outer rim) actually
        /// resolves via TileResolver.HasHeightAwareCandidate before committing -- the authoritative,
        /// tileset-real check, rather than trusting the coarse HasRimVocabulary probe alone. Reverts and
        /// reports failure (a normal, expected outcome -- callers retry with a new placement) on any
        /// violation.
        /// </summary>
        internal static bool TryPlaceRectangle(
            MacroLayout layout, TileResolver.HeightAwareProbeCache cache, string terrain,
            HashSet<(int X, int Y)> forbidden, HashSet<(int X, int Y)> touchedThisPass,
            int x0, int y0, int x1, int y1)
        {
            var corners = layout.Corners;

            var padX0 = x0 - 1;
            var padY0 = y0 - 1;
            var padX1 = x1 + 1;
            var padY1 = y1 + 1;
            if (padX0 < 0 || padY0 < 0 || padX1 > corners.Width || padY1 > corners.Height) return false;

            // Padding-ring corners (the rim) are never raised, so the "never raise" forbidden set
            // (border/transition-anchor/room-center -- see BuildForbiddenCorners) doesn't apply to
            // them; they only need to currently BE this terrain, flat, untouched, and crosser-free so
            // the rim shape's math is valid. The inner rectangle corners (the ones actually about to be
            // raised) additionally must not be in the forbidden set.
            var safe = true;
            for (var x = padX0; x <= padX1 && safe; x++)
            for (var y = padY0; y <= padY1 && safe; y++)
            {
                var isInner = x >= x0 && x <= x1 && y >= y0 && y <= y1;
                if (isInner && forbidden.Contains((x, y))) { safe = false; break; }
                if (!IsCornerSafe(corners, touchedThisPass, terrain, x, y))
                    safe = false;
            }
            if (!safe) return false;

            for (var x = x0; x <= x1; x++)
            for (var y = y0; y <= y1; y++)
                corners.Heights[x, y] = RaiseDelta;

            var allResolve = true;
            for (var cx = padX0; cx <= padX1 - 1 && allResolve; cx++)
            for (var cy = padY0; cy <= padY1 - 1 && allResolve; cy++)
            {
                if (!CellResolves(corners, layout.Crossers, cache, cx, cy))
                    allResolve = false;
            }

            if (!allResolve)
            {
                for (var x = x0; x <= x1; x++)
                for (var y = y0; y <= y1; y++)
                    corners.Heights[x, y] = 0;
                return false;
            }

            for (var x = x0; x <= x1; x++)
            for (var y = y0; y <= y1; y++)
                touchedThisPass.Add((x, y));

            return true;
        }

        /// <summary>
        /// True when corner (x,y) currently qualifies to be part of this pass's footprint (raised
        /// rectangle OR its rim): the target terrain, still flat, and not already touched by an earlier
        /// region this same Paint() call. Does NOT check the "never raise" forbidden set
        /// (border/transition-anchor/room-center) -- that only applies to corners about to be RAISED,
        /// checked separately by the caller (see TryPlaceRectangle), since a rim corner is never raised
        /// and so never needs to avoid them.
        ///
        /// Deliberately does NOT pre-check crosser-bearing cells: every cell a RAISED (inner-rectangle)
        /// corner touches is, by construction, inside the padded footprint TryPlaceRectangle already
        /// re-verifies cell-by-cell via CellResolves -- a crosser-bearing cell there naturally fails
        /// that check (none of this pass's rim/interior candidates carry crossers) and reverts, so a
        /// duplicate hand-rolled check here would only add a false rejection: a PADDING corner can
        /// legitimately sit one step from a crosser-bearing cell that lies just OUTSIDE the footprint
        /// (e.g. a room's own outer boundary corner, next to its Tunnel-mode doorway) without that cell
        /// ever being read, modified, or affected -- padding corners are never raised, so their height
        /// never changes regardless of what neighbors them.
        /// </summary>
        private static bool IsCornerSafe(
            CornerTerrainGrid corners,
            HashSet<(int X, int Y)> touchedThisPass,
            string terrain, int x, int y)
        {
            if (corners.Labels[x, y] != terrain) return false;
            if (corners.Heights[x, y] != 0) return false;
            if (touchedThisPass.Contains((x, y))) return false;

            return true;
        }

        /// <summary>Re-derives cell (x,y)'s current 4-corner-terrain + normalized height-delta key and
        /// checks TileResolver.HasHeightAwareCandidate against it -- the same computation TryResolve
        /// itself performs per cell, used here purely as a pre-commit verification (no tile is actually
        /// picked or placed).</summary>
        internal static bool CellResolves(CornerTerrainGrid corners, EdgeCrosserGrid crossers, TileResolver.HeightAwareProbeCache cache, int x, int y)
        {
            var tl = corners.Labels[x, y + 1];
            var tr = corners.Labels[x + 1, y + 1];
            var br = corners.Labels[x + 1, y];
            var bl = corners.Labels[x, y];

            var top = crossers.GetEdge(x, y, EdgeSlot.Top);
            var right = crossers.GetEdge(x, y, EdgeSlot.Right);
            var bottom = crossers.GetEdge(x, y, EdgeSlot.Bottom);
            var left = crossers.GetEdge(x, y, EdgeSlot.Left);

            var hTl = corners.Heights[x, y + 1];
            var hTr = corners.Heights[x + 1, y + 1];
            var hBr = corners.Heights[x + 1, y];
            var hBl = corners.Heights[x, y];
            var min = Math.Min(Math.Min(hTl, hTr), Math.Min(hBr, hBl));

            return TileResolver.HasHeightAwareCandidate(
                cache, tl, tr, br, bl, top, right, bottom, left,
                hTl - min, hTr - min, hBr - min, hBl - min,
                min);
        }

        private static void Shuffle<T>(List<T> list, System.Random random)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
