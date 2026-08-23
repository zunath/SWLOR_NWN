#nullable disable
using System;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Shared tile-local -&gt; world-space transform math and small corner/edge helpers used by both
    /// TileDoorPlanner (Door-style transitions) and GroupExitPlanner (GroupExit-style transitions),
    /// so the rotation/translation/bearing formula is pinned in exactly one place — empirically
    /// verified against hand-built module doors by
    /// TileDoorPlannerTests.DoorWorldTransform_MatchesHandBuiltModuleDoors.
    /// </summary>
    internal static class TileDoorGeometry
    {
        /// <summary>
        /// Rotates a tile-local (origin at tile center, range roughly [-5, 5]) point by
        /// orientation * 90 degrees counterclockwise, using exact swaps/negations (orientation is
        /// always a 90-degree multiple, so trig would only introduce needless floating-point error).
        /// Matches the world-transform empirically pinned against hand-built module doors: for a
        /// tile at grid cell (cx, cy) with this orientation, a raw door local (x, y) lands at world
        /// (cx*10 + 5 + rx, cy*10 + 5 + ry).
        /// </summary>
        internal static (float X, float Y) RotateCcw90Multiple(float x, float y, int orientation)
        {
            return ((orientation % 4 + 4) % 4) switch
            {
                0 => (x, y),
                1 => (-y, x),
                2 => (-x, -y),
                3 => (y, -x),
                _ => (x, y)
            };
        }

        internal static float NormalizeDegrees(float degrees)
        {
            var d = degrees % 360f;
            if (d > 180f) d -= 360f;
            if (d <= -180f) d += 360f;
            return d;
        }

        /// <summary>
        /// World position/bearing of a tile-local door slot sitting in a resolved tile at grid cell
        /// (cellX, cellY) with the given orientation. Position = cell center (cellX*10+5, cellY*10+5)
        /// + the slot's local (X, Y) rotated orientation*90 degrees CCW. Bearing = slot.Orientation +
        /// orientation*90, normalized to (-180, 180].
        /// </summary>
        internal static (float X, float Y, float Z, float Orientation) DoorWorldTransform(
            TileDoorRecord slot, int cellX, int cellY, int orientation)
        {
            var (rx, ry) = RotateCcw90Multiple(slot.X, slot.Y, orientation);
            var x = cellX * 10f + 5f + rx;
            var y = cellY * 10f + 5f + ry;
            var bearing = NormalizeDegrees(slot.Orientation + orientation * 90f);
            return (x, y, slot.Z, bearing);
        }

        internal static (string TL, string TR, string BR, string BL) CellCorners(CornerTerrainGrid corners, int x, int y)
        {
            return (
                corners.Labels[x, y + 1],
                corners.Labels[x + 1, y + 1],
                corners.Labels[x + 1, y],
                corners.Labels[x, y]);
        }

        internal static bool HasAnyCrosserEdge(EdgeCrosserGrid crossers, (int X, int Y) cell)
        {
            for (var slot = 0; slot < 4; slot++)
            {
                if (crossers.GetEdge(cell.X, cell.Y, slot).Length != 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// True when all 4 corners of tile cell (x, y) carry height 0 in the layout's corner-height
        /// grid. Used to defensively gate structural passes (door/exit planners, set-piece stamping,
        /// room-membership) to flat space: no layout style paints CornerTerrainGrid.Heights yet, so
        /// this is always true today, but keeps those passes from ever silently mismatching a raised
        /// cell once a future layout style starts painting elevation.
        /// </summary>
        internal static bool IsFlatCell(CornerTerrainGrid corners, int x, int y)
        {
            return corners.Heights[x, y] == 0 &&
                   corners.Heights[x + 1, y] == 0 &&
                   corners.Heights[x, y + 1] == 0 &&
                   corners.Heights[x + 1, y + 1] == 0;
        }

        internal static bool Eq(string a, string b) => string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
