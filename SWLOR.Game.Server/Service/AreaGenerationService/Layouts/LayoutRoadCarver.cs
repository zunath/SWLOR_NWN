using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.AreaGenerationService.Layouts
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
    /// Deliberately runs LAST in MacroLayoutGenerator's post-pass pipeline -- AFTER LayoutGroupStamper,
    /// not alongside LayoutAccentChannelCarver/LayoutFenceCarver immediately after transitions are
    /// anchored. A road never repaints corner terrain (it stays this composition's own OpenTerrain the
    /// whole time), so unlike a channel band it gets no automatic protection from LayoutGroupStamper's
    /// OpenSetPiece placement, which validates candidate footprints purely by corner-terrain/PinnedTiles/
    /// transition membership (see LayoutGroupStamper.IsOpenSetPieceSiteValid), never by scanning
    /// existing crosser edges the way its WallRoom/CorridorStub/CorridorInsert site checks do. Running
    /// after Stamp instead flips this into a benefit: LayoutGroupStamper's own PinnedTiles set becomes
    /// the "occupied by a building" signal a lane's path can consult and route around, producing the
    /// hand-built pattern directly -- streets threaded through the gaps BETWEEN stamped building
    /// footprints -- with zero extra bookkeeping and zero risk of a later stamp silently overwriting an
    /// already-carved lane's pinned/crosser state.
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
            // representative center tile -- both are guaranteed by this point (LayoutGroupStamper has
            // already run and relocates a room's CenterTile away from any footprint it stamps, and
            // OpenSetPiece/WallRoom/CorridorStub site validation always excludes transition tiles from
            // a stamped footprint) to be open, unpinned cells. Distinct + deterministically ordered
            // (transitions in their own list order, then rooms by Id) so the same seed always offers
            // the same anchor set in the same order.
            var anchors = layout.Transitions.Select(t => t.Tile)
                .Concat(layout.Rooms.OrderBy(r => r.Id).Select(r => r.CenterTile))
                .Distinct()
                .ToList();

            if (anchors.Count < 2) return;

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

                if (!TryBuildPath(corners, width, height, open, a, b, out var path)) continue;
                if (!IsPathClear(layout, crossers, road, path)) continue;

                CommitPath(crossers, road, path);
                placed++;
            }
        }

        /// <summary>
        /// Builds a right-angle (Manhattan) chain of cells from <paramref name="a"/> to
        /// <paramref name="b"/>: a horizontal run at a.Y from a.X to b.X, then a vertical run at b.X
        /// from a.Y to b.Y (degenerates to a single straight run when a.X == b.X or a.Y == b.Y).
        /// Fails when either anchor sits outside the corner-cell grid.
        /// </summary>
        private static bool TryBuildPath(
            CornerTerrainGrid corners, int width, int height, string open,
            (int X, int Y) a, (int X, int Y) b, out List<(int X, int Y)> path)
        {
            path = null;
            if (!InBounds(a, width, height) || !InBounds(b, width, height)) return false;

            var cells = new List<(int X, int Y)> { a };

            var stepX = b.X > a.X ? 1 : -1;
            for (var x = a.X + stepX; x != b.X + stepX; x += stepX)
                cells.Add((x, a.Y));

            var stepY = b.Y > a.Y ? 1 : -1;
            for (var y = a.Y + stepY; y != b.Y + stepY; y += stepY)
                cells.Add((b.X, y));

            // De-duplicate the corner cell shared by both runs when a.X == b.X or a.Y == b.Y (a
            // straight-line path never enters the second loop / re-adds the first cell).
            var deduped = new List<(int X, int Y)>();
            foreach (var cell in cells)
            {
                if (deduped.Count == 0 || deduped[^1] != cell)
                    deduped.Add(cell);
            }

            if (deduped.Count < 2) return false; // a == b, no lane to carve

            foreach (var cell in deduped)
            {
                if (!InBounds(cell, width, height)) return false;
                if (!LayoutCornerUtils.IsTileFullyOpen(corners, cell.X, cell.Y, open)) return false;
            }

            path = deduped;
            return true;
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
    }
}
