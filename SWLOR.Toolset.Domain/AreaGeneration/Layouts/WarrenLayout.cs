#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// Maze-like corridor network with small chambers and loops (Veles sewers/undercity feel). Most
    /// open space stays 1-2 corners wide with intersections; loop removal eliminates most dead ends.
    /// </summary>
    internal static class WarrenLayout
    {
        internal static MacroLayout Generate(MacroLayoutParameters parameters, System.Random random)
        {
            var width = parameters.Width;
            var height = parameters.Height;

            var corners = new CornerTerrainGrid(width, height, parameters.SolidTerrain);
            var layout = new MacroLayout(corners);

            var corridorWidth = Math.Max(1, parameters.CorridorWidth);
            var pitch = corridorWidth + 1;

            var cellsX = Math.Max(1, CountCells(width, pitch));
            var cellsY = Math.Max(1, CountCells(height, pitch));

            var visited = new bool[cellsX, cellsY];
            var passages = new HashSet<((int X, int Y) A, (int X, int Y) B)>();

            CarveMaze(visited, passages, cellsX, cellsY, random);
            RemoveLoopWalls(passages, cellsX, cellsY, parameters.LoopFactor, random);

            OpenCells(corners, parameters, visited, cellsX, cellsY, pitch, corridorWidth);
            OpenPassages(corners, parameters, passages, pitch, corridorWidth);

            layout.Rooms = CarveChambers(corners, parameters, cellsX, cellsY, pitch, corridorWidth, random);
            return layout;
        }

        private static int CountCells(int span, int pitch)
        {
            var count = 0;
            while (1 + count * pitch <= span - 1)
                count++;
            return count;
        }

        private static (int X0, int Y0, int X1, int Y1) CellBounds(int cx, int cy, int pitch, int corridorWidth, int width, int height)
        {
            var x0 = 1 + cx * pitch;
            var y0 = 1 + cy * pitch;
            var x1 = Math.Min(x0 + corridorWidth - 1, width - 1);
            var y1 = Math.Min(y0 + corridorWidth - 1, height - 1);
            return (x0, y0, x1, y1);
        }

        /// <summary>Recursive-backtracker (iterative, stack-based) perfect maze over the cell graph.</summary>
        private static void CarveMaze(bool[,] visited, HashSet<((int X, int Y) A, (int X, int Y) B)> passages, int cellsX, int cellsY, System.Random random)
        {
            var stack = new Stack<(int X, int Y)>();
            var start = (X: random.Next(cellsX), Y: random.Next(cellsY));
            visited[start.X, start.Y] = true;
            stack.Push(start);

            while (stack.Count > 0)
            {
                var current = stack.Peek();
                var neighbors = ShuffledNeighbors(current, cellsX, cellsY, random)
                    .Where(n => !visited[n.X, n.Y])
                    .ToList();

                if (neighbors.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                var next = neighbors[0];
                visited[next.X, next.Y] = true;
                passages.Add(NormalizeEdge(current, next));
                stack.Push(next);
            }
        }

        private static List<(int X, int Y)> ShuffledNeighbors((int X, int Y) cell, int cellsX, int cellsY, System.Random random)
        {
            var candidates = new List<(int X, int Y)>();
            foreach (var (dx, dy) in LayoutCornerUtils.Ortho4)
            {
                var nx = cell.X + dx;
                var ny = cell.Y + dy;
                if (nx < 0 || nx >= cellsX || ny < 0 || ny >= cellsY) continue;
                candidates.Add((nx, ny));
            }

            for (var i = candidates.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            return candidates;
        }

        private static ((int X, int Y) A, (int X, int Y) B) NormalizeEdge((int X, int Y) a, (int X, int Y) b)
        {
            if (a.X < b.X || (a.X == b.X && a.Y < b.Y))
                return (a, b);
            return (b, a);
        }

        private static void RemoveLoopWalls(HashSet<((int X, int Y) A, (int X, int Y) B)> passages, int cellsX, int cellsY, double loopFactor, System.Random random)
        {
            var allEdges = new List<((int X, int Y) A, (int X, int Y) B)>();
            for (var x = 0; x < cellsX; x++)
            {
                for (var y = 0; y < cellsY; y++)
                {
                    if (x + 1 < cellsX) allEdges.Add(NormalizeEdge((x, y), (x + 1, y)));
                    if (y + 1 < cellsY) allEdges.Add(NormalizeEdge((x, y), (x, y + 1)));
                }
            }

            var walls = allEdges.Where(e => !passages.Contains(e)).ToList();
            var extraCount = Math.Min((int)Math.Round(loopFactor * walls.Count), walls.Count);

            for (var i = 0; i < extraCount; i++)
            {
                var index = random.Next(walls.Count);
                passages.Add(walls[index]);
                walls.RemoveAt(index);
            }
        }

        private static void OpenCells(
            CornerTerrainGrid corners, MacroLayoutParameters parameters, bool[,] visited,
            int cellsX, int cellsY, int pitch, int corridorWidth)
        {
            for (var cx = 0; cx < cellsX; cx++)
            {
                for (var cy = 0; cy < cellsY; cy++)
                {
                    if (!visited[cx, cy]) continue;

                    var bounds = CellBounds(cx, cy, pitch, corridorWidth, parameters.Width, parameters.Height);
                    for (var x = bounds.X0; x <= bounds.X1; x++)
                        for (var y = bounds.Y0; y <= bounds.Y1; y++)
                            corners.Labels[x, y] = parameters.OpenTerrain;
                }
            }
        }

        private static void OpenPassages(
            CornerTerrainGrid corners, MacroLayoutParameters parameters,
            HashSet<((int X, int Y) A, (int X, int Y) B)> passages, int pitch, int corridorWidth)
        {
            foreach (var (a, b) in passages)
            {
                var boundsA = CellBounds(a.X, a.Y, pitch, corridorWidth, parameters.Width, parameters.Height);
                var boundsB = CellBounds(b.X, b.Y, pitch, corridorWidth, parameters.Width, parameters.Height);

                if (a.X == b.X)
                {
                    // Vertical neighbors: the wall gap is the row(s) strictly between the two Y bands.
                    var wallY0 = Math.Min(boundsA.Y1, boundsB.Y1) + 1;
                    var wallY1 = Math.Max(boundsA.Y0, boundsB.Y0) - 1;
                    var xLo = Math.Max(boundsA.X0, boundsB.X0);
                    var xHi = Math.Min(boundsA.X1, boundsB.X1);

                    for (var y = wallY0; y <= wallY1; y++)
                        for (var x = xLo; x <= xHi; x++)
                            corners.Labels[x, y] = parameters.OpenTerrain;
                }
                else
                {
                    // Horizontal neighbors: the wall gap is the column(s) strictly between the two X bands.
                    var wallX0 = Math.Min(boundsA.X1, boundsB.X1) + 1;
                    var wallX1 = Math.Max(boundsA.X0, boundsB.X0) - 1;
                    var yLo = Math.Max(boundsA.Y0, boundsB.Y0);
                    var yHi = Math.Min(boundsA.Y1, boundsB.Y1);

                    for (var x = wallX0; x <= wallX1; x++)
                        for (var y = yLo; y <= yHi; y++)
                            corners.Labels[x, y] = parameters.OpenTerrain;
                }
            }
        }

        private static List<LayoutRoom> CarveChambers(
            CornerTerrainGrid corners, MacroLayoutParameters parameters,
            int cellsX, int cellsY, int pitch, int corridorWidth, System.Random random)
        {
            var allCells = new List<(int X, int Y)>();
            for (var x = 0; x < cellsX; x++)
                for (var y = 0; y < cellsY; y++)
                    allCells.Add((x, y));

            if (allCells.Count < 2)
            {
                throw new InvalidOperationException(
                    $"Warren maze only produced {allCells.Count} cell(s) in a {parameters.Width}x{parameters.Height} " +
                    "area; at least 2 are required for chambers.");
            }

            var requested = random.Next(parameters.MinRooms, parameters.MaxRooms + 1);
            var chamberCount = Math.Max(2, Math.Min(requested, allCells.Count));

            var chosenCells = LayoutCornerUtils.FarthestPointSample(allCells, chamberCount, random);

            var minSize = Math.Max(2, parameters.MinRoomCornerSize);
            var maxSize = Math.Max(minSize, Math.Min(parameters.MaxRoomCornerSize, 5));

            var rooms = new List<LayoutRoom>(chosenCells.Count);
            for (var i = 0; i < chosenCells.Count; i++)
            {
                var cellBounds = CellBounds(chosenCells[i].X, chosenCells[i].Y, pitch, corridorWidth, parameters.Width, parameters.Height);
                var centerX = (cellBounds.X0 + cellBounds.X1) / 2;
                var centerY = (cellBounds.Y0 + cellBounds.Y1) / 2;

                var w = Math.Min(random.Next(minSize, maxSize + 1), Math.Max(2, parameters.Width - 2));
                var h = Math.Min(random.Next(minSize, maxSize + 1), Math.Max(2, parameters.Height - 2));

                var maxX0 = Math.Max(1, parameters.Width - 1 - (w - 1));
                var maxY0 = Math.Max(1, parameters.Height - 1 - (h - 1));

                var x0 = LayoutCornerUtils.Clamp(centerX - w / 2, 1, maxX0);
                var y0 = LayoutCornerUtils.Clamp(centerY - h / 2, 1, maxY0);
                var x1 = x0 + w - 1;
                var y1 = y0 + h - 1;

                var rect = new RoomRect(x0, y0, x1, y1);
                for (var x = rect.X0; x <= rect.X1; x++)
                    for (var y = rect.Y0; y <= rect.Y1; y++)
                        corners.Labels[x, y] = parameters.OpenTerrain;

                rooms.Add(LayoutRoomBuilder.BuildFromRect(i, rect, corners, parameters.OpenTerrain));
            }

            return rooms;
        }
    }
}
