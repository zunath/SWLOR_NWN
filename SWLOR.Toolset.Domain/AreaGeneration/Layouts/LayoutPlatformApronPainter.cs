#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// Paints a paved PLATFORM APRON <see cref="ApronDepth"/> margin-cells deep around the
    /// walkable grid on chasm-margin city compositions (see
    /// <see cref="MacroLayoutParameters.PlatformApron"/>): every margin cell within that many
    /// cardinal margin steps of an open room cell gets its remaining solid-terrain corners
    /// repainted to the fronted open terrain, so the platform surface extends UNDER the
    /// structural frontage buildings later erected on those margins -- their full footprints,
    /// not just their anchor cells -- and the chasm drop begins BEYOND them.
    ///
    /// This is the hand-built composition from the support-evidence audit: fcx01 city areas are
    /// platform-dominant -- median
    /// "holes" chasm corner share ~0.17 with several flagship areas at 0.00 (pw_ar_narpromena's
    /// towers stand ON flat cobble, abyss beyond the rim) -- while ungated generated chasm-margin
    /// layouts ran 0.72, leaving deep towers nothing to stand on. With the apron, workhorse
    /// towers (build007/build004/kyru08, depths 11-20m) satisfy the footprint-support envelope
    /// (FrontageSupportRule) on interior street margins exactly as they do on hand-built paving;
    /// without it only shallow accents fit chasm-heavy margins and the building mix breaches the
    /// non-workhorse salience ceiling.
    ///
    /// The DEPTH is what makes the zero-overhang support envelope satisfiable (street-coherence
    /// override -- see FrontageSupportRule): at depth 1 a deep workhorse tower
    /// (build004, 20.3m) standing on the ring still hung its back half over the abyss, which the
    /// removed moat/overhang tolerances used to forgive. At depth 2 the painted corner rows reach
    /// 20m out from the open boundary, whose quadrants cover the footprint of every model up to
    /// ~27m deep -- paving under the building by construction, uniformly along the whole frontage
    /// band so the ring reads as one consistent paved deck rather than per-building pads.
    ///
    /// Painting rules (deterministic, no RNG):
    ///  - only margin cells (not in any room) reachable from a cardinal non-set-piece open room
    ///    neighbor within <see cref="ApronDepth"/> margin steps -- the frontage anchor band,
    ///  - never a cell pinned by LayoutGroupStamper (a stamped group's corner plan is part of its
    ///    footprint contract); transition-adjacent cells ARE paved -- fcx01's whole murs
    ///    door-slot vocabulary is uniformly Cobble/Cobble2-cornered, so door slots resolve at
    ///    least as well on paving, and unpaved doorside slots rejected every deep workhorse tower,
    ///  - only corners whose label is the layout's SolidTerrain (chasm) -- district, accent,
    ///    channel, and feature labels stay untouched,
    ///  - never a grid-border corner (the area rim stays SolidTerrain, preserving both the
    ///    generator's border invariant and the rim drop that hand-built skylines overhang),
    ///  - never a corner touching a pinned cell (a stamped group tile's corner plan is part of
    ///    its footprint contract),
    ///  - the repaint label is the terrain the cell's already-open corners carry (the fronted
    ///    room's own terrain, so Cobble2 district streets pave Cobble2 aprons).
    /// Runs after every other terrain/height/road/stamp pass so it can see pinned tiles and road
    /// edges; corner heights are left untouched (the apron extends the surface, not the relief).
    /// </summary>
    public static class LayoutPlatformApronPainter
    {
        /// <summary>How many margin cells deep the paved apron band extends from the open-room
        /// boundary -- deep enough that a frontage building's WHOLE footprint stands on painted
        /// platform under the zero-overhang support envelope (see the class doc comment).</summary>
        public const int ApronDepth = 2;

        private static readonly (int Dx, int Dy)[] CardinalDirections =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1)
        };

        public static void Paint(MacroLayout layout, MacroLayoutParameters parameters)
        {
            if (!parameters.PlatformApron || string.IsNullOrEmpty(parameters.SolidTerrain))
                return;

            var corners = layout.Corners;
            var width = corners.Width;
            var height = corners.Height;

            var openCells = new HashSet<(int X, int Y)>();
            var allRoomCells = new HashSet<(int X, int Y)>();
            foreach (var room in layout.Rooms)
            foreach (var tile in room.Tiles)
            {
                allRoomCells.Add(tile);
                if (!room.IsSetPiece)
                    openCells.Add(tile);
            }

            // Pinned (stamped-group) cells keep their own corner plan; transition-adjacent margin
            // cells are deliberately NOT blocked -- fcx01's entire murs door-slot vocabulary is
            // uniformly Cobble/Cobble2-cornered (no door tile carries a chasm corner at all), so
            // paving the door cell's corners can only improve door-slot resolution, and leaving
            // those cells unpaved made every doorside frontage slot reject deep workhorse towers
            // (the support rule saw a bare 5m lip) and drift the building mix toward accents.
            var blocked = new HashSet<(int X, int Y)>(layout.PinnedTiles.Keys);

            // Deterministic outward dilation, one ring at a time: ring 1 is every margin cell
            // cardinally fronting an open room cell; ring n+1 is every margin cell cardinally
            // touching ring n. Each cell's apron label propagates from what it fronts -- ring-1
            // cells read their own already-open corners (the fronted room's paving), deeper rings
            // inherit the label of the ring cell that reached them first (fixed scan order, so a
            // cell reachable from two districts takes a deterministic one; both labels are real
            // open paving either way).
            var ringLabels = new Dictionary<(int X, int Y), string>();
            // Discovery-ordered list so the repaint loop below never depends on dictionary
            // enumeration order (two adjacent apron cells with different district labels share
            // corners; the writer order must be deterministic).
            var orderedCells = new List<(int X, int Y)>();
            var currentRing = new List<(int X, int Y)>();

            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var cell = (X: x, Y: y);
                if (allRoomCells.Contains(cell) || blocked.Contains(cell))
                    continue;

                var fronts = false;
                foreach (var (dx, dy) in CardinalDirections)
                {
                    if (openCells.Contains((x + dx, y + dy)))
                    {
                        fronts = true;
                        break;
                    }
                }

                if (!fronts)
                    continue;

                // The ring-1 apron label is what the cell's already-open corners carry --
                // deterministic fixed corner order, first non-solid non-empty label wins.
                var label = OwnOpenLabel(cell, corners, parameters.SolidTerrain);
                if (label == null)
                    continue;

                ringLabels[cell] = label;
                orderedCells.Add(cell);
                currentRing.Add(cell);
            }

            for (var depth = 2; depth <= ApronDepth && currentRing.Count > 0; depth++)
            {
                var nextRing = new List<(int X, int Y)>();
                foreach (var cell in currentRing)
                foreach (var (dx, dy) in CardinalDirections)
                {
                    var neighbor = (X: cell.X + dx, Y: cell.Y + dy);
                    if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= width || neighbor.Y >= height)
                        continue;
                    if (allRoomCells.Contains(neighbor) || blocked.Contains(neighbor))
                        continue;
                    if (ringLabels.ContainsKey(neighbor))
                        continue;

                    ringLabels[neighbor] = ringLabels[cell];
                    orderedCells.Add(neighbor);
                    nextRing.Add(neighbor);
                }

                currentRing = nextRing;
            }

            foreach (var cell in orderedCells)
            {
                var apronLabel = ringLabels[cell];
                var cellCorners = new List<(int X, int Y)>
                {
                    (cell.X, cell.Y), (cell.X + 1, cell.Y), (cell.X, cell.Y + 1), (cell.X + 1, cell.Y + 1)
                };

                // Pinned cells can block the two shared corners between adjacent apron rings.
                // Paint only the portion of this cell that can reach an already-open corner of the
                // same paving label; otherwise a far corner could become an isolated open island.
                var connected = new HashSet<(int X, int Y)>(
                    cellCorners.Where(corner => corners.Labels[corner.X, corner.Y] == apronLabel));
                var remaining = cellCorners
                    .Where(corner => CanPaintCorner(
                        corner.X,
                        corner.Y,
                        corners,
                        parameters.SolidTerrain,
                        width,
                        height,
                        layout))
                    .ToList();

                var painted = true;
                while (painted && remaining.Count > 0)
                {
                    painted = false;
                    for (var index = remaining.Count - 1; index >= 0; index--)
                    {
                        var corner = remaining[index];
                        if (!connected.Any(existing =>
                                Math.Abs(existing.X - corner.X) + Math.Abs(existing.Y - corner.Y) == 1))
                        {
                            continue;
                        }

                        corners.Labels[corner.X, corner.Y] = apronLabel;
                        connected.Add(corner);
                        remaining.RemoveAt(index);
                        painted = true;
                    }
                }
            }
        }

        private static bool CanPaintCorner(
            int cx,
            int cy,
            CornerTerrainGrid corners,
            string solidTerrain,
            int width,
            int height,
            MacroLayout layout)
        {
            return corners.Labels[cx, cy] == solidTerrain &&
                   cx != 0 && cy != 0 && cx != width && cy != height &&
                   !TouchesPinnedCell(cx, cy, layout);
        }

        /// <summary>The first non-solid, non-empty label among a cell's corners in fixed order --
        /// the paving the cell already fronts; null when every corner is solid/empty.</summary>
        private static string OwnOpenLabel(
            (int X, int Y) cell, CornerTerrainGrid corners, string solidTerrain)
        {
            var cellCorners = new[]
            {
                (cell.X, cell.Y), (cell.X + 1, cell.Y), (cell.X, cell.Y + 1), (cell.X + 1, cell.Y + 1)
            };
            foreach (var (cx, cy) in cellCorners)
            {
                var label = corners.Labels[cx, cy];
                if (!string.IsNullOrEmpty(label) && label != solidTerrain)
                    return label;
            }

            return null;
        }

        /// <summary>A corner touches a pinned cell when any of its up-to-4 adjacent cells is
        /// pinned -- repainting it would contradict the stamped group's own corner plan.</summary>
        private static bool TouchesPinnedCell(int cx, int cy, MacroLayout layout)
        {
            for (var dx = -1; dx <= 0; dx++)
            for (var dy = -1; dy <= 0; dy++)
            {
                if (layout.PinnedTiles.ContainsKey((cx + dx, cy + dy)))
                    return true;
            }

            return false;
        }
    }
}
