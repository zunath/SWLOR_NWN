using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SlicingService
{
    /// <summary>
    /// Development-time branch-and-bound search for the cheapest way to power a slicing board.
    /// A route visits each cell once. Each route cell either keeps its tile or swaps in the tile
    /// from an adjacent movable cell, then turns clockwise until it links the route's in and out
    /// sides. Rotations cost 1 trace and swaps cost 2, matching live slicing sessions.
    /// </summary>
    public static class SlicingBoardSolver
    {
        public const int Unreachable = 1000;

        private const int North = 0;
        private const int East = 1;
        private const int South = 2;
        private const int West = 3;

        public sealed class SearchLimitExceededException : Exception
        {
            public SearchLimitExceededException(long nodeLimit)
                : base($"Slicing solver exceeded its {nodeLimit:N0}-node search limit.")
            {
            }
        }

        public sealed class Solution
        {
            public int Cost { get; init; }
            /// <summary>Cell indices from entry to core, inclusive.</summary>
            public IReadOnlyList<int> Path { get; init; }
            /// <summary>Clockwise turns applied to the tile that ends in each path cell.</summary>
            public IReadOnlyList<int> Rotations { get; init; }
            /// <summary>Swaps in the order they are applied to the starting board.</summary>
            public IReadOnlyList<SlicingSwap> Swaps { get; init; }
        }

        /// <summary>
        /// Returns the cheapest solution that costs less than <paramref name="costLimit"/>, or null
        /// when none exists. Throws when the search exceeds <paramref name="nodeLimit"/> so callers
        /// never record an unproven optimum.
        /// </summary>
        public static Solution Solve(SlicingBoard board, int costLimit, bool allowSwaps = true, long nodeLimit = 5_000_000)
        {
            var search = new Search(board, allowSwaps, nodeLimit) { Best = costLimit };
            search.Run();
            return search.BestSolution;
        }

        /// <summary>
        /// Counts distinct entry-to-core cell sequences that can be powered for at most
        /// <paramref name="maxCost"/> trace, stopping once <paramref name="cap"/> are found.
        /// </summary>
        public static int CountRoutes(SlicingBoard board, int maxCost, int cap, bool allowSwaps = true, long nodeLimit = 5_000_000)
        {
            var search = new Search(board, allowSwaps, nodeLimit)
            {
                Best = maxCost + 1,
                RouteCap = cap
            };
            search.Run();
            return search.Routes.Count;
        }

        /// <summary>Clockwise turns before a tile links both sides, or <see cref="Unreachable"/>.</summary>
        public static int GetLinkTurns(SlicingTileType type, int orientation, SlicingConnection first, SlicingConnection second)
        {
            return LinkTurns(BaseMask(type), orientation, SideOf(first), SideOf(second));
        }

        private static int LinkTurns(int baseMask, int orientation, int firstSide, int secondSide)
        {
            var needed = (1 << firstSide) | (1 << secondSide);
            for (var turns = 0; turns < 4; turns++)
            {
                if ((Rotate(baseMask, orientation + turns) & needed) == needed)
                    return turns;
            }

            return Unreachable;
        }

        private static int BaseMask(SlicingTileType type) => (int)Slicing.GetConnections(new SlicingTile { Type = type });

        private static int Rotate(int mask, int steps)
        {
            var normalized = ((steps % 4) + 4) % 4;
            return ((mask << normalized) | (mask >> (4 - normalized))) & 0xF;
        }

        private static int SideOf(SlicingConnection connection) => connection switch
        {
            SlicingConnection.North => North,
            SlicingConnection.East => East,
            SlicingConnection.South => South,
            SlicingConnection.West => West,
            _ => throw new ArgumentOutOfRangeException(nameof(connection))
        };

        private static int Opposite(int side) => (side + 2) % 4;

        private sealed class Search
        {
            private readonly int _width;
            private readonly int _height;
            private readonly int _cellCount;
            private readonly int _entry;
            private readonly int _core;
            private readonly int _coreSide;
            private readonly int _firstCell;
            private readonly int _firstSide;
            private readonly bool _allowSwaps;
            private readonly long _nodeLimit;
            private readonly int[] _tileAt;
            private readonly int[,] _tileTurns;
            private readonly bool[] _fixed;
            private readonly bool[] _visited;
            private readonly int[,] _neighbor;
            private readonly int[,] _distance;
            private readonly ulong[,] _zobrist;
            private readonly Dictionary<ulong, int[]> _heuristicCache = new();
            private readonly List<int> _path = new();
            private readonly List<int> _rotations = new();
            private readonly List<SlicingSwap> _swaps = new();

            private ulong _hash;
            private int[] _heuristic;
            private long _nodes;

            public int Best { get; set; }
            public Solution BestSolution { get; private set; }
            public int RouteCap { get; init; }
            public HashSet<string> Routes { get; } = new();

            private bool IsCounting => RouteCap > 0;

            public Search(SlicingBoard board, bool allowSwaps, long nodeLimit)
            {
                _width = board.Width;
                _height = board.Height;
                _cellCount = board.Tiles.Count;
                _allowSwaps = allowSwaps;
                _nodeLimit = nodeLimit;
                _entry = board.Tiles.FindIndex(tile => tile.Type == SlicingTileType.Entry);
                _core = board.Tiles.FindIndex(tile => tile.Type == SlicingTileType.Core);
                if (_entry < 0 || _core < 0)
                    throw new InvalidOperationException($"Slicing board {board.BoardId} needs an entry and a core.");

                _tileAt = new int[_cellCount];
                _tileTurns = new int[_cellCount, 16];
                _fixed = new bool[_cellCount];
                _visited = new bool[_cellCount];
                _neighbor = new int[_cellCount, 4];
                _distance = new int[_cellCount, _cellCount];
                _zobrist = new ulong[_cellCount, _cellCount];

                var random = new System.Random(7919);
                for (var cell = 0; cell < _cellCount; cell++)
                {
                    var tile = board.Tiles[cell];
                    _tileAt[cell] = cell;
                    _fixed[cell] = tile.Type is SlicingTileType.Entry or SlicingTileType.Core;
                    var baseMask = BaseMask(tile.Type);
                    for (var first = 0; first < 4; first++)
                    for (var second = 0; second < 4; second++)
                    {
                        _tileTurns[cell, first * 4 + second] = first == second
                            ? Unreachable
                            : LinkTurns(baseMask, tile.Orientation, first, second);
                    }

                    var x = cell % _width;
                    var y = cell / _width;
                    _neighbor[cell, North] = y > 0 ? cell - _width : -1;
                    _neighbor[cell, East] = x < _width - 1 ? cell + 1 : -1;
                    _neighbor[cell, South] = y < _height - 1 ? cell + _width : -1;
                    _neighbor[cell, West] = x > 0 ? cell - 1 : -1;

                    for (var other = 0; other < _cellCount; other++)
                    {
                        _distance[cell, other] = Math.Abs(other % _width - x) + Math.Abs(other / _width - y);
                        _zobrist[cell, other] = (ulong)random.NextInt64() ^ ((ulong)random.NextInt64() << 32);
                    }
                }

                for (var cell = 0; cell < _cellCount; cell++)
                    _hash ^= _zobrist[cell, _tileAt[cell]];

                _coreSide = SingleSide(board.Tiles[_core]);
                var entrySide = SingleSide(board.Tiles[_entry]);
                _firstCell = _neighbor[_entry, entrySide];
                _firstSide = Opposite(entrySide);
            }

            public void Run()
            {
                if (_firstCell < 0 || _firstCell == _core || _fixed[_firstCell])
                    return;

                _visited[_entry] = true;
                _path.Add(_entry);
                _rotations.Add(0);
                _heuristic = Heuristic();
                Visit(_firstCell, _firstSide, 0);
            }

            private void Visit(int cell, int inSide, int cost)
            {
                if (++_nodes > _nodeLimit)
                    throw new SearchLimitExceededException(_nodeLimit);
                if (IsCounting && Routes.Count >= RouteCap)
                    return;
                if (cost + _heuristic[cell * 4 + inSide] >= Best)
                    return;

                _visited[cell] = true;
                var options = new List<(int Estimate, int OutSide, int Next, int Partner, int Turns)>();
                for (var outSide = 0; outSide < 4; outSide++)
                {
                    if (outSide == inSide)
                        continue;

                    var next = _neighbor[cell, outSide];
                    if (next < 0 || next == _entry || _visited[next])
                        continue;
                    if (next == _core && _coreSide != Opposite(outSide))
                        continue;

                    var remaining = next == _core ? 0 : _heuristic[next * 4 + Opposite(outSide)];
                    var turns = _tileTurns[_tileAt[cell], inSide * 4 + outSide];
                    if (turns < Unreachable)
                        options.Add((cost + turns + remaining, outSide, next, -1, turns));

                    if (!_allowSwaps)
                        continue;

                    for (var swapSide = 0; swapSide < 4; swapSide++)
                    {
                        var partner = _neighbor[cell, swapSide];
                        if (partner < 0 || _fixed[partner] || _visited[partner])
                            continue;

                        var swappedTurns = _tileTurns[_tileAt[partner], inSide * 4 + outSide];
                        if (swappedTurns < Unreachable)
                            options.Add((cost + 2 + swappedTurns + remaining, outSide, next, partner, swappedTurns));
                    }
                }

                options.Sort((left, right) => left.Estimate.CompareTo(right.Estimate));
                foreach (var option in options)
                {
                    var stepCost = option.Turns + (option.Partner >= 0 ? 2 : 0);
                    if (cost + stepCost >= Best)
                        continue;

                    if (option.Partner < 0)
                    {
                        Step(cell, option.OutSide, option.Next, cost + stepCost, option.Turns);
                        continue;
                    }

                    var previousHeuristic = _heuristic;
                    SwapTiles(cell, option.Partner);
                    _swaps.Add(new SlicingSwap { FirstIndex = cell, SecondIndex = option.Partner });
                    _heuristic = Heuristic();

                    Step(cell, option.OutSide, option.Next, cost + stepCost, option.Turns);

                    _heuristic = previousHeuristic;
                    _swaps.RemoveAt(_swaps.Count - 1);
                    SwapTiles(cell, option.Partner);
                }

                _visited[cell] = false;
            }

            private void Step(int cell, int outSide, int next, int cost, int turns)
            {
                _path.Add(cell);
                _rotations.Add(turns);

                if (next == _core)
                    RecordSolution(cost);
                else
                    Visit(next, Opposite(outSide), cost);

                _path.RemoveAt(_path.Count - 1);
                _rotations.RemoveAt(_rotations.Count - 1);
            }

            private void RecordSolution(int cost)
            {
                if (IsCounting)
                {
                    Routes.Add(string.Join(",", _path));
                    return;
                }

                if (cost >= Best)
                    return;

                Best = cost;
                BestSolution = new Solution
                {
                    Cost = cost,
                    Path = new List<int>(_path) { _core },
                    Rotations = new List<int>(_rotations) { 0 },
                    Swaps = new List<SlicingSwap>(_swaps)
                };
            }

            private void SwapTiles(int first, int second)
            {
                _hash ^= _zobrist[first, _tileAt[first]] ^ _zobrist[second, _tileAt[second]];
                (_tileAt[first], _tileAt[second]) = (_tileAt[second], _tileAt[first]);
                _hash ^= _zobrist[first, _tileAt[first]] ^ _zobrist[second, _tileAt[second]];
            }

            /// <summary>
            /// Admissible remaining-cost bound for every (cell, entered side) state. A cell may use
            /// any movable tile at Manhattan distance d for d + turns: every swap costs 2 trace and
            /// moves two tiles one step, so total swap trace covers the summed tile displacement.
            /// Walks may revisit cells, which only lowers the bound.
            /// </summary>
            private int[] Heuristic()
            {
                if (_heuristicCache.TryGetValue(_hash, out var cached))
                    return cached;

                var cellCost = new int[_cellCount * 16];
                for (var cell = 0; cell < _cellCount; cell++)
                {
                    for (var pair = 0; pair < 16; pair++)
                        cellCost[cell * 16 + pair] = Unreachable;
                    if (_fixed[cell])
                        continue;

                    for (var source = 0; source < _cellCount; source++)
                    {
                        if (_fixed[source] || (!_allowSwaps && source != cell))
                            continue;

                        var distance = _distance[source, cell];
                        var tile = _tileAt[source];
                        for (var pair = 0; pair < 16; pair++)
                        {
                            var candidate = distance + _tileTurns[tile, pair];
                            if (candidate < cellCost[cell * 16 + pair])
                                cellCost[cell * 16 + pair] = candidate;
                        }
                    }
                }

                var stateCount = _cellCount * 4;
                var distanceToCore = new int[stateCount];
                var done = new bool[stateCount];
                Array.Fill(distanceToCore, Unreachable);

                for (var cell = 0; cell < _cellCount; cell++)
                {
                    if (_fixed[cell])
                        continue;

                    for (var outSide = 0; outSide < 4; outSide++)
                    {
                        if (_neighbor[cell, outSide] != _core || _coreSide != Opposite(outSide))
                            continue;

                        for (var inSide = 0; inSide < 4; inSide++)
                        {
                            if (inSide == outSide)
                                continue;
                            var state = cell * 4 + inSide;
                            distanceToCore[state] = Math.Min(distanceToCore[state], cellCost[cell * 16 + inSide * 4 + outSide]);
                        }
                    }
                }

                // Reverse Dijkstra: state (cell, inSide) reaches (next, opposite(outSide)) for cellCost.
                while (true)
                {
                    var current = -1;
                    for (var state = 0; state < stateCount; state++)
                    {
                        if (!done[state] && distanceToCore[state] < Unreachable &&
                            (current < 0 || distanceToCore[state] < distanceToCore[current]))
                            current = state;
                    }

                    if (current < 0)
                        break;

                    done[current] = true;
                    var currentCell = current / 4;
                    var enteredSide = current % 4;
                    var previous = _neighbor[currentCell, enteredSide];
                    if (previous < 0 || _fixed[previous])
                        continue;

                    var previousOut = Opposite(enteredSide);
                    for (var previousIn = 0; previousIn < 4; previousIn++)
                    {
                        if (previousIn == previousOut)
                            continue;

                        var state = previous * 4 + previousIn;
                        var candidate = distanceToCore[current] + cellCost[previous * 16 + previousIn * 4 + previousOut];
                        if (!done[state] && candidate < distanceToCore[state])
                            distanceToCore[state] = candidate;
                    }
                }

                if (_heuristicCache.Count >= 100_000)
                    _heuristicCache.Clear();
                _heuristicCache[_hash] = distanceToCore;
                return distanceToCore;
            }

            private static int SingleSide(SlicingTile tile)
            {
                return SideOf(Slicing.GetConnections(tile));
            }
        }
    }
}
