using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SlicingService
{
    /// <summary>
    /// Development-time board authoring used by the catalog generator. Live slicing sessions
    /// load checked-in catalog entries through <see cref="Slicing.GetBoard"/> and never call this.
    /// </summary>
    public static class SlicingBoardAuthoring
    {
        public const int BoardsPerTier = 100;

        private static readonly Dictionary<int, (int Width, int Height, int ExtraTrace, int SwapCount)> _tierRules = new()
        {
            [1] = (3, 3, 4, 0),
            [2] = (4, 3, 3, 1),
            [3] = (4, 4, 3, 2),
            [4] = (5, 4, 2, 3),
            [5] = (5, 5, 2, 4)
        };

        private static readonly Dictionary<int, (int X, int Y)[]> _routes = new()
        {
            [1] = new[] { (0, 1), (0, 0), (1, 0), (1, 1), (2, 1) },
            [2] = new[] { (0, 1), (0, 0), (1, 0), (1, 1), (1, 2), (2, 2), (2, 1), (3, 1) },
            [3] = new[] { (0, 2), (0, 3), (1, 3), (1, 2), (1, 1), (2, 1), (2, 0), (3, 0), (3, 1), (3, 2) },
            [4] = new[] { (0, 2), (0, 3), (1, 3), (1, 2), (1, 1), (2, 1), (2, 0), (3, 0), (3, 1), (3, 2), (4, 2) },
            [5] = new[] { (0, 2), (0, 3), (0, 4), (1, 4), (1, 3), (1, 2), (1, 1), (2, 1), (2, 0), (3, 0), (3, 1), (3, 2), (3, 3), (4, 3), (4, 2) }
        };

        public static SlicingBoard GenerateBoard(int tier, int boardNumber)
        {
            if (!_tierRules.TryGetValue(tier, out var rule))
                throw new ArgumentOutOfRangeException(nameof(tier), "Slicing tier must be between 1 and 5.");
            if (boardNumber < 1 || boardNumber > BoardsPerTier)
                throw new ArgumentOutOfRangeException(nameof(boardNumber),
                    $"Slicing board number must be between 1 and {BoardsPerTier}.");

            var random = new System.Random(boardNumber);
            var board = new SlicingBoard
            {
                Tier = tier,
                BoardNumber = boardNumber,
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
                var scrambleSteps = type is SlicingTileType.Entry or SlicingTileType.Core
                    ? 0
                    : random.Next(1, 4);

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
            EnsureMeaningfulStartingState(board);

            var rotationCost = board.Tiles
                .Where(x => x.SolutionIndex >= 0)
                .Sum(Slicing.GetClockwiseSolutionRotationCost);
            var solutionCost = rotationCost + board.SolutionSwaps.Count * 2;

            return new SlicingBoard
            {
                Tier = board.Tier,
                BoardNumber = board.BoardNumber,
                Width = board.Width,
                Height = board.Height,
                BaseTrace = solutionCost + rule.ExtraTrace,
                SolutionActionCost = solutionCost,
                Tiles = board.Tiles,
                SolutionSwaps = board.SolutionSwaps
            };
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

        private static void EnsureMeaningfulStartingState(SlicingBoard board)
        {
            if (!Slicing.IsSolved(board) && !Slicing.CanSolveWithSingleAction(board))
                return;

            var adjustableIndices = board.Tiles
                .Select((tile, index) => (tile, index))
                .Where(x => x.tile.Type is
                    SlicingTileType.Straight or
                    SlicingTileType.Corner or
                    SlicingTileType.Junction)
                .OrderByDescending(x => x.tile.SolutionIndex >= 0)
                .Select(x => x.index)
                .ToList();

            foreach (var index in adjustableIndices)
            {
                for (var turn = 0; turn < 3; turn++)
                {
                    Slicing.RotateClockwise(board, index);
                    if (!Slicing.IsSolved(board) && !Slicing.CanSolveWithSingleAction(board))
                        return;
                }
            }

            throw new InvalidOperationException(
                $"Unable to author a meaningful tier {board.Tier} slicing board {board.BoardId}.");
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
                var tile = new SlicingTile { Type = type, Orientation = orientation };
                if (Slicing.GetConnections(tile) == desired)
                    return orientation;
            }

            throw new InvalidOperationException($"No orientation for {type} matches {desired}.");
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

        private static int ToIndex(int x, int y, int width)
        {
            return y * width + x;
        }
    }
}
