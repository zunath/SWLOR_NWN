#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// Shared accent post-pass: paints a secondary terrain (e.g. Water pools, Pit channels) as random-walk
    /// blob patches strictly inside open space, subject to hard constraints (never touches solid, never
    /// eats a room's center tile, never breaks open-corner connectivity). Runs last, after role assignment.
    ///
    /// v1 scope note: operates only on MacroLayoutParameters.OpenTerrain (the primary terrain), never
    /// MacroLayoutParameters.SecondaryOpenTerrain -- a multi-terrain district room's corners never equal
    /// OpenTerrain, so CanAccept naturally excludes them with no extra guard needed; districted layouts
    /// simply never paint accents into a secondary room.
    /// </summary>
    internal static class LayoutAccentPainter
    {
        private const int MaxAttempts = 3000;

        internal static void PaintAccents(MacroLayout layout, MacroLayoutParameters parameters, System.Random random)
        {
            if (string.IsNullOrEmpty(parameters.AccentTerrain) || parameters.AccentDensity <= 0)
                return;

            var corners = layout.Corners;
            var openTerrain = parameters.OpenTerrain;
            var solidTerrain = parameters.SolidTerrain;
            var accentTerrain = parameters.AccentTerrain;

            var forbidden = new HashSet<(int X, int Y)>();
            foreach (var room in layout.Rooms)
            {
                var (cx, cy) = room.CenterTile;
                forbidden.Add((cx, cy));
                forbidden.Add((cx + 1, cy));
                forbidden.Add((cx, cy + 1));
                forbidden.Add((cx + 1, cy + 1));
            }

            var initialOpenCount = LayoutCornerUtils.GetCorners(corners, openTerrain).Count;
            var targetAccentCount = (int)Math.Round(parameters.AccentDensity * initialOpenCount);
            if (targetAccentCount <= 0) return;

            var accentedCount = 0;
            var attempts = 0;

            while (accentedCount < targetAccentCount && attempts < MaxAttempts)
            {
                attempts++;

                var openCorners = LayoutCornerUtils.GetCorners(corners, openTerrain)
                    .Where(c => !forbidden.Contains(c))
                    .ToList();

                if (openCorners.Count == 0) break;

                var seed = openCorners[random.Next(openCorners.Count)];
                if (!CanAccept(corners, openTerrain, solidTerrain, forbidden, seed)) continue;

                var blobTarget = random.Next(3, 9);
                var blob = GrowBlob(corners, openTerrain, solidTerrain, forbidden, seed, blobTarget, random);
                if (blob.Count == 0) continue;

                foreach (var c in blob)
                    corners.Labels[c.X, c.Y] = accentTerrain;

                if (LayoutCornerUtils.IsSingleComponent(corners, openTerrain))
                {
                    accentedCount += blob.Count;
                }
                else
                {
                    foreach (var c in blob)
                        corners.Labels[c.X, c.Y] = openTerrain;
                }
            }

            LayoutCornerUtils.RecomputeFullyOpenRoomTiles(layout, openTerrain);
        }

        private static bool CanAccept(
            CornerTerrainGrid corners, string openTerrain, string solidTerrain,
            HashSet<(int X, int Y)> forbidden, (int X, int Y) corner)
        {
            if (corners.Labels[corner.X, corner.Y] != openTerrain) return false;
            if (forbidden.Contains(corner)) return false;

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    var nx = corner.X + dx;
                    var ny = corner.Y + dy;
                    if (nx < 0 || nx > corners.Width || ny < 0 || ny > corners.Height) continue;
                    if (corners.Labels[nx, ny] == solidTerrain) return false;
                }
            }

            return true;
        }

        private static List<(int X, int Y)> GrowBlob(
            CornerTerrainGrid corners, string openTerrain, string solidTerrain,
            HashSet<(int X, int Y)> forbidden, (int X, int Y) seed, int targetSize, System.Random random)
        {
            var blob = new List<(int X, int Y)>();
            var queued = new HashSet<(int X, int Y)> { seed };
            var frontier = new List<(int X, int Y)> { seed };

            while (blob.Count < targetSize && frontier.Count > 0)
            {
                var index = random.Next(frontier.Count);
                var current = frontier[index];
                frontier.RemoveAt(index);

                if (!CanAccept(corners, openTerrain, solidTerrain, forbidden, current)) continue;

                blob.Add(current);

                foreach (var (dx, dy) in LayoutCornerUtils.Ortho4)
                {
                    var next = (current.X + dx, current.Y + dy);
                    if (!queued.Add(next)) continue;
                    frontier.Add(next);
                }
            }

            return blob;
        }
    }
}
