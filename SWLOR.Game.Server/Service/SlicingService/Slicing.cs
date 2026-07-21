using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SlicingService
{
    public static class Slicing
    {
        private static readonly Dictionary<int, (int Width, int Height, int ExtraTrace, int SwapCount)> _tierRules = new()
        {
            [1] = (3, 3, 4, 0),
            [2] = (4, 3, 3, 1),
            [3] = (4, 4, 3, 2),
            [4] = (5, 4, 2, 3),
            [5] = (5, 5, 2, 4)
        };

        // These routes are authored once, then rotated and displaced from a deterministic seed.
        // Construction therefore has a known solution and never runs a solver on the game server.
        private static readonly Dictionary<int, (int X, int Y)[]> _routes = new()
        {
            [1] = new[] { (0, 1), (0, 0), (1, 0), (1, 1), (2, 1) },
            [2] = new[] { (0, 1), (0, 0), (1, 0), (1, 1), (1, 2), (2, 2), (2, 1), (3, 1) },
            [3] = new[] { (0, 2), (0, 3), (1, 3), (1, 2), (1, 1), (2, 1), (2, 0), (3, 0), (3, 1), (3, 2) },
            [4] = new[] { (0, 2), (0, 3), (1, 3), (1, 2), (1, 1), (2, 1), (2, 0), (3, 0), (3, 1), (3, 2), (4, 2) },
            [5] = new[] { (0, 2), (0, 3), (0, 4), (1, 4), (1, 3), (1, 2), (1, 1), (2, 1), (2, 0), (3, 0), (3, 1), (3, 2), (3, 3), (4, 3), (4, 2) }
        };

        public static SlicingBoard BuildBoard(int tier, int seed)
        {
            if (!_tierRules.TryGetValue(tier, out var rule))
                throw new ArgumentOutOfRangeException(nameof(tier), "Slicing tier must be between 1 and 5.");

            var random = new System.Random(seed);
            var board = new SlicingBoard
            {
                Tier = tier,
                Seed = seed,
                Width = rule.Width,
                Height = rule.Height
            };

            for (var index = 0; index < rule.Width * rule.Height; index++)
            {
                board.Tiles.Add(CreateDecoy(random));
            }

            var route = _routes[tier];
            for (var routeIndex = 0; routeIndex < route.Length; routeIndex++)
            {
                var coordinate = route[routeIndex];
                var index = ToIndex(coordinate.X, coordinate.Y, rule.Width);
                var connections = SlicingConnection.None;

                if (routeIndex > 0)
                    connections |= DirectionTo(coordinate, route[routeIndex - 1]);
                if (routeIndex < route.Length - 1)
                    connections |= DirectionTo(coordinate, route[routeIndex + 1]);

                var type = routeIndex == 0
                    ? SlicingTileType.Entry
                    : routeIndex == route.Length - 1
                        ? SlicingTileType.Core
                        : TypeForConnections(connections);
                var solutionOrientation = FindOrientation(type, connections);
                var scrambleSteps = random.Next(1, 4);

                board.Tiles[index] = new SlicingTile
                {
                    Type = type,
                    Orientation = (solutionOrientation + scrambleSteps) % 4,
                    SolutionIndex = index,
                    RouteOrder = routeIndex,
                    SolutionOrientation = solutionOrientation
                };
            }

            ApplyDeterministicSwaps(board, route, rule.SwapCount, random);

            // A decoy can occasionally complete an unintended path after scrambling.
            // Break that path at the fixed entry socket without changing the authored solution.
            var entry = board.Tiles.FindIndex(x => x.Type == SlicingTileType.Entry);
            for (var turn = 0; turn < 3 && IsSolved(board); turn++)
                RotateClockwise(board, entry);

            var rotationCost = board.Tiles
                .Where(x => x.SolutionIndex >= 0)
                .Sum(GetClockwiseSolutionRotationCost);
            var solutionCost = rotationCost + board.SolutionSwaps.Count * 2;

            return new SlicingBoard
            {
                Tier = board.Tier,
                Seed = board.Seed,
                Width = board.Width,
                Height = board.Height,
                BaseTrace = solutionCost + rule.ExtraTrace,
                SolutionActionCost = solutionCost,
                Tiles = board.Tiles,
                SolutionSwaps = board.SolutionSwaps
            };
        }

        public static int GetTraceBonus(int slicingRank, int lockpicking, int positivePerceptionModifier)
        {
            var perkBonus = slicingRank switch
            {
                >= 5 => 3,
                4 => 2,
                3 => 1,
                _ => 0
            };
            var statBonus = Math.Min(5, Math.Max(0, lockpicking + Math.Max(0, positivePerceptionModifier)) / 5);
            return perkBonus + statBonus;
        }

        public static int GetDestructionChance(int failedAttempts)
        {
            return failedAttempts switch
            {
                <= 1 => 0,
                2 => 10,
                3 => 25,
                4 => 50,
                _ => 100
            };
        }

        public static bool RotateClockwise(SlicingBoard board, int index)
        {
            if (!IsValidIndex(board, index))
                return false;

            board.Tiles[index].Orientation = (board.Tiles[index].Orientation + 1) % 4;
            return true;
        }

        public static bool SwapAdjacent(SlicingBoard board, int firstIndex, int secondIndex)
        {
            if (!AreAdjacent(board, firstIndex, secondIndex))
                return false;

            if (board.Tiles[firstIndex].Type is SlicingTileType.Entry or SlicingTileType.Core ||
                board.Tiles[secondIndex].Type is SlicingTileType.Entry or SlicingTileType.Core)
                return false;

            (board.Tiles[firstIndex], board.Tiles[secondIndex]) = (board.Tiles[secondIndex], board.Tiles[firstIndex]);
            return true;
        }

        public static bool AreAdjacent(SlicingBoard board, int firstIndex, int secondIndex)
        {
            if (!IsValidIndex(board, firstIndex) || !IsValidIndex(board, secondIndex))
                return false;

            var firstX = firstIndex % board.Width;
            var firstY = firstIndex / board.Width;
            var secondX = secondIndex % board.Width;
            var secondY = secondIndex / board.Width;
            return Math.Abs(firstX - secondX) + Math.Abs(firstY - secondY) == 1;
        }

        public static IReadOnlySet<int> GetPoweredIndices(SlicingBoard board)
        {
            var powered = new HashSet<int>();
            var entry = board.Tiles.FindIndex(x => x.Type == SlicingTileType.Entry);
            if (entry < 0)
                return powered;

            var queue = new Queue<int>();
            queue.Enqueue(entry);
            powered.Add(entry);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var (direction, opposite, offsetX, offsetY) in NeighborRules())
                {
                    if (!GetConnections(board.Tiles[current]).HasFlag(direction))
                        continue;

                    var x = current % board.Width + offsetX;
                    var y = current / board.Width + offsetY;
                    if (x < 0 || x >= board.Width || y < 0 || y >= board.Height)
                        continue;

                    var neighbor = ToIndex(x, y, board.Width);
                    if (!GetConnections(board.Tiles[neighbor]).HasFlag(opposite) || !powered.Add(neighbor))
                        continue;

                    queue.Enqueue(neighbor);
                }
            }

            return powered;
        }

        public static bool IsSolved(SlicingBoard board)
        {
            var core = board.Tiles.FindIndex(x => x.Type == SlicingTileType.Core);
            return core >= 0 && GetPoweredIndices(board).Contains(core);
        }

        public static SlicingConnection GetConnections(SlicingTile tile)
        {
            var baseConnections = tile.Type switch
            {
                SlicingTileType.Straight => SlicingConnection.North | SlicingConnection.South,
                SlicingTileType.Corner => SlicingConnection.North | SlicingConnection.East,
                SlicingTileType.Junction => SlicingConnection.North | SlicingConnection.East | SlicingConnection.South,
                SlicingTileType.Cross => SlicingConnection.North | SlicingConnection.East | SlicingConnection.South | SlicingConnection.West,
                SlicingTileType.Entry or SlicingTileType.Core => SlicingConnection.North,
                _ => SlicingConnection.None
            };

            return RotateConnections(baseConnections, tile.Orientation);
        }

        public static int GetClockwiseSolutionRotationCost(SlicingTile tile)
        {
            var target = RotateConnections(BaseConnections(tile.Type), tile.SolutionOrientation);
            for (var steps = 0; steps < 4; steps++)
            {
                var current = RotateConnections(BaseConnections(tile.Type), tile.Orientation + steps);
                if (current == target)
                    return steps;
            }

            return 0;
        }

        private static void ApplyDeterministicSwaps(
            SlicingBoard board,
            (int X, int Y)[] route,
            int swapCount,
            System.Random random)
        {
            if (swapCount <= 0)
                return;

            var candidates = new List<SlicingSwap>();
            for (var routeIndex = 1; routeIndex < route.Length - 2; routeIndex++)
            {
                var first = ToIndex(route[routeIndex].X, route[routeIndex].Y, board.Width);
                var second = ToIndex(route[routeIndex + 1].X, route[routeIndex + 1].Y, board.Width);
                if (board.Tiles[first].Type != board.Tiles[second].Type ||
                    board.Tiles[first].SolutionOrientation != board.Tiles[second].SolutionOrientation)
                {
                    candidates.Add(new SlicingSwap { FirstIndex = first, SecondIndex = second });
                }
            }

            while (board.SolutionSwaps.Count < swapCount && candidates.Count > 0)
            {
                var candidateIndex = random.Next(candidates.Count);
                var candidate = candidates[candidateIndex];
                candidates.RemoveAt(candidateIndex);

                if (board.SolutionSwaps.Any(x =>
                        x.FirstIndex == candidate.FirstIndex || x.FirstIndex == candidate.SecondIndex ||
                        x.SecondIndex == candidate.FirstIndex || x.SecondIndex == candidate.SecondIndex))
                    continue;

                (board.Tiles[candidate.FirstIndex], board.Tiles[candidate.SecondIndex]) =
                    (board.Tiles[candidate.SecondIndex], board.Tiles[candidate.FirstIndex]);
                board.SolutionSwaps.Insert(0, candidate);
            }
        }

        private static SlicingTile CreateDecoy(System.Random random)
        {
            var types = new[]
            {
                SlicingTileType.Straight,
                SlicingTileType.Corner,
                SlicingTileType.Junction,
                SlicingTileType.Cross,
                SlicingTileType.Blocker,
                SlicingTileType.Corrupt
            };

            return new SlicingTile
            {
                Type = types[random.Next(types.Length)],
                Orientation = random.Next(4)
            };
        }

        private static SlicingTileType TypeForConnections(SlicingConnection connections)
        {
            var count = CountConnections(connections);
            if (count == 4) return SlicingTileType.Cross;
            if (count == 3) return SlicingTileType.Junction;
            if (count == 2)
            {
                var opposite = connections == (SlicingConnection.North | SlicingConnection.South) ||
                               connections == (SlicingConnection.East | SlicingConnection.West);
                return opposite ? SlicingTileType.Straight : SlicingTileType.Corner;
            }

            throw new InvalidOperationException($"Unsupported route connection mask: {connections}.");
        }

        private static int FindOrientation(SlicingTileType type, SlicingConnection desired)
        {
            for (var orientation = 0; orientation < 4; orientation++)
            {
                if (RotateConnections(BaseConnections(type), orientation) == desired)
                    return orientation;
            }

            throw new InvalidOperationException($"No orientation for {type} matches {desired}.");
        }

        private static SlicingConnection BaseConnections(SlicingTileType type)
        {
            return type switch
            {
                SlicingTileType.Straight => SlicingConnection.North | SlicingConnection.South,
                SlicingTileType.Corner => SlicingConnection.North | SlicingConnection.East,
                SlicingTileType.Junction => SlicingConnection.North | SlicingConnection.East | SlicingConnection.South,
                SlicingTileType.Cross => SlicingConnection.North | SlicingConnection.East | SlicingConnection.South | SlicingConnection.West,
                SlicingTileType.Entry or SlicingTileType.Core => SlicingConnection.North,
                _ => SlicingConnection.None
            };
        }

        private static SlicingConnection RotateConnections(SlicingConnection connections, int steps)
        {
            var normalized = ((steps % 4) + 4) % 4;
            for (var step = 0; step < normalized; step++)
            {
                var rotated = SlicingConnection.None;
                if (connections.HasFlag(SlicingConnection.North)) rotated |= SlicingConnection.East;
                if (connections.HasFlag(SlicingConnection.East)) rotated |= SlicingConnection.South;
                if (connections.HasFlag(SlicingConnection.South)) rotated |= SlicingConnection.West;
                if (connections.HasFlag(SlicingConnection.West)) rotated |= SlicingConnection.North;
                connections = rotated;
            }

            return connections;
        }

        private static int CountConnections(SlicingConnection connections)
        {
            var value = (int)connections;
            var count = 0;
            while (value > 0)
            {
                count += value & 1;
                value >>= 1;
            }

            return count;
        }

        private static SlicingConnection DirectionTo((int X, int Y) from, (int X, int Y) to)
        {
            if (to.X == from.X && to.Y == from.Y - 1) return SlicingConnection.North;
            if (to.X == from.X + 1 && to.Y == from.Y) return SlicingConnection.East;
            if (to.X == from.X && to.Y == from.Y + 1) return SlicingConnection.South;
            if (to.X == from.X - 1 && to.Y == from.Y) return SlicingConnection.West;
            throw new InvalidOperationException("Authored slicing route contains non-adjacent coordinates.");
        }

        private static IEnumerable<(SlicingConnection Direction, SlicingConnection Opposite, int X, int Y)> NeighborRules()
        {
            yield return (SlicingConnection.North, SlicingConnection.South, 0, -1);
            yield return (SlicingConnection.East, SlicingConnection.West, 1, 0);
            yield return (SlicingConnection.South, SlicingConnection.North, 0, 1);
            yield return (SlicingConnection.West, SlicingConnection.East, -1, 0);
        }

        private static bool IsValidIndex(SlicingBoard board, int index)
        {
            return index >= 0 && index < board.Tiles.Count;
        }

        private static int ToIndex(int x, int y, int width)
        {
            return y * width + x;
        }
    }
}
