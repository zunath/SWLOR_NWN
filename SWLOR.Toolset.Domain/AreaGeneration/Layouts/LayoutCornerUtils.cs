#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// Corner-grid helpers shared by every layout style and the shared post-passes
    /// (role assignment, accent painting, final validation).
    /// </summary>
    internal static class LayoutCornerUtils
    {
        internal static readonly (int Dx, int Dy)[] Ortho4 =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1)
        };

        internal static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        internal static List<(int X, int Y)> GetCorners(CornerTerrainGrid corners, string label)
        {
            return GetCorners(corners, new HashSet<string> { label });
        }

        /// <summary>Multi-label variant: every corner whose terrain is any of <paramref name="labels"/>.</summary>
        internal static List<(int X, int Y)> GetCorners(CornerTerrainGrid corners, HashSet<string> labels)
        {
            var result = new List<(int X, int Y)>();
            for (var x = 0; x <= corners.Width; x++)
            {
                for (var y = 0; y <= corners.Height; y++)
                {
                    if (labels.Contains(corners.Labels[x, y]))
                        result.Add((x, y));
                }
            }

            return result;
        }

        /// <summary>
        /// The set of terrain labels a layout treats as "open" for district-aware connectivity/geodesic
        /// passes: OpenTerrain always, plus SecondaryOpenTerrain when districts are configured (see
        /// MacroLayoutParameters.SecondaryOpenTerrain). Single-label callers (accent painting, accent
        /// channels, fences) intentionally keep using the plain string overloads below — those systems
        /// are v1-scoped to the primary terrain only.
        /// </summary>
        internal static HashSet<string> OpenLabelSet(MacroLayoutParameters parameters)
        {
            var labels = new HashSet<string> { parameters.OpenTerrain };
            if (!string.IsNullOrEmpty(parameters.SecondaryOpenTerrain))
                labels.Add(parameters.SecondaryOpenTerrain);
            return labels;
        }

        internal static HashSet<(int X, int Y)> FloodFill(CornerTerrainGrid corners, string label, (int X, int Y) start)
        {
            var visited = new HashSet<(int X, int Y)> { start };
            var queue = new Queue<(int X, int Y)>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                foreach (var (dx, dy) in Ortho4)
                {
                    var nx = x + dx;
                    var ny = y + dy;
                    if (nx < 0 || nx > corners.Width || ny < 0 || ny > corners.Height) continue;
                    if (corners.Labels[nx, ny] != label) continue;
                    if (!visited.Add((nx, ny))) continue;

                    queue.Enqueue((nx, ny));
                }
            }

            return visited;
        }

        /// <summary>True when every corner labeled <paramref name="label"/> is reachable from every other.</summary>
        internal static bool IsSingleComponent(CornerTerrainGrid corners, string label)
        {
            var all = GetCorners(corners, label);
            if (all.Count == 0) return false;

            var reachable = FloodFill(corners, label, all[0]);
            return reachable.Count == all.Count;
        }

        internal static Dictionary<(int X, int Y), int> BfsDistances(CornerTerrainGrid corners, string label, (int X, int Y) start)
        {
            return BfsDistances(corners, new HashSet<string> { label }, start);
        }

        /// <summary>Multi-label variant: traverses any corner whose terrain is in <paramref name="labels"/>.</summary>
        internal static Dictionary<(int X, int Y), int> BfsDistances(CornerTerrainGrid corners, HashSet<string> labels, (int X, int Y) start)
        {
            var dist = new Dictionary<(int X, int Y), int> { [start] = 0 };
            var queue = new Queue<(int X, int Y)>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var d = dist[current];

                foreach (var (dx, dy) in Ortho4)
                {
                    var nx = current.X + dx;
                    var ny = current.Y + dy;
                    if (nx < 0 || nx > corners.Width || ny < 0 || ny > corners.Height) continue;
                    if (!labels.Contains(corners.Labels[nx, ny])) continue;

                    var key = (nx, ny);
                    if (dist.ContainsKey(key)) continue;

                    dist[key] = d + 1;
                    queue.Enqueue(key);
                }
            }

            return dist;
        }

        /// <summary>
        /// Geodesic distances over open corners, additionally traversing tunnel links (weighted
        /// edges between their endpoint corners). Falls back to plain BFS behavior when
        /// <paramref name="links"/> is empty. Dijkstra because links carry integer lengths > 1.
        /// </summary>
        internal static Dictionary<(int X, int Y), int> DistancesWithLinks(
            CornerTerrainGrid corners,
            string label,
            (int X, int Y) start,
            IReadOnlyList<TunnelLink> links)
        {
            return DistancesWithLinks(corners, new HashSet<string> { label }, start, links);
        }

        /// <summary>
        /// Multi-label variant of <see cref="DistancesWithLinks(CornerTerrainGrid,string,(int,int),IReadOnlyList{TunnelLink})"/>:
        /// treats any corner whose terrain is in <paramref name="labels"/> as walkable. Used by
        /// district-aware passes (role assignment boss geodesics, ValidateInvariants) so a secondary
        /// district's own open corners count as reachable space alongside the primary terrain.
        /// </summary>
        internal static Dictionary<(int X, int Y), int> DistancesWithLinks(
            CornerTerrainGrid corners,
            HashSet<string> labels,
            (int X, int Y) start,
            IReadOnlyList<TunnelLink> links)
        {
            if (links == null || links.Count == 0)
                return BfsDistances(corners, labels, start);

            // Corner -> (other endpoint, length) adjacency contributed by tunnel links.
            var linkEdges = new Dictionary<(int X, int Y), List<((int X, int Y) To, int Length)>>();
            foreach (var link in links)
            {
                if (!linkEdges.TryGetValue(link.CornerA, out var fromA))
                    linkEdges[link.CornerA] = fromA = new List<((int X, int Y), int)>();
                fromA.Add((link.CornerB, link.Length));

                if (!linkEdges.TryGetValue(link.CornerB, out var fromB))
                    linkEdges[link.CornerB] = fromB = new List<((int X, int Y), int)>();
                fromB.Add((link.CornerA, link.Length));
            }

            var dist = new Dictionary<(int X, int Y), int> { [start] = 0 };
            var queue = new PriorityQueue<(int X, int Y), int>();
            queue.Enqueue(start, 0);

            while (queue.TryDequeue(out var current, out var d))
            {
                if (dist[current] < d) continue;

                void Relax((int X, int Y) next, int cost)
                {
                    var nd = d + cost;
                    if (dist.TryGetValue(next, out var known) && known <= nd) return;
                    dist[next] = nd;
                    queue.Enqueue(next, nd);
                }

                foreach (var (dx, dy) in Ortho4)
                {
                    var nx = current.X + dx;
                    var ny = current.Y + dy;
                    if (nx < 0 || nx > corners.Width || ny < 0 || ny > corners.Height) continue;
                    if (!labels.Contains(corners.Labels[nx, ny])) continue;
                    Relax((nx, ny), 1);
                }

                if (linkEdges.TryGetValue(current, out var throughTunnels))
                {
                    foreach (var (to, length) in throughTunnels)
                    {
                        if (to.X < 0 || to.X > corners.Width || to.Y < 0 || to.Y > corners.Height)
                            continue;
                        if (!labels.Contains(corners.Labels[to.X, to.Y]))
                            continue;
                        Relax(to, Math.Max(1, length));
                    }
                }
            }

            return dist;
        }

        /// <summary>
        /// True when every corner labeled <paramref name="label"/> is reachable from every other,
        /// counting tunnel links as connections between their endpoint corners.
        /// </summary>
        internal static bool IsConnectedWithLinks(CornerTerrainGrid corners, string label, IReadOnlyList<TunnelLink> links)
        {
            return IsConnectedWithLinks(corners, new HashSet<string> { label }, links);
        }

        /// <summary>
        /// Multi-label variant: true when every corner whose terrain is in <paramref name="labels"/> is
        /// reachable from every other, counting tunnel links as connections between their endpoint
        /// corners. A districted layout's primary and secondary open corners are disjoint components in
        /// the plain corner graph (they only ever touch through a Tunnel-mode TunnelLink), so this is
        /// the check ValidateInvariants/role assignment must use once SecondaryOpenTerrain is active.
        /// </summary>
        internal static bool IsConnectedWithLinks(CornerTerrainGrid corners, HashSet<string> labels, IReadOnlyList<TunnelLink> links)
        {
            var all = GetCorners(corners, labels);
            if (all.Count == 0) return false;

            // BfsDistances with no links reduces to a plain flood fill; DistancesWithLinks falls back
            // to it internally too, so this single call covers both the linked and unlinked case.
            var reachable = DistancesWithLinks(corners, labels, all[0], links);
            return reachable.Count == all.Count;
        }

        internal static bool IsTileFullyOpen(CornerTerrainGrid corners, int tx, int ty, string openTerrain)
        {
            if (tx < 0 || ty < 0 || tx >= corners.Width || ty >= corners.Height) return false;

            return corners.Labels[tx, ty] == openTerrain &&
                   corners.Labels[tx + 1, ty] == openTerrain &&
                   corners.Labels[tx, ty + 1] == openTerrain &&
                   corners.Labels[tx + 1, ty + 1] == openTerrain &&
                   TileDoorGeometry.IsFlatCell(corners, tx, ty);
        }

        /// <summary>
        /// Drops any tile from each room's reported Tiles list that lost full-open status because a
        /// post-pass (accent blob painting, accent channel carving) painted one of its four corners
        /// non-open. Center tiles are guaranteed to survive since callers forbid painting their
        /// corners. Shared by every post-pass that repaints open corners after LayoutRoomBuilder
        /// first populates Rooms.Tiles.
        /// </summary>
        internal static void RecomputeFullyOpenRoomTiles(MacroLayout layout, string openTerrain)
        {
            foreach (var room in layout.Rooms)
            {
                room.Tiles = room.Tiles
                    .Where(t => IsTileFullyOpen(layout.Corners, t.X, t.Y, openTerrain))
                    .ToList();
            }
        }

        /// <summary>
        /// Greedy farthest-point sampling: picks a random first point, then repeatedly adds whichever
        /// remaining candidate maximizes its minimum distance to the already-chosen set. Used to spread
        /// room/chamber seeds across the map instead of letting them cluster.
        /// </summary>
        internal static List<(int X, int Y)> FarthestPointSample(List<(int X, int Y)> candidates, int count, System.Random random)
        {
            var chosen = new List<(int X, int Y)>();
            if (candidates.Count == 0 || count <= 0) return chosen;

            var remaining = new List<(int X, int Y)>(candidates);
            var firstIndex = random.Next(remaining.Count);
            chosen.Add(remaining[firstIndex]);
            remaining.RemoveAt(firstIndex);

            while (chosen.Count < count && remaining.Count > 0)
            {
                var bestIndex = -1;
                var bestDist = -1L;

                for (var i = 0; i < remaining.Count; i++)
                {
                    var minDist = long.MaxValue;
                    foreach (var c in chosen)
                    {
                        var dx = (long)(remaining[i].X - c.X);
                        var dy = (long)(remaining[i].Y - c.Y);
                        var d = dx * dx + dy * dy;
                        if (d < minDist) minDist = d;
                    }

                    if (minDist > bestDist)
                    {
                        bestDist = minDist;
                        bestIndex = i;
                    }
                }

                chosen.Add(remaining[bestIndex]);
                remaining.RemoveAt(bestIndex);
            }

            return chosen;
        }

        internal static void CarveHorizontalBand(CornerTerrainGrid corners, int xa, int xb, int y, int corridorWidth, int width, int height, string openTerrain)
        {
            var lo = Math.Min(xa, xb);
            var hi = Math.Max(xa, xb);
            var half = (corridorWidth - 1) / 2;

            for (var w = 0; w < corridorWidth; w++)
            {
                var yy = Clamp(y + w - half, 1, height - 1);
                for (var x = lo; x <= hi; x++)
                {
                    var xx = Clamp(x, 1, width - 1);
                    corners.Labels[xx, yy] = openTerrain;
                }
            }
        }

        internal static void CarveVerticalBand(CornerTerrainGrid corners, int ya, int yb, int x, int corridorWidth, int width, int height, string openTerrain)
        {
            var lo = Math.Min(ya, yb);
            var hi = Math.Max(ya, yb);
            var half = (corridorWidth - 1) / 2;

            for (var w = 0; w < corridorWidth; w++)
            {
                var xx = Clamp(x + w - half, 1, width - 1);
                for (var y = lo; y <= hi; y++)
                {
                    var yy = Clamp(y, 1, height - 1);
                    corners.Labels[xx, yy] = openTerrain;
                }
            }
        }

        /// <summary>Carves an L-shaped corridor band between two points, bending horizontal-then-vertical or vice versa.</summary>
        internal static void CarveLShapedCorridor(
            CornerTerrainGrid corners,
            int x0, int y0, int x1, int y1,
            bool horizontalFirst,
            int corridorWidth,
            int width, int height,
            string openTerrain)
        {
            if (horizontalFirst)
            {
                CarveHorizontalBand(corners, x0, x1, y0, corridorWidth, width, height, openTerrain);
                CarveVerticalBand(corners, y0, y1, x1, corridorWidth, width, height, openTerrain);
            }
            else
            {
                CarveVerticalBand(corners, y0, y1, x0, corridorWidth, width, height, openTerrain);
                CarveHorizontalBand(corners, x0, x1, y1, corridorWidth, width, height, openTerrain);
            }
        }
    }
}
