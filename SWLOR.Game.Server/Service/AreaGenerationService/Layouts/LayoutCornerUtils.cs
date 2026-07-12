using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AreaGenerationService.Layouts
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
            var result = new List<(int X, int Y)>();
            for (var x = 0; x <= corners.Width; x++)
            {
                for (var y = 0; y <= corners.Height; y++)
                {
                    if (corners.Labels[x, y] == label)
                        result.Add((x, y));
                }
            }

            return result;
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
                    if (corners.Labels[nx, ny] != label) continue;

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
            if (links == null || links.Count == 0)
                return BfsDistances(corners, label, start);

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
                    if (corners.Labels[nx, ny] != label) continue;
                    Relax((nx, ny), 1);
                }

                if (linkEdges.TryGetValue(current, out var throughTunnels))
                {
                    foreach (var (to, length) in throughTunnels)
                        Relax(to, Math.Max(1, length));
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
            if (links == null || links.Count == 0)
                return IsSingleComponent(corners, label);

            var all = GetCorners(corners, label);
            if (all.Count == 0) return false;

            var reachable = DistancesWithLinks(corners, label, all[0], links);
            return reachable.Count == all.Count;
        }

        internal static bool IsTileFullyOpen(CornerTerrainGrid corners, int tx, int ty, string openTerrain)
        {
            if (tx < 0 || ty < 0 || tx >= corners.Width || ty >= corners.Height) return false;

            return corners.Labels[tx, ty] == openTerrain &&
                   corners.Labels[tx + 1, ty] == openTerrain &&
                   corners.Labels[tx, ty + 1] == openTerrain &&
                   corners.Labels[tx + 1, ty + 1] == openTerrain;
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
