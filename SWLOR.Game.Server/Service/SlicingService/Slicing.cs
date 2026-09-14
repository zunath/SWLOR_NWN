using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SlicingService
{
    public static class Slicing
    {
        public static int GetBoardCount(int tier) => SlicingBoardCatalog.GetBoardCount(tier);

        public static SlicingBoard GetBoard(int tier, int boardNumber) =>
            SlicingBoardCatalog.GetBoard(tier, boardNumber);

        public static int MapSelectionToBoardNumber(int tier, int selection)
        {
            var count = GetBoardCount(tier);
            var zeroBased = ((long)selection - 1) % count;
            if (zeroBased < 0)
                zeroBased += count;
            return (int)zeroBased + 1;
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
            if (board.Tiles[index].Type is SlicingTileType.Entry or SlicingTileType.Core)
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

        public static bool CanSolveWithSingleAction(SlicingBoard board)
        {
            for (var index = 0; index < board.Tiles.Count; index++)
            {
                if (board.Tiles[index].Type is not (
                        SlicingTileType.Straight or
                        SlicingTileType.Corner or
                        SlicingTileType.Junction))
                    continue;

                var rotated = board.Clone();
                if (RotateClockwise(rotated, index) && IsSolved(rotated))
                    return true;
            }

            for (var first = 0; first < board.Tiles.Count; first++)
            {
                for (var second = first + 1; second < board.Tiles.Count; second++)
                {
                    if (!AreAdjacent(board, first, second) ||
                        board.Tiles[first].Type is SlicingTileType.Entry or SlicingTileType.Core ||
                        board.Tiles[second].Type is SlicingTileType.Entry or SlicingTileType.Core)
                        continue;

                    var swapped = board.Clone();
                    if (SwapAdjacent(swapped, first, second) && IsSolved(swapped))
                        return true;
                }
            }

            return false;
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
