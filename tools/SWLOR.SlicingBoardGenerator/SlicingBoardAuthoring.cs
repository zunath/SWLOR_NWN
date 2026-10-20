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

        private const int MaxAttemptsPerBoard = 400;

        /// <param name="RouteCells">Inclusive range of authored route cells, counting START and GOAL.</param>
        /// <param name="MinEndpointDistance">Smallest Manhattan distance between START and GOAL.</param>
        /// <param name="RouteJunctionPercent">Chance a route tile is a junction whose spare arm leaks power into decoys.</param>
        /// <param name="MinSolutionCost">Smallest optimal trace cost a board may have.</param>
        /// <param name="MaxRoutesInTrace">Most distinct paths a player may power within the base trace.</param>
        private sealed record TierRule(
            int Width,
            int Height,
            int ExtraTrace,
            int SwapCount,
            (int Min, int Max) RouteCells,
            int MinEndpointDistance,
            int RouteJunctionPercent,
            int MinSolutionCost,
            int MaxRoutesInTrace);

        private static readonly Dictionary<int, TierRule> _tierRules = new()
        {
            [1] = new(3, 3, 4, 0, (5, 7), 2, 0, 5, 3),
            [2] = new(4, 3, 3, 1, (6, 9), 3, 15, 7, 3),
            [3] = new(4, 4, 3, 2, (8, 11), 4, 20, 9, 4),
            [4] = new(5, 4, 2, 3, (9, 13), 4, 25, 11, 4),
            [5] = new(5, 5, 2, 4, (11, 15), 5, 25, 13, 5)
        };

        // Decoys are mostly pipe pieces so false branches look like the route.
        private static readonly (SlicingTileType Type, int Weight)[] _decoyWeights =
        {
            (SlicingTileType.Straight, 3),
            (SlicingTileType.Corner, 4),
            (SlicingTileType.Junction, 2),
            (SlicingTileType.Cross, 1),
            (SlicingTileType.Blocker, 1),
            (SlicingTileType.Corrupt, 1)
        };

        public static SlicingBoard GenerateBoard(int tier, int boardNumber)
        {
            if (!_tierRules.TryGetValue(tier, out var rule))
                throw new ArgumentOutOfRangeException(nameof(tier), "Slicing tier must be between 1 and 5.");
            if (boardNumber < 1 || boardNumber > BoardsPerTier)
                throw new ArgumentOutOfRangeException(nameof(boardNumber),
                    $"Slicing board number must be between 1 and {BoardsPerTier}.");

            for (var attempt = 0; attempt < MaxAttemptsPerBoard; attempt++)
            {
                var random = new System.Random(tier * 1_000_003 + boardNumber * 10_007 + attempt);
                var board = TryAuthor(tier, boardNumber, rule, random);
                if (board != null)
                    return board;
            }

            throw new InvalidOperationException(
                $"Unable to author a meaningful tier {tier} slicing board T{tier}-{boardNumber:000}.");
        }

        private static SlicingBoard TryAuthor(int tier, int boardNumber, TierRule rule, System.Random random)
        {
            var routeLength = random.Next(rule.RouteCells.Min, rule.RouteCells.Max + 1);
            var route = RandomRoute(rule.Width, rule.Height, routeLength, rule.MinEndpointDistance, random);
            if (route == null)
                return null;

            var board = new SlicingBoard
            {
                Tier = tier,
                BoardNumber = boardNumber,
                Width = rule.Width,
                Height = rule.Height
            };

            for (var index = 0; index < rule.Width * rule.Height; index++)
                board.Tiles.Add(CreateDecoy(random));

            var routeIndices = route.Select(cell => ToIndex(cell.X, cell.Y, rule.Width)).ToList();
            var routeSides = new Dictionary<int, (SlicingConnection In, SlicingConnection Out)>();
            var authoredCost = 0;
            for (var routeIndex = 0; routeIndex < route.Count; routeIndex++)
            {
                var coordinate = route[routeIndex];
                var index = routeIndices[routeIndex];
                var inSide = routeIndex > 0 ? DirectionTo(coordinate, route[routeIndex - 1]) : SlicingConnection.None;
                var outSide = routeIndex < route.Count - 1 ? DirectionTo(coordinate, route[routeIndex + 1]) : SlicingConnection.None;

                if (routeIndex == 0 || routeIndex == route.Count - 1)
                {
                    var type = routeIndex == 0 ? SlicingTileType.Entry : SlicingTileType.Core;
                    board.Tiles[index] = new SlicingTile
                    {
                        Type = type,
                        Orientation = FindOrientations(type, inSide | outSide).First()
                    };
                    continue;
                }

                routeSides[index] = (inSide, outSide);
                var tileType = random.Next(100) < rule.RouteJunctionPercent
                    ? SlicingTileType.Junction
                    : TypeForConnections(inSide | outSide);
                var orientations = FindOrientations(tileType, inSide | outSide).ToList();
                var home = orientations[random.Next(orientations.Count)];
                var tile = new SlicingTile
                {
                    Type = tileType,
                    Orientation = (home + random.Next(1, 4)) % 4
                };
                board.Tiles[index] = tile;
                authoredCost += SlicingBoardSolver.GetLinkTurns(tile.Type, tile.Orientation, inSide, outSide);
            }

            authoredCost += ApplyAuthoredSwaps(board, routeIndices, routeSides, rule.SwapCount, random) * 2;

            if (Slicing.IsSolved(board) || Slicing.CanSolveWithSingleAction(board))
                return null;

            SlicingBoardSolver.Solution solution;
            try
            {
                // The authored repair is always within the solver's model, so a solution exists.
                solution = SlicingBoardSolver.Solve(board, authoredCost + 1);
                if (solution == null)
                    throw new InvalidOperationException($"Solver missed the authored repair for {board.BoardId}.");
                if (solution.Cost < rule.MinSolutionCost)
                    return null;

                // Every board past tier 1 must need a swap within the trace it starts with.
                var baseTrace = solution.Cost + rule.ExtraTrace;
                if (rule.SwapCount > 0 &&
                    SlicingBoardSolver.Solve(board, baseTrace + 1, allowSwaps: false) != null)
                    return null;
                if (SlicingBoardSolver.CountRoutes(board, baseTrace, rule.MaxRoutesInTrace + 1) > rule.MaxRoutesInTrace)
                    return null;
            }
            catch (SlicingBoardSolver.SearchLimitExceededException)
            {
                return null;
            }

            RecordSolution(board, solution);
            var finished = new SlicingBoard
            {
                Tier = board.Tier,
                BoardNumber = board.BoardNumber,
                Width = board.Width,
                Height = board.Height,
                BaseTrace = solution.Cost + rule.ExtraTrace,
                SolutionActionCost = solution.Cost,
                Tiles = board.Tiles,
                SolutionSwaps = solution.Swaps.ToList()
            };

            VerifyRecordedSolution(finished);
            return finished;
        }

        /// <summary>
        /// Self-avoiding walk with backtracking. Returns null when no walk of the requested
        /// length exists from the chosen start with START and GOAL far enough apart.
        /// </summary>
        private static List<(int X, int Y)> RandomRoute(int width, int height, int length, int minEndpointDistance, System.Random random)
        {
            var start = (X: random.Next(width), Y: random.Next(height));
            var path = new List<(int X, int Y)> { start };
            var visited = new HashSet<(int X, int Y)> { start };
            var budget = 20_000;

            bool Walk()
            {
                if (--budget < 0)
                    return false;
                if (path.Count == length)
                {
                    var end = path[^1];
                    return Math.Abs(end.X - start.X) + Math.Abs(end.Y - start.Y) >= minEndpointDistance;
                }

                var current = path[^1];
                var steps = new List<(int X, int Y)>
                {
                    (current.X, current.Y - 1),
                    (current.X + 1, current.Y),
                    (current.X, current.Y + 1),
                    (current.X - 1, current.Y)
                };
                Shuffle(steps, random);
                foreach (var step in steps)
                {
                    if (step.X < 0 || step.X >= width || step.Y < 0 || step.Y >= height || !visited.Add(step))
                        continue;

                    path.Add(step);
                    if (Walk())
                        return true;
                    path.RemoveAt(path.Count - 1);
                    visited.Remove(step);
                }

                return false;
            }

            return Walk() ? path : null;
        }

        /// <summary>
        /// Moves route pieces out of place. A displaced piece trades with an adjacent decoy that
        /// cannot carry the route; an exchanged pair trades two route pieces that do not fit each
        /// other's cell. Returns the number of swaps applied.
        /// </summary>
        private static int ApplyAuthoredSwaps(
            SlicingBoard board,
            IReadOnlyList<int> routeIndices,
            IReadOnlyDictionary<int, (SlicingConnection In, SlicingConnection Out)> routeSides,
            int swapCount,
            System.Random random)
        {
            var routeSet = routeIndices.ToHashSet();
            var used = new HashSet<int>();
            var applied = 0;

            var interior = routeSides.Keys.ToList();
            Shuffle(interior, random);

            for (var slot = 0; slot < swapCount; slot++)
            {
                var preferDisplaced = random.Next(100) < 60;
                if (!(preferDisplaced && TryDisplace()) && !TryExchange() && !TryDisplace())
                    break;
                applied++;
            }

            return applied;

            bool TryDisplace()
            {
                foreach (var cell in interior.Where(cell => !used.Contains(cell)))
                {
                    var partners = Neighbors(board, cell)
                        .Where(partner => !routeSet.Contains(partner) && !used.Contains(partner))
                        .ToList();
                    if (partners.Count == 0)
                        continue;

                    var partner = partners[random.Next(partners.Count)];
                    var sides = routeSides[cell];
                    board.Tiles[partner] = CreateBlockingDecoy(sides.In | sides.Out, random);
                    Swap(cell, partner);
                    return true;
                }

                return false;
            }

            bool TryExchange()
            {
                foreach (var cell in interior.Where(cell => !used.Contains(cell)))
                {
                    var partners = Neighbors(board, cell)
                        .Where(partner => routeSides.ContainsKey(partner) && !used.Contains(partner))
                        .Where(partner => !Fits(board.Tiles[partner], routeSides[cell]) ||
                                          !Fits(board.Tiles[cell], routeSides[partner]))
                        .ToList();
                    if (partners.Count == 0)
                        continue;

                    Swap(cell, partners[random.Next(partners.Count)]);
                    return true;
                }

                return false;
            }

            void Swap(int first, int second)
            {
                (board.Tiles[first], board.Tiles[second]) = (board.Tiles[second], board.Tiles[first]);
                used.Add(first);
                used.Add(second);
            }
        }

        private static bool Fits(SlicingTile tile, (SlicingConnection In, SlicingConnection Out) sides)
        {
            return SlicingBoardSolver.GetLinkTurns(tile.Type, tile.Orientation, sides.In, sides.Out) < SlicingBoardSolver.Unreachable;
        }

        /// <summary>Clears authored metadata and stamps the solver's optimal repair onto the tiles.</summary>
        private static void RecordSolution(SlicingBoard board, SlicingBoardSolver.Solution solution)
        {
            foreach (var tile in board.Tiles)
            {
                tile.SolutionIndex = -1;
                tile.RouteOrder = -1;
                tile.SolutionOrientation = 0;
            }

            foreach (var swap in solution.Swaps)
                Slicing.SwapAdjacent(board, swap.FirstIndex, swap.SecondIndex);

            for (var routeOrder = 0; routeOrder < solution.Path.Count; routeOrder++)
            {
                var index = solution.Path[routeOrder];
                var tile = board.Tiles[index];
                tile.SolutionIndex = index;
                tile.RouteOrder = routeOrder;
                tile.SolutionOrientation = (tile.Orientation + solution.Rotations[routeOrder]) % 4;
            }

            for (var swapIndex = solution.Swaps.Count - 1; swapIndex >= 0; swapIndex--)
            {
                var swap = solution.Swaps[swapIndex];
                Slicing.SwapAdjacent(board, swap.FirstIndex, swap.SecondIndex);
            }
        }

        private static void VerifyRecordedSolution(SlicingBoard board)
        {
            var replay = board.Clone();
            var spent = 0;
            foreach (var swap in replay.SolutionSwaps)
            {
                if (!Slicing.SwapAdjacent(replay, swap.FirstIndex, swap.SecondIndex))
                    throw new InvalidOperationException($"Recorded swap is illegal on {board.BoardId}.");
                spent += 2;
            }

            for (var index = 0; index < replay.Tiles.Count; index++)
            {
                var tile = replay.Tiles[index];
                if (tile.SolutionIndex < 0)
                    continue;
                if (tile.SolutionIndex != index)
                    throw new InvalidOperationException($"Route tile is not home after swaps on {board.BoardId}.");

                var turns = Slicing.GetClockwiseSolutionRotationCost(tile);
                tile.Orientation = (tile.Orientation + turns) % 4;
                spent += turns;
            }

            if (spent != board.SolutionActionCost || !Slicing.IsSolved(replay))
                throw new InvalidOperationException($"Recorded solution does not solve {board.BoardId}.");
        }

        private static IEnumerable<int> Neighbors(SlicingBoard board, int index)
        {
            var x = index % board.Width;
            var y = index / board.Width;
            if (y > 0) yield return index - board.Width;
            if (x < board.Width - 1) yield return index + 1;
            if (y < board.Height - 1) yield return index + board.Width;
            if (x > 0) yield return index - 1;
        }

        private static SlicingTile CreateDecoy(System.Random random)
        {
            var roll = random.Next(_decoyWeights.Sum(entry => entry.Weight));
            foreach (var (type, weight) in _decoyWeights)
            {
                if (roll < weight)
                    return new SlicingTile { Type = type, Orientation = random.Next(4) };
                roll -= weight;
            }

            throw new InvalidOperationException("Decoy weights are empty.");
        }

        /// <summary>A decoy that cannot link the given sides in any orientation.</summary>
        private static SlicingTile CreateBlockingDecoy(SlicingConnection sides, System.Random random)
        {
            var mismatchedPipe = TypeForConnections(sides) == SlicingTileType.Straight
                ? SlicingTileType.Corner
                : SlicingTileType.Straight;
            var roll = random.Next(8);
            var type = roll < 6
                ? mismatchedPipe
                : roll == 6
                    ? SlicingTileType.Blocker
                    : SlicingTileType.Corrupt;
            return new SlicingTile { Type = type, Orientation = random.Next(4) };
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

        /// <summary>Orientations whose connections include every desired side (exactly, for sockets).</summary>
        private static IEnumerable<int> FindOrientations(SlicingTileType type, SlicingConnection desired)
        {
            var exact = type is SlicingTileType.Entry or SlicingTileType.Core;
            for (var orientation = 0; orientation < 4; orientation++)
            {
                var connections = Slicing.GetConnections(new SlicingTile { Type = type, Orientation = orientation });
                if (exact ? connections == desired : (connections & desired) == desired)
                    yield return orientation;
            }
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

        private static void Shuffle<T>(IList<T> items, System.Random random)
        {
            for (var index = items.Count - 1; index > 0; index--)
            {
                var other = random.Next(index + 1);
                (items[index], items[other]) = (items[other], items[index]);
            }
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
