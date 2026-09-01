#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// Cellular-automata caverns: winding, blobby open space with nooks and pockets (Korriban caves feel).
    /// </summary>
    internal static class OrganicCaveLayout
    {
        /// <summary>Open-corner components smaller than this are discarded rather than tunnel-connected.</summary>
        private const int MinComponentSize = 8;

        private const int MaxRoomTiles = 24;

        internal static MacroLayout Generate(MacroLayoutParameters parameters, System.Random random)
        {
            var width = parameters.Width;
            var height = parameters.Height;

            var corners = new CornerTerrainGrid(width, height, parameters.SolidTerrain);
            var layout = new MacroLayout(corners);

            FillRandom(corners, parameters, random);

            for (var pass = 0; pass < parameters.SmoothingPasses; pass++)
                Smooth(corners, parameters);

            ConnectComponents(corners, parameters, random);

            layout.Rooms = SampleRooms(corners, parameters, random);
            return layout;
        }

        private static void FillRandom(CornerTerrainGrid corners, MacroLayoutParameters parameters, System.Random random)
        {
            for (var x = 1; x < parameters.Width; x++)
            {
                for (var y = 1; y < parameters.Height; y++)
                {
                    corners.Labels[x, y] = random.NextDouble() < parameters.OpenFillTarget
                        ? parameters.OpenTerrain
                        : parameters.SolidTerrain;
                }
            }
        }

        /// <summary>
        /// One cellular-automata smoothing pass: a corner becomes solid if 5 or more of its 8 neighbors
        /// (from the pre-pass state; out-of-bounds treated as solid) are solid, else open.
        /// </summary>
        private static void Smooth(CornerTerrainGrid corners, MacroLayoutParameters parameters)
        {
            var width = parameters.Width;
            var height = parameters.Height;
            var next = new string[width + 1, height + 1];

            for (var x = 0; x <= width; x++)
                for (var y = 0; y <= height; y++)
                    next[x, y] = corners.Labels[x, y];

            for (var x = 1; x < width; x++)
            {
                for (var y = 1; y < height; y++)
                {
                    var solidNeighbors = 0;

                    for (var dx = -1; dx <= 1; dx++)
                    {
                        for (var dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            var nx = x + dx;
                            var ny = y + dy;
                            var isSolid = nx < 0 || nx > width || ny < 0 || ny > height ||
                                          corners.Labels[nx, ny] == parameters.SolidTerrain;

                            if (isSolid) solidNeighbors++;
                        }
                    }

                    next[x, y] = solidNeighbors >= 5 ? parameters.SolidTerrain : parameters.OpenTerrain;
                }
            }

            for (var x = 1; x < width; x++)
                for (var y = 1; y < height; y++)
                    corners.Labels[x, y] = next[x, y];
        }

        /// <summary>
        /// Keeps every open-corner component of size >= MinComponentSize, fills smaller components
        /// solid, then tunnels every kept component (other than the largest) to the largest via a
        /// winding random walk with a guaranteed straight-line fallback so connectivity never depends
        /// on the walk actually landing on the target.
        /// </summary>
        private static void ConnectComponents(CornerTerrainGrid corners, MacroLayoutParameters parameters, System.Random random)
        {
            var openTerrain = parameters.OpenTerrain;
            var solidTerrain = parameters.SolidTerrain;

            var visitedGlobal = new HashSet<(int X, int Y)>();
            var components = new List<HashSet<(int X, int Y)>>();

            foreach (var corner in LayoutCornerUtils.GetCorners(corners, openTerrain))
            {
                if (visitedGlobal.Contains(corner)) continue;

                var component = LayoutCornerUtils.FloodFill(corners, openTerrain, corner);
                foreach (var c in component) visitedGlobal.Add(c);
                components.Add(component);
            }

            if (components.Count == 0)
            {
                throw new InvalidOperationException(
                    "OrganicCave produced no open space after smoothing; the facade should retry with a new seed.");
            }

            var kept = components.Where(c => c.Count >= MinComponentSize).ToList();
            if (kept.Count == 0)
            {
                // Nothing survived the size cutoff; keep the single largest so there's a base cavern.
                kept.Add(components.OrderByDescending(c => c.Count).First());
            }

            foreach (var component in components)
            {
                if (kept.Contains(component)) continue;
                foreach (var (x, y) in component)
                    corners.Labels[x, y] = solidTerrain;
            }

            var largest = kept.OrderByDescending(c => c.Count).First();

            foreach (var component in kept)
            {
                if (component == largest) continue;
                TunnelToward(corners, parameters, component, largest, random);
            }
        }

        private static void TunnelToward(
            CornerTerrainGrid corners, MacroLayoutParameters parameters,
            HashSet<(int X, int Y)> source, HashSet<(int X, int Y)> target, System.Random random)
        {
            var width = parameters.Width;
            var height = parameters.Height;
            var corridorWidth = Math.Max(1, parameters.CorridorWidth);
            var openTerrain = parameters.OpenTerrain;

            var current = source.ElementAt(random.Next(source.Count));
            var goal = target.ElementAt(random.Next(target.Count));

            var maxSteps = (width + height) * 3;
            var steps = 0;

            while ((current.X != goal.X || current.Y != goal.Y) && steps < maxSteps)
            {
                steps++;
                PaintDisk(corners, parameters, current, corridorWidth);

                var dx = Math.Sign(goal.X - current.X);
                var dy = Math.Sign(goal.Y - current.Y);

                int stepX, stepY;
                if (dx != 0 && (dy == 0 || random.NextDouble() < 0.6))
                {
                    stepX = dx;
                    stepY = 0;
                }
                else if (dy != 0)
                {
                    stepX = 0;
                    stepY = dy;
                }
                else
                {
                    stepX = 0;
                    stepY = 0;
                }

                if (random.NextDouble() < 0.2)
                {
                    // Jitter: take a perpendicular step instead, for a winding rather than straight tunnel.
                    if (stepX != 0)
                    {
                        stepX = 0;
                        stepY = random.Next(2) == 0 ? 1 : -1;
                    }
                    else if (stepY != 0)
                    {
                        stepY = 0;
                        stepX = random.Next(2) == 0 ? 1 : -1;
                    }
                }

                var nextX = LayoutCornerUtils.Clamp(current.X + stepX, 1, width - 1);
                var nextY = LayoutCornerUtils.Clamp(current.Y + stepY, 1, height - 1);
                current = (nextX, nextY);
            }

            PaintDisk(corners, parameters, goal, corridorWidth);

            // Guaranteed connector: the winding walk gives the tunnel its organic shape, but whether it
            // lands exactly on the goal is not mathematically certain within a bounded step budget. This
            // straight L-shaped carve from wherever the walk ended to the goal makes connectivity certain
            // regardless of the walk's outcome.
            var horizontalFirst = random.Next(2) == 0;
            LayoutCornerUtils.CarveLShapedCorridor(
                corners, current.X, current.Y, goal.X, goal.Y, horizontalFirst,
                corridorWidth, width, height, openTerrain);
        }

        private static void PaintDisk(CornerTerrainGrid corners, MacroLayoutParameters parameters, (int X, int Y) center, int corridorWidth)
        {
            var radius = (corridorWidth - 1) / 2;
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    var x = LayoutCornerUtils.Clamp(center.X + dx, 1, parameters.Width - 1);
                    var y = LayoutCornerUtils.Clamp(center.Y + dy, 1, parameters.Height - 1);
                    corners.Labels[x, y] = parameters.OpenTerrain;
                }
            }
        }

        private static List<LayoutRoom> SampleRooms(CornerTerrainGrid corners, MacroLayoutParameters parameters, System.Random random)
        {
            var candidates = new List<(int X, int Y)>();
            for (var x = 1; x < parameters.Width; x++)
            {
                for (var y = 1; y < parameters.Height; y++)
                {
                    if (IsFullyOpenNeighborhood(corners, parameters.OpenTerrain, x, y))
                        candidates.Add((x, y));
                }
            }

            if (candidates.Count < 2)
            {
                throw new InvalidOperationException(
                    $"OrganicCave found only {candidates.Count} spacious seed point(s) in a " +
                    $"{parameters.Width}x{parameters.Height} area; at least 2 are required.");
            }

            var requested = random.Next(parameters.MinRooms, parameters.MaxRooms + 1);
            var roomCount = Math.Max(2, Math.Min(requested, candidates.Count));

            var seeds = LayoutCornerUtils.FarthestPointSample(candidates, roomCount, random);
            var claimed = new HashSet<(int X, int Y)>();
            var rooms = new List<LayoutRoom>(seeds.Count);

            foreach (var cornerSeed in seeds)
            {
                // A late seed can find every touching tile already claimed by earlier rooms; skip it
                // rather than emitting a room with no tiles (spawn placement needs at least one).
                if (!TryFindTouchingOpenTile(corners, parameters.OpenTerrain, cornerSeed, claimed, out var seedTile))
                    continue;

                var room = LayoutRoomBuilder.BuildFromSeed(rooms.Count, seedTile, corners, parameters.OpenTerrain, MaxRoomTiles, claimed);
                if (room.Tiles.Count == 0)
                    continue;

                rooms.Add(room);
            }

            if (rooms.Count < 2)
            {
                throw new InvalidOperationException(
                    $"OrganicCave could only build {rooms.Count} non-empty room(s) from {seeds.Count} seed(s) in a " +
                    $"{parameters.Width}x{parameters.Height} area; at least 2 are required.");
            }

            return rooms;
        }

        /// <summary>
        /// A corner whose full 3x3 neighborhood is open guarantees at least one of the four tiles
        /// touching it is fully open; picks whichever of those four is open and unclaimed, or reports
        /// failure when earlier rooms already claimed all of them.
        /// </summary>
        private static bool TryFindTouchingOpenTile(
            CornerTerrainGrid corners, string openTerrain, (int X, int Y) corner, HashSet<(int X, int Y)> claimed,
            out (int X, int Y) tile)
        {
            var options = new[]
            {
                (corner.X - 1, corner.Y - 1),
                (corner.X, corner.Y - 1),
                (corner.X - 1, corner.Y),
                (corner.X, corner.Y)
            };

            foreach (var option in options)
            {
                if (!claimed.Contains(option) && LayoutCornerUtils.IsTileFullyOpen(corners, option.Item1, option.Item2, openTerrain))
                {
                    tile = option;
                    return true;
                }
            }

            tile = default;
            return false;
        }

        private static bool IsFullyOpenNeighborhood(CornerTerrainGrid corners, string openTerrain, int x, int y)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    var nx = x + dx;
                    var ny = y + dy;
                    if (nx < 0 || nx > corners.Width || ny < 0 || ny > corners.Height) return false;
                    if (corners.Labels[nx, ny] != openTerrain) return false;
                }
            }

            return true;
        }
    }
}
