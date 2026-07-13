using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AreaGenerationService.Layouts
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
    /// needed). tde01 additionally has "Ramp" edge-crosser tiles and 1x1-GROUPed "Ramp -
    /// Straight/Corner, *" pieces that could connect two DIFFERENT terrains or richer rim variety --
    /// deliberately NOT used here (v1 scope: corner-blend rim only, no crosser plumbing, no group
    /// stamping) since the six plain ungrouped tiles already give full rectangular-blob coverage
    /// without them. A future pass could extend this to consume the Ramp crosser/group vocabulary for
    /// wider or cross-terrain rims.
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

            var forbidden = BuildForbiddenCorners(layout, corners);
            var touchedThisPass = new HashSet<(int X, int Y)>();

            var painted = 0;
            var attempts = 0;
            var maxAttempts = parameters.ElevationRegions * MaxAttemptsPerRegion;

            while (painted < parameters.ElevationRegions && attempts < maxAttempts)
            {
                attempts++;

                if (solidRimOk && TryPaintSolidBlob(layout, tileset, parameters.SolidTerrain, forbidden, touchedThisPass, random))
                {
                    painted++;
                    continue;
                }

                if (openRimOk && TryPaintOpenRoomBlob(layout, tileset, parameters.OpenTerrain, forbidden, touchedThisPass, random))
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
            MacroLayout layout, TilesetModel tileset, string terrain,
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

                if (TryPlaceRectangle(layout, tileset, terrain, forbidden, touchedThisPass, x0, y0, x0 + spanX, y0 + spanY))
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
            MacroLayout layout, TilesetModel tileset, string terrain,
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

                for (var placementAttempt = 0; placementAttempt < RoomPlacementAttempts; placementAttempt++)
                {
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

                    if (TryPlaceRectangle(layout, tileset, terrain, forbidden, touchedThisPass, x0, y0, x0 + spanX, y0 + spanY))
                        return true;
                }
            }

            return false;
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
        private static bool TryPlaceRectangle(
            MacroLayout layout, TilesetModel tileset, string terrain,
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
                if (!CellResolves(corners, layout.Crossers, tileset, cx, cy))
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
        private static bool CellResolves(CornerTerrainGrid corners, EdgeCrosserGrid crossers, TilesetModel tileset, int x, int y)
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
                tileset, tl, tr, br, bl, top, right, bottom, left,
                hTl - min, hTr - min, hBr - min, hBl - min);
        }
    }
}
