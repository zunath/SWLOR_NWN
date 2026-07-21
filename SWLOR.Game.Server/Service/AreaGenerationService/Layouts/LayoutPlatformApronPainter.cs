using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AreaGenerationService.Layouts
{
    /// <summary>
    /// Paints a paved PLATFORM APRON one margin-cell deep around the walkable grid on
    /// chasm-margin city compositions (see <see cref="MacroLayoutParameters.PlatformApron"/>):
    /// every margin cell that cardinally fronts an open room cell gets its remaining
    /// solid-terrain corners repainted to the fronted open terrain, so the platform surface
    /// extends under the structural frontage buildings later erected on those margins and the
    /// chasm drop begins BEYOND them.
    ///
    /// This is the hand-built composition (r16 support-evidence pass,
    /// _scratch_decor/r16_mine_support.py): fcx01 city areas are platform-dominant -- median
    /// "holes" chasm corner share ~0.17 with several flagship areas at 0.00 (pw_ar_narpromena's
    /// towers stand ON flat cobble, abyss beyond the rim) -- while ungated generated chasm-margin
    /// layouts ran 0.72, leaving deep towers nothing to stand on. With the apron, workhorse
    /// towers (build007/build004/kyru08, depths 11-20m) satisfy the mined footprint-support
    /// envelope (FrontageSupportRule) on interior street margins exactly as they do on hand-built
    /// paving; without it only shallow accents fit chasm-heavy margins and the building mix
    /// breaches the mined non-workhorse salience ceiling.
    ///
    /// Painting rules (deterministic, no RNG):
    ///  - only margin cells (not in any room) with at least one cardinal non-set-piece open room
    ///    neighbor -- the frontage anchor ring,
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

                // The apron label is what the cell's already-open corners carry -- deterministic
                // fixed corner order, first non-solid non-empty label wins (a cell fronting two
                // districts takes the first; both labels are real open paving either way).
                var cellCorners = new[] { (x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1) };
                string apronLabel = null;
                foreach (var (cx, cy) in cellCorners)
                {
                    var label = corners.Labels[cx, cy];
                    if (!string.IsNullOrEmpty(label) && label != parameters.SolidTerrain)
                    {
                        apronLabel = label;
                        break;
                    }
                }

                if (apronLabel == null)
                    continue;

                foreach (var (cx, cy) in cellCorners)
                {
                    if (corners.Labels[cx, cy] != parameters.SolidTerrain)
                        continue;
                    // Border corners stay solid: the generator's border invariant, and the rim
                    // drop hand-built skylines overhang.
                    if (cx == 0 || cy == 0 || cx == width || cy == height)
                        continue;
                    if (TouchesPinnedCell(cx, cy, layout))
                        continue;

                    corners.Labels[cx, cy] = apronLabel;
                }
            }
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
