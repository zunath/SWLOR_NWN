using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// Produces a corner-granularity macro layout (rooms + corridors) for procedural area generation.
    /// Connectivity is guaranteed by construction: every room is linked into a single spanning tree of
    /// corridors, so the resulting open-corner graph is always fully connected.
    /// </summary>
    public static class MacroLayoutGenerator
    {
        private const int MinRoomCornerSize = 3;
        private const int MaxRoomCornerSize = 7;

        /// <summary>Minimum solid-corner gap required between a room rectangle and the border ring, and between rooms.</summary>
        private const int MinBorderGap = 1;

        /// <summary>Bounded retry budget for random room placement across the whole layout.</summary>
        private const int MaxPlacementAttempts = 2000;

        public static MacroLayout Generate(MacroLayoutParameters parameters, System.Random random)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (random == null) throw new ArgumentNullException(nameof(random));

            var width = parameters.Width;
            var height = parameters.Height;

            var corners = new CornerTerrainGrid(width, height, parameters.SolidTerrain);
            var layout = new MacroLayout(corners);

            var roomRects = PlaceRooms(parameters, random);

            // Carve room interiors open. Border ring (x==0, x==Width, y==0, y==Height) is never touched,
            // since room placement always keeps a >=1 solid-corner gap from it.
            foreach (var rect in roomRects)
            {
                for (var x = rect.X0; x <= rect.X1; x++)
                {
                    for (var y = rect.Y0; y <= rect.Y1; y++)
                    {
                        corners.Labels[x, y] = parameters.OpenTerrain;
                    }
                }
            }

            var centers = new (int X, int Y)[roomRects.Count];
            for (var i = 0; i < roomRects.Count; i++)
            {
                var rect = roomRects[i];
                centers[i] = ((rect.X0 + rect.X1) / 2, (rect.Y0 + rect.Y1) / 2);
            }

            var treeEdges = BuildSpanningTree(centers);

            foreach (var edge in treeEdges)
            {
                var a = centers[edge.U];
                var b = centers[edge.V];
                var horizontalFirst = random.Next(2) == 0;
                CarveCorridor(corners, a.X, a.Y, b.X, b.Y, horizontalFirst, width, height, parameters.OpenTerrain);
            }

            var rooms = BuildRoomMetadata(roomRects, corners, parameters.OpenTerrain);
            AssignRoles(rooms, treeEdges, random);

            layout.Rooms = rooms;
            return layout;
        }

        private readonly struct RoomRect
        {
            public RoomRect(int x0, int y0, int x1, int y1)
            {
                X0 = x0;
                Y0 = y0;
                X1 = x1;
                Y1 = y1;
            }

            public int X0 { get; }
            public int Y0 { get; }
            public int X1 { get; }
            public int Y1 { get; }
        }

        private readonly struct TreeEdge
        {
            public TreeEdge(int u, int v)
            {
                U = u;
                V = v;
            }

            public int U { get; }
            public int V { get; }
        }

        private static List<RoomRect> PlaceRooms(MacroLayoutParameters parameters, System.Random random)
        {
            var rooms = new List<RoomRect>();
            var targetCount = random.Next(parameters.MinRooms, parameters.MaxRooms + 1);

            var attempts = 0;
            while (rooms.Count < targetCount && attempts < MaxPlacementAttempts)
            {
                attempts++;

                var w = random.Next(MinRoomCornerSize, MaxRoomCornerSize + 1);
                var h = random.Next(MinRoomCornerSize, MaxRoomCornerSize + 1);

                // Room corners must keep >=1 solid-corner gap from the border ring at x==0/Width, y==0/Height.
                var maxX0 = parameters.Width - MinBorderGap - 1 - (w - 1);
                var maxY0 = parameters.Height - MinBorderGap - 1 - (h - 1);
                var minX0 = MinBorderGap + 1;
                var minY0 = MinBorderGap + 1;

                if (maxX0 < minX0 || maxY0 < minY0)
                    continue;

                var x0 = random.Next(minX0, maxX0 + 1);
                var y0 = random.Next(minY0, maxY0 + 1);
                var x1 = x0 + w - 1;
                var y1 = y0 + h - 1;

                if (OverlapsAny(rooms, x0, y0, x1, y1))
                    continue;

                rooms.Add(new RoomRect(x0, y0, x1, y1));
            }

            if (rooms.Count < 2)
            {
                throw new InvalidOperationException(
                    $"MacroLayoutGenerator could not place enough rooms: only {rooms.Count} fit in a " +
                    $"{parameters.Width}x{parameters.Height} area after {attempts} placement attempts " +
                    "(at least 2 rooms are required).");
            }

            return rooms;
        }

        private static bool OverlapsAny(List<RoomRect> rooms, int x0, int y0, int x1, int y1)
        {
            foreach (var r in rooms)
            {
                // Inflate the candidate rect by the required gap; if it still intersects an existing
                // room, the two rooms would be closer than the minimum solid-corner gap apart.
                if (x0 - MinBorderGap <= r.X1 && x1 + MinBorderGap >= r.X0 &&
                    y0 - MinBorderGap <= r.Y1 && y1 + MinBorderGap >= r.Y0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Deterministic Prim's algorithm (nearest-neighbor) spanning tree over room centers.
        /// No randomness is needed here: ties are broken by iteration order, which is fixed for a
        /// given room list.
        /// </summary>
        private static List<TreeEdge> BuildSpanningTree((int X, int Y)[] centers)
        {
            var n = centers.Length;
            var edges = new List<TreeEdge>(n - 1);
            var inTree = new bool[n];
            inTree[0] = true;
            var connected = 1;

            while (connected < n)
            {
                var bestDist = long.MaxValue;
                var bestU = -1;
                var bestV = -1;

                for (var u = 0; u < n; u++)
                {
                    if (!inTree[u]) continue;

                    for (var v = 0; v < n; v++)
                    {
                        if (inTree[v]) continue;

                        var dx = (long)(centers[u].X - centers[v].X);
                        var dy = (long)(centers[u].Y - centers[v].Y);
                        var dist = dx * dx + dy * dy;

                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestU = u;
                            bestV = v;
                        }
                    }
                }

                inTree[bestV] = true;
                edges.Add(new TreeEdge(bestU, bestV));
                connected++;
            }

            return edges;
        }

        private static void CarveCorridor(
            CornerTerrainGrid corners,
            int x0, int y0, int x1, int y1,
            bool horizontalFirst,
            int width, int height,
            string openTerrain)
        {
            if (horizontalFirst)
            {
                CarveHorizontal(corners, x0, x1, y0, width, height, openTerrain);
                CarveVertical(corners, y0, y1, x1, width, height, openTerrain);
            }
            else
            {
                CarveVertical(corners, y0, y1, x0, width, height, openTerrain);
                CarveHorizontal(corners, x0, x1, y1, width, height, openTerrain);
            }
        }

        private static void CarveHorizontal(CornerTerrainGrid corners, int xa, int xb, int y, int width, int height, string openTerrain)
        {
            var clampedY = Clamp(y, 1, height - 1);
            var lo = Math.Min(xa, xb);
            var hi = Math.Max(xa, xb);

            for (var x = lo; x <= hi; x++)
            {
                var clampedX = Clamp(x, 1, width - 1);
                corners.Labels[clampedX, clampedY] = openTerrain;
            }
        }

        private static void CarveVertical(CornerTerrainGrid corners, int ya, int yb, int x, int width, int height, string openTerrain)
        {
            var clampedX = Clamp(x, 1, width - 1);
            var lo = Math.Min(ya, yb);
            var hi = Math.Max(ya, yb);

            for (var y = lo; y <= hi; y++)
            {
                var clampedY = Clamp(y, 1, height - 1);
                corners.Labels[clampedX, clampedY] = openTerrain;
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static List<LayoutRoom> BuildRoomMetadata(List<RoomRect> roomRects, CornerTerrainGrid corners, string openTerrain)
        {
            var rooms = new List<LayoutRoom>(roomRects.Count);

            for (var i = 0; i < roomRects.Count; i++)
            {
                var rect = roomRects[i];
                var tiles = new List<(int X, int Y)>();

                for (var tx = rect.X0; tx < rect.X1; tx++)
                {
                    for (var ty = rect.Y0; ty < rect.Y1; ty++)
                    {
                        var open = corners.Labels[tx, ty] == openTerrain &&
                                   corners.Labels[tx + 1, ty] == openTerrain &&
                                   corners.Labels[tx, ty + 1] == openTerrain &&
                                   corners.Labels[tx + 1, ty + 1] == openTerrain;

                        if (open)
                            tiles.Add((tx, ty));
                    }
                }

                var centerX = (rect.X0 + (rect.X1 - 1)) / 2.0;
                var centerY = (rect.Y0 + (rect.Y1 - 1)) / 2.0;

                var centerTile = tiles[0];
                var bestDist = double.MaxValue;
                foreach (var tile in tiles)
                {
                    var dx = tile.X - centerX;
                    var dy = tile.Y - centerY;
                    var dist = dx * dx + dy * dy;

                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        centerTile = tile;
                    }
                }

                rooms.Add(new LayoutRoom
                {
                    Id = i,
                    Role = RoomRole.Standard,
                    CenterTile = centerTile,
                    Tiles = tiles
                });
            }

            return rooms;
        }

        private static void AssignRoles(List<LayoutRoom> rooms, List<TreeEdge> treeEdges, System.Random random)
        {
            var n = rooms.Count;
            var entranceIndex = random.Next(n);

            var adjacency = new List<int>[n];
            for (var i = 0; i < n; i++)
                adjacency[i] = new List<int>();

            foreach (var edge in treeEdges)
            {
                adjacency[edge.U].Add(edge.V);
                adjacency[edge.V].Add(edge.U);
            }

            var hopDistance = new int[n];
            for (var i = 0; i < n; i++)
                hopDistance[i] = -1;

            hopDistance[entranceIndex] = 0;
            var queue = new Queue<int>();
            queue.Enqueue(entranceIndex);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in adjacency[current])
                {
                    if (hopDistance[next] != -1) continue;
                    hopDistance[next] = hopDistance[current] + 1;
                    queue.Enqueue(next);
                }
            }

            var bossIndex = -1;
            var bestHop = -1;
            for (var i = 0; i < n; i++)
            {
                if (i == entranceIndex) continue;
                if (hopDistance[i] > bestHop)
                {
                    bestHop = hopDistance[i];
                    bossIndex = i;
                }
            }

            rooms[entranceIndex].Role = RoomRole.Entrance;
            if (bossIndex != -1)
                rooms[bossIndex].Role = RoomRole.Boss;
        }
    }
}
