#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// Shared accent-channel post-pass: carves straight, one-cell-wide bands of accent terrain
    /// (e.g. Water/Pit/Chasm "crossers") through open space, each spanned by exactly one real
    /// Bridge edge-crosser chain so the two straddled open regions stay connected. Complements
    /// LayoutAccentPainter's blob patches with a linear, always-crossable feature. Runs in the same
    /// post-pass slot, immediately after it, so both consult (and further constrain) the same
    /// pre-transition, pre-stamp corner grid — transitions and set pieces are assigned afterward and
    /// naturally avoid channel/bank cells because they are no longer fully open.
    ///
    /// Vocabulary this relies on (verified offline against tdt01/tds01/vmr01 .set data, see
    /// SWLOR.Toolset.Tests.AreaGeneration.BridgeChannelTests): flat, ungrouped tiles whose four
    /// corners are all the accent terrain and carry opposite-edge Bridge crossers (a straight span,
    /// pathnode L, connecting a Bridge edge on each side), plus tiles split half-accent/half-open
    /// terrain carrying a single Bridge edge on the accent side (a bank/landing tile, pathnode A).
    /// Every rotation of both physical tiles is available via TileResolver's orientation search, so a
    /// channel is always exactly: bank - span - bank (a "1-cell-wide" crossing), regardless of which
    /// axis it runs along. The Bridge chain is recorded as a TunnelLink (the same mechanism
    /// LayoutTunnelCarver uses for wall-embedded tunnels) so MacroLayoutGenerator.ValidateInvariants
    /// and role-assignment geodesics see it as a crossing even where the channel fully separates the
    /// two sides.
    ///
    /// v1 scope note: operates only on MacroLayoutParameters.OpenTerrain (the primary terrain), never
    /// SecondaryOpenTerrain -- ValidateBand requires every corner it reads to already equal the primary
    /// open terrain, so a multi-terrain district room's corners are naturally excluded with no extra
    /// guard needed.
    /// </summary>
    internal static class LayoutAccentChannelCarver
    {
        private const string BridgeCrosser = "Bridge";
        private const int MinLength = 3;
        private const int MaxLength = 6;
        private const int MaxAttempts = 500;

        internal static void CarveChannels(MacroLayout layout, MacroLayoutParameters parameters, TilesetModel tileset, System.Random random)
        {
            if (parameters.AccentChannels <= 0) return;

            // ChannelTerrain is the dedicated channel/bank terrain slot (falls back to AccentTerrain
            // when a tileset never declared one separately — see DungeonTilesetProfile.ChannelTerrain).
            var accent = !string.IsNullOrEmpty(parameters.ChannelTerrain) ? parameters.ChannelTerrain : parameters.AccentTerrain;
            if (string.IsNullOrEmpty(accent)) return;

            // Zero-config capability probe, mirroring LayoutFenceCarver's own tileset != null guard:
            // some onboarded tilesets configure a ChannelTerrain that has a verified Bridge-gated WALL
            // crossing (a CorridorInsert SetPiece like BridgeDoor01) but no crosser-free tile blending
            // the primary open terrain and the channel terrain -- an OPEN-SPACE crossing (what this
            // carver paints) can never resolve there (e.g. Ruins/tdr01's Chasm, see
            // BaseGameTilesetProfiles.Ruins). A room-and-corridor layout's thin, fragmented open space
            // already makes ValidateBand fail every attempt on such tilesets in practice (placed stays
            // 0, a silent no-op), but a blobbier style (OrganicCave) has plenty of open space for
            // ValidateBand to succeed and would otherwise commit a band TileResolver can never place a
            // tile for. tileset is optional (defaults to null for back-compat, matching
            // MacroLayoutGenerator's own convention) -- callers that never pass one keep the pre-check
            // blind-carving behavior exactly as before.
            if (tileset != null && !CanCarve(tileset, parameters.OpenTerrain, accent))
                return;

            var corners = layout.Corners;
            var open = parameters.OpenTerrain;

            var forbidden = new HashSet<(int X, int Y)>();
            foreach (var room in layout.Rooms)
            {
                var (cx, cy) = room.CenterTile;
                forbidden.Add((cx, cy));
                forbidden.Add((cx + 1, cy));
                forbidden.Add((cx, cy + 1));
                forbidden.Add((cx + 1, cy + 1));
            }

            var placed = 0;
            var attempts = 0;

            while (placed < parameters.AccentChannels && attempts < MaxAttempts)
            {
                attempts++;

                var horizontal = random.Next(2) == 0;
                var length = random.Next(MinLength, MaxLength + 1);

                if (horizontal)
                {
                    // Channel stretches along X (columns cx0..cx1), 1 cell row tall at row "cross",
                    // crossed by a north/south bridge at column "bridgeAt".
                    var maxCross = corners.Height - 3;
                    if (maxCross < 2) continue;
                    var cross = random.Next(2, maxCross + 1);

                    var maxCx0 = corners.Width - 2 - length;
                    if (maxCx0 < 1) continue;
                    var cx0 = random.Next(1, maxCx0 + 1);
                    var cx1 = cx0 + length - 1;

                    if (!ValidateBand(corners, open, forbidden, cx0, cx1, cross, isHorizontal: true)) continue;

                    var bridgeAt = random.Next(cx0, cx1 + 1);
                    if (TryCarveBand(layout, open, accent, cx0, cx1, cross, bridgeAt, isHorizontal: true))
                        placed++;
                }
                else
                {
                    // Channel stretches along Y (rows cy0..cy1), 1 cell column wide at column
                    // "cross", crossed by an east/west bridge at row "bridgeAt".
                    var maxCross = corners.Width - 3;
                    if (maxCross < 2) continue;
                    var cross = random.Next(2, maxCross + 1);

                    var maxCy0 = corners.Height - 2 - length;
                    if (maxCy0 < 1) continue;
                    var cy0 = random.Next(1, maxCy0 + 1);
                    var cy1 = cy0 + length - 1;

                    if (!ValidateBand(corners, open, forbidden, cy0, cy1, cross, isHorizontal: false)) continue;

                    var bridgeAt = random.Next(cy0, cy1 + 1);
                    if (TryCarveBand(layout, open, accent, cy0, cy1, cross, bridgeAt, isHorizontal: false))
                        placed++;
                }
            }

            LayoutCornerUtils.RecomputeFullyOpenRoomTiles(layout, open);
        }

        /// <summary>
        /// Whole-tileset capability probe: true only when the tileset can resolve BOTH physical shapes
        /// a channel needs -- mirrors LayoutFenceCarver.CarveFencesForTerrain's own hasStraightRun/
        /// hasEndCap pair. TileResolver's rotation search means one representative orientation of each
        /// shape stands in for all four a channel can need (horizontal or vertical, either bank side):
        /// a span tile rotated 90 degrees moves its Bridge pair from Top/Bottom to Left/Right, and a
        /// bank tile rotated 180 degrees swaps which half is accent, so checking one orientation each
        /// is sufficient, not a partial check.
        /// </summary>
        private static bool CanCarve(TilesetModel tileset, string open, string accent)
        {
            // Span: a full channel cell -- all four corners accent, opposite-edge Bridge crossers (the
            // straight "pathnode L" span both bank tiles land against).
            var hasSpan = TileResolver.HasCandidate(
                tileset, accent, accent, accent, accent, BridgeCrosser, string.Empty, BridgeCrosser, string.Empty);

            // Bank: a landing tile split half-open/half-accent along one axis, with a single Bridge
            // edge on the accent side.
            var hasBank = TileResolver.HasCandidate(
                tileset, accent, accent, open, open, BridgeCrosser, string.Empty, string.Empty, string.Empty);

            return hasSpan && hasBank;
        }

        /// <summary>
        /// Validates a candidate 1-cell-wide band before any grid mutation. "along0..along1" are the
        /// long-axis cell coordinates the band spans; "cross" is the cross-axis cell coordinate the
        /// band sits at (its two painted corner rows/columns are "cross" and "cross + 1"; "cross - 1"
        /// and "cross + 2" are the untouched bank-side corner rows/columns, which must already be
        /// open so the resulting bank tiles are the verified half-accent/half-open shape). Every
        /// corner touched or read must currently be open terrain, and none of the corners in the two
        /// painted rows/columns may belong to a room's center tile (forbidden), which transitively
        /// protects any bank cell that happens to be a room's center too.
        /// </summary>
        private static bool ValidateBand(
            CornerTerrainGrid corners, string open, HashSet<(int X, int Y)> forbidden,
            int along0, int along1, int cross, bool isHorizontal)
        {
            for (var a = along0; a <= along1 + 1; a++)
            {
                for (var c = cross - 1; c <= cross + 2; c++)
                {
                    var (x, y) = isHorizontal ? (a, c) : (c, a);
                    if (x < 0 || x > corners.Width || y < 0 || y > corners.Height) return false;
                    if (corners.Labels[x, y] != open) return false;

                    if ((c == cross || c == cross + 1) && forbidden.Contains((x, y))) return false;
                }
            }

            // Every corner the band touches or reads was just confirmed to be plain open terrain, so
            // the two edges the bridge will claim (bank->channel on each side) were never touched by
            // an earlier post-pass either — OpenLane corridors never set crossers, and a prior
            // channel attempt would have failed the open-terrain check above on overlap.
            return true;
        }

        /// <summary>
        /// Paints the band and adds its Bridge chain + TunnelLink, then verifies the single new link
        /// actually restores full open-corner connectivity. A straight band drawn through an irregular
        /// shape (OrganicCave/Warren blobs, not a plain rectangle) can sever it into more than the two
        /// pieces a lone link bridges — e.g. the open region loops back across the band's line at a
        /// second point — so this tentatively commits and reverts (corners, both crosser edges, and
        /// the link) exactly like LayoutAccentPainter's blob-growth commit/revert when the result
        /// isn't fully connected. Returns true only on a kept placement.
        /// </summary>
        private static bool TryCarveBand(
            MacroLayout layout, string open, string accent,
            int along0, int along1, int cross, int bridgeAt, bool isHorizontal)
        {
            var corners = layout.Corners;
            var crossers = layout.Crossers;

            for (var a = along0; a <= along1 + 1; a++)
            {
                for (var c = cross; c <= cross + 1; c++)
                {
                    var (x, y) = isHorizontal ? (a, c) : (c, a);
                    corners.Labels[x, y] = accent;
                }
            }

            if (isHorizontal)
            {
                // South bank's Top edge = channel cell's Bottom edge; channel cell's Top edge =
                // north bank's Bottom edge. Two SetEdge calls claim both shared boundaries.
                crossers.SetEdge(bridgeAt, cross - 1, EdgeSlot.Top, BridgeCrosser);
                crossers.SetEdge(bridgeAt, cross, EdgeSlot.Top, BridgeCrosser);
            }
            else
            {
                // West bank's Right edge = channel cell's Left edge; channel cell's Right edge =
                // east bank's Left edge.
                crossers.SetEdge(cross - 1, bridgeAt, EdgeSlot.Right, BridgeCrosser);
                crossers.SetEdge(cross, bridgeAt, EdgeSlot.Right, BridgeCrosser);
            }

            var link = new TunnelLink
            {
                CornerA = isHorizontal ? (bridgeAt, cross - 1) : (cross - 1, bridgeAt),
                CornerB = isHorizontal ? (bridgeAt, cross + 2) : (cross + 2, bridgeAt),
                Length = 3
            };
            layout.TunnelLinks.Add(link);

            if (LayoutCornerUtils.IsConnectedWithLinks(corners, open, layout.TunnelLinks))
                return true;

            // Revert: this band's single bridge wasn't enough to restore connectivity (the band cut
            // an irregular open shape into more than two pieces) — undo every mutation and let the
            // caller retry a different placement.
            layout.TunnelLinks.Remove(link);

            for (var a = along0; a <= along1 + 1; a++)
            {
                for (var c = cross; c <= cross + 1; c++)
                {
                    var (x, y) = isHorizontal ? (a, c) : (c, a);
                    corners.Labels[x, y] = open;
                }
            }

            if (isHorizontal)
            {
                crossers.SetEdge(bridgeAt, cross - 1, EdgeSlot.Top, string.Empty);
                crossers.SetEdge(bridgeAt, cross, EdgeSlot.Top, string.Empty);
            }
            else
            {
                crossers.SetEdge(cross - 1, bridgeAt, EdgeSlot.Right, string.Empty);
                crossers.SetEdge(cross, bridgeAt, EdgeSlot.Right, string.Empty);
            }

            return false;
        }
    }
}
