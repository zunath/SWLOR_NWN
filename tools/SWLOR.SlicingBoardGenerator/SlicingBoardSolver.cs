using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SlicingService
{
    /// <summary>
    /// Development-time exact search for the cheapest way to power a slicing board. Rotations
    /// cost 1 trace and adjacent swaps cost 2, matching live slicing sessions. A rotation travels
    /// with its tile, so every solution can be reordered as swaps first, then rotations: the
    /// search explores every legal swap sequence and, on each resulting arrangement, finds the
    /// cheapest rotation-only route by depth-first search over simple paths.
    /// </summary>
    public static class SlicingBoardSolver
    {
        public const int Unreachable = 1000;

        private const int North = 0;
        private const int East = 1;
        private const int South = 2;
        private const int West = 3;
        private const int PairCount = 6;
        private const int MaxBucket = 255;

        // Unordered side pairs: N-E, N-S, N-W, E-S, E-W, S-W. Same-side lookups stay -1.
        private static readonly int[] _pairIndex = BuildPairIndex();

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
        /// when none exists. <paramref name="knownMinimum"/> is a proven lower bound on the
        /// cheapest cost that lets the search skip work. Throws when the search exceeds
        /// <paramref name="nodeLimit"/> so callers never record an unproven optimum.
        /// </summary>
        public static Solution Solve(
            SlicingBoard board,
            int costLimit,
            bool allowSwaps = true,
            long nodeLimit = 5_000_000,
            int knownMinimum = 0)
        {
            var search = new Search(board, allowSwaps, nodeLimit) { Best = costLimit };
            search.Run(knownMinimum);
            return search.BestSolution;
        }

        /// <summary>
        /// Counts distinct entry-to-core cell paths that can be powered for at most
        /// <paramref name="maxCost"/> trace, stopping once more than <paramref name="cap"/> are
        /// found. Candidate paths come from an admissible assignment bound; each candidate is then
        /// confirmed by an exact search over swap sequences for that path.
        /// </summary>
        public static int CountRoutes(SlicingBoard board, int maxCost, int cap, bool allowSwaps = true, long nodeLimit = 20_000_000)
        {
            var search = new Search(board, allowSwaps, nodeLimit);
            return search.CountRoutes(maxCost, cap);
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

        private static int[] BuildPairIndex()
        {
            var index = new int[16];
            Array.Fill(index, -1);
            var next = 0;
            for (var first = 0; first < 4; first++)
            for (var second = first + 1; second < 4; second++)
            {
                index[first * 4 + second] = next;
                index[second * 4 + first] = next;
                next++;
            }

            return index;
        }

        private sealed class Search
        {
            private readonly int _cellCount;
            private readonly int _entry;
            private readonly int _core;
            private readonly int _coreSide;
            private readonly int _firstCell;
            private readonly int _firstSide;
            private readonly int _rootState;
            private readonly bool _allowSwaps;
            private readonly long _nodeLimit;
            private readonly int[] _tileAt;
            private readonly int[,] _tileTurns;
            private readonly bool[] _fixed;
            private readonly bool[] _visited;
            private readonly int[,] _neighbor;
            private readonly int[,] _distance;
            private readonly ulong[,] _zobrist;
            private readonly int[] _tileClass;
            private readonly (int First, int Second)[] _swapEdges;
            private readonly Dictionary<ulong, int> _arrangementCost = new();
            private readonly List<int> _path = new();
            private readonly List<int> _rotations = new();
            private readonly List<SlicingSwap> _swaps = new();

            // Reused bound buffers. The rotation-only bound stays live while Visit runs; the
            // movement bound is consumed immediately.
            private readonly int[] _routeBound;
            private readonly int[] _moveBound;
            private readonly int[] _cellCost;
            private readonly bool[] _done;
            private readonly List<int>[] _buckets;

            private readonly List<int> _pathPairs = new();
            private readonly Dictionary<ulong, int> _pathArrangementCost = new();
            private readonly int[] _movableCells;

            private ulong _hash;
            private int _routeLimit;
            private int _routeCount;
            private int _routeCap;
            private long _nodes;

            public int Best { get; set; }
            public Solution BestSolution { get; private set; }

            public Search(SlicingBoard board, bool allowSwaps, long nodeLimit)
            {
                var width = board.Width;
                _cellCount = board.Tiles.Count;
                _allowSwaps = allowSwaps;
                _nodeLimit = nodeLimit;
                _entry = board.Tiles.FindIndex(tile => tile.Type == SlicingTileType.Entry);
                _core = board.Tiles.FindIndex(tile => tile.Type == SlicingTileType.Core);
                if (_entry < 0 || _core < 0)
                    throw new InvalidOperationException($"Slicing board {board.BoardId} needs an entry and a core.");

                _tileAt = new int[_cellCount];
                _tileTurns = new int[_cellCount, PairCount];
                _fixed = new bool[_cellCount];
                _visited = new bool[_cellCount];
                _neighbor = new int[_cellCount, 4];
                _distance = new int[_cellCount, _cellCount];
                _zobrist = new ulong[_cellCount, _cellCount];
                _tileClass = new int[_cellCount];
                _routeBound = new int[_cellCount * 4];
                _moveBound = new int[_cellCount * 4];
                _cellCost = new int[_cellCount * PairCount];
                _done = new bool[_cellCount * 4];
                _buckets = Enumerable.Range(0, MaxBucket + 1).Select(_ => new List<int>()).ToArray();

                var random = new System.Random(7919);
                for (var cell = 0; cell < _cellCount; cell++)
                {
                    var tile = board.Tiles[cell];
                    _tileAt[cell] = cell;
                    _fixed[cell] = tile.Type is SlicingTileType.Entry or SlicingTileType.Core;
                    var baseMask = BaseMask(tile.Type);
                    for (var first = 0; first < 4; first++)
                    for (var second = first + 1; second < 4; second++)
                        _tileTurns[cell, _pairIndex[first * 4 + second]] = LinkTurns(baseMask, tile.Orientation, first, second);

                    var x = cell % width;
                    var y = cell / width;
                    _neighbor[cell, North] = y > 0 ? cell - width : -1;
                    _neighbor[cell, East] = x < width - 1 ? cell + 1 : -1;
                    _neighbor[cell, South] = y < board.Height - 1 ? cell + width : -1;
                    _neighbor[cell, West] = x > 0 ? cell - 1 : -1;

                    for (var other = 0; other < _cellCount; other++)
                    {
                        _distance[cell, other] = Math.Abs(other % width - x) + Math.Abs(other / width - y);
                        _zobrist[cell, other] = (ulong)random.NextInt64() ^ ((ulong)random.NextInt64() << 32);
                    }
                }

                // Tiles that link the same sides for the same turns are interchangeable, so the
                // arrangement cache keys on that signature rather than on tile identity.
                var classes = new Dictionary<string, int>();
                for (var tile = 0; tile < _cellCount; tile++)
                {
                    var signature = string.Join(",", Enumerable.Range(0, PairCount).Select(pair => _tileTurns[tile, pair]));
                    if (!classes.TryGetValue(signature, out var tileClass))
                        classes[signature] = tileClass = classes.Count;
                    _tileClass[tile] = tileClass;
                }

                for (var cell = 0; cell < _cellCount; cell++)
                    _hash ^= _zobrist[cell, _tileClass[_tileAt[cell]]];

                _movableCells = Enumerable.Range(0, _cellCount).Where(cell => !_fixed[cell]).ToArray();
                _swapEdges = Enumerable.Range(0, _cellCount)
                    .Where(cell => !_fixed[cell])
                    .SelectMany(cell => new[] { _neighbor[cell, East], _neighbor[cell, South] }
                        .Where(other => other >= 0 && !_fixed[other])
                        .Select(other => (cell, other)))
                    .ToArray();

                _coreSide = SingleSide(board.Tiles[_core]);
                var entrySide = SingleSide(board.Tiles[_entry]);
                _firstCell = _neighbor[_entry, entrySide];
                _firstSide = Opposite(entrySide);
                _rootState = _firstCell * 4 + _firstSide;
            }

            /// <summary>
            /// Rotation-only solves search once under their limit. Swap-enabled solves deepen the
            /// cost limit one trace at a time, so the first solution found is optimal and no
            /// arrangement costlier than the optimum is ever explored.
            /// </summary>
            public void Run(int knownMinimum)
            {
                if (_firstCell < 0 || _firstCell == _core || _fixed[_firstCell])
                    return;

                _visited[_entry] = true;
                var rootBound = _allowSwaps ? Bound(allowMoves: true, _moveBound)[_rootState] : 0;
                if (!_allowSwaps)
                {
                    Arrange(0, rootBound);
                    return;
                }

                var costLimit = Best;
                for (var limit = Math.Max(knownMinimum, rootBound) + 1; limit <= costLimit; limit++)
                {
                    Best = limit;
                    _arrangementCost.Clear();
                    Arrange(0, rootBound);
                    if (BestSolution != null)
                        return;
                }

                Best = costLimit;
            }

            /// <summary>
            /// Visits one tile arrangement reached with <paramref name="swapCost"/> trace of swaps,
            /// routes it with rotations alone, then tries every further swap the bound allows.
            /// </summary>
            private void Arrange(int swapCost, int moveBound)
            {
                if (++_nodes > _nodeLimit)
                    throw new SearchLimitExceededException(_nodeLimit);
                if (swapCost + moveBound >= Best)
                    return;
                if (_arrangementCost.TryGetValue(_hash, out var seen) && seen <= swapCost)
                    return;
                _arrangementCost[_hash] = swapCost;

                Bound(allowMoves: false, _routeBound);
                _routeLimit = Best - swapCost;
                if (_routeBound[_rootState] < _routeLimit)
                    Visit(_firstCell, _firstSide, 0, swapCost);

                if (!_allowSwaps)
                    return;

                var children = new List<(int Estimate, int First, int Second)>();
                foreach (var (first, second) in _swapEdges)
                {
                    SwapTiles(first, second);
                    if (!_arrangementCost.TryGetValue(_hash, out var childSeen) || childSeen > swapCost + 2)
                    {
                        var estimate = swapCost + 2 + Bound(allowMoves: true, _moveBound)[_rootState];
                        if (estimate < Best)
                            children.Add((estimate, first, second));
                    }

                    SwapTiles(first, second);
                }

                children.Sort((left, right) => left.Estimate.CompareTo(right.Estimate));
                foreach (var (estimate, first, second) in children)
                {
                    if (estimate >= Best)
                        continue;

                    SwapTiles(first, second);
                    _swaps.Add(new SlicingSwap { FirstIndex = first, SecondIndex = second });
                    Arrange(swapCost + 2, estimate - swapCost - 2);
                    _swaps.RemoveAt(_swaps.Count - 1);
                    SwapTiles(first, second);
                }
            }

            /// <summary>Rotation-only depth-first search over simple paths on the current arrangement.</summary>
            private void Visit(int cell, int inSide, int turnCost, int swapCost)
            {
                if (++_nodes > _nodeLimit)
                    throw new SearchLimitExceededException(_nodeLimit);
                if (turnCost + _routeBound[cell * 4 + inSide] >= _routeLimit)
                    return;

                _visited[cell] = true;
                _path.Add(cell);
                for (var outSide = 0; outSide < 4; outSide++)
                {
                    if (outSide == inSide)
                        continue;

                    var next = _neighbor[cell, outSide];
                    if (next < 0 || next == _entry || _visited[next])
                        continue;
                    if (next == _core && _coreSide != Opposite(outSide))
                        continue;

                    var turns = _tileTurns[_tileAt[cell], _pairIndex[inSide * 4 + outSide]];
                    if (turns >= Unreachable || turnCost + turns >= _routeLimit)
                        continue;

                    _rotations.Add(turns);
                    if (next == _core)
                        RecordSolution(turnCost + turns, swapCost);
                    else
                        Visit(next, Opposite(outSide), turnCost + turns, swapCost);
                    _rotations.RemoveAt(_rotations.Count - 1);
                }

                _path.RemoveAt(_path.Count - 1);
                _visited[cell] = false;
            }

            private void RecordSolution(int turnCost, int swapCost)
            {
                var cost = turnCost + swapCost;
                if (cost >= Best)
                    return;

                Best = cost;
                _routeLimit = Best - swapCost;
                BestSolution = new Solution
                {
                    Cost = cost,
                    Path = new List<int> { _entry }.Concat(_path).Append(_core).ToList(),
                    Rotations = new List<int> { 0 }.Concat(_rotations).Append(0).ToList(),
                    Swaps = new List<SlicingSwap>(_swaps)
                };
            }

            public int CountRoutes(int maxCost, int cap)
            {
                if (_firstCell < 0 || _firstCell == _core || _fixed[_firstCell])
                    return 0;

                // Fills _cellCost with each cell's cheapest relaxed cost per side pair.
                Bound(allowMoves: _allowSwaps, _moveBound);
                _visited[_entry] = true;
                _routeLimit = maxCost + 1;
                _routeCap = cap;
                _routeCount = 0;
                EnumerateRoutes(_firstCell, _firstSide, 0);
                return _routeCount;
            }

            /// <summary>Simple paths whose relaxed cost fits, confirmed by the assignment bound.</summary>
            private void EnumerateRoutes(int cell, int inSide, int relaxedCost)
            {
                if (++_nodes > _nodeLimit)
                    throw new SearchLimitExceededException(_nodeLimit);
                if (_routeCount > _routeCap || relaxedCost + _moveBound[cell * 4 + inSide] >= _routeLimit)
                    return;

                _visited[cell] = true;
                _path.Add(cell);
                for (var outSide = 0; outSide < 4; outSide++)
                {
                    if (outSide == inSide)
                        continue;

                    var next = _neighbor[cell, outSide];
                    if (next < 0 || next == _entry || _visited[next])
                        continue;
                    if (next == _core && _coreSide != Opposite(outSide))
                        continue;

                    var pair = _pairIndex[inSide * 4 + outSide];
                    var cost = _cellCost[cell * PairCount + pair];
                    if (cost >= Unreachable || relaxedCost + cost >= _routeLimit)
                        continue;

                    _pathPairs.Add(pair);
                    if (next != _core)
                        EnumerateRoutes(next, Opposite(outSide), relaxedCost + cost);
                    else if (IsPathAffordable())
                        _routeCount++;
                    _pathPairs.RemoveAt(_pathPairs.Count - 1);
                }

                _path.RemoveAt(_path.Count - 1);
                _visited[cell] = false;
            }

            /// <summary>
            /// Whether some swap sequence lets rotations alone power the current path within the
            /// route limit. Depth-first over swaps, pruned by the path's assignment bound and by
            /// arrangements already reached as cheaply.
            /// </summary>
            private bool IsPathAffordable()
            {
                if (AssignmentBound() >= _routeLimit)
                    return false;
                if (!_allowSwaps)
                    return true;

                _pathArrangementCost.Clear();
                return CanPowerPath(0);
            }

            private bool CanPowerPath(int swapCost)
            {
                if (++_nodes > _nodeLimit)
                    throw new SearchLimitExceededException(_nodeLimit);

                var turns = 0;
                for (var row = 0; row < _path.Count && turns < Unreachable; row++)
                    turns += _tileTurns[_tileAt[_path[row]], _pathPairs[row]];
                if (swapCost + turns < _routeLimit)
                    return true;
                if (swapCost + 2 >= _routeLimit || swapCost + AssignmentBound() >= _routeLimit)
                    return false;
                if (_pathArrangementCost.TryGetValue(_hash, out var seen) && seen <= swapCost)
                    return false;
                _pathArrangementCost[_hash] = swapCost;

                foreach (var (first, second) in _swapEdges)
                {
                    SwapTiles(first, second);
                    var affordable = CanPowerPath(swapCost + 2);
                    SwapTiles(first, second);
                    if (affordable)
                        return true;
                }

                return false;
            }

            /// <summary>
            /// Cheapest assignment of distinct movable tiles to the current path cells, each costing
            /// its Manhattan distance plus turns (Hungarian algorithm). Swaps cost 2 and move two
            /// tiles one step, so this never exceeds the true cost of powering the path. Without
            /// swaps every cell keeps its own tile and the sum is exact.
            /// </summary>
            private int AssignmentBound()
            {
                var rows = _path.Count;
                if (!_allowSwaps)
                {
                    var total = 0;
                    for (var row = 0; row < rows; row++)
                        total += _tileTurns[_tileAt[_path[row]], _pathPairs[row]];
                    return total;
                }

                const int infinity = int.MaxValue / 4;
                var columns = _movableCells.Length;
                var rowPotential = new int[rows + 1];
                var columnPotential = new int[columns + 1];
                var assignedRow = new int[columns + 1];
                var previousColumn = new int[columns + 1];
                for (var row = 1; row <= rows; row++)
                {
                    assignedRow[0] = row;
                    var column = 0;
                    var slack = new int[columns + 1];
                    var used = new bool[columns + 1];
                    Array.Fill(slack, infinity);
                    do
                    {
                        used[column] = true;
                        var currentRow = assignedRow[column];
                        var target = _path[currentRow - 1];
                        var pair = _pathPairs[currentRow - 1];
                        var delta = infinity;
                        var nextColumn = 0;
                        for (var candidate = 1; candidate <= columns; candidate++)
                        {
                            if (used[candidate])
                                continue;

                            var source = _movableCells[candidate - 1];
                            var cost = _distance[source, target] + _tileTurns[_tileAt[source], pair];
                            var reduced = cost - rowPotential[currentRow] - columnPotential[candidate];
                            if (reduced < slack[candidate])
                            {
                                slack[candidate] = reduced;
                                previousColumn[candidate] = column;
                            }

                            if (slack[candidate] < delta)
                            {
                                delta = slack[candidate];
                                nextColumn = candidate;
                            }
                        }

                        for (var candidate = 0; candidate <= columns; candidate++)
                        {
                            if (used[candidate])
                            {
                                rowPotential[assignedRow[candidate]] += delta;
                                columnPotential[candidate] -= delta;
                            }
                            else
                            {
                                slack[candidate] -= delta;
                            }
                        }

                        column = nextColumn;
                    }
                    while (assignedRow[column] != 0);

                    do
                    {
                        var prior = previousColumn[column];
                        assignedRow[column] = assignedRow[prior];
                        column = prior;
                    }
                    while (column != 0);
                }

                return -columnPotential[0];
            }

            private void SwapTiles(int first, int second)
            {
                _hash ^= _zobrist[first, _tileClass[_tileAt[first]]] ^ _zobrist[second, _tileClass[_tileAt[second]]];
                (_tileAt[first], _tileAt[second]) = (_tileAt[second], _tileAt[first]);
                _hash ^= _zobrist[first, _tileClass[_tileAt[first]]] ^ _zobrist[second, _tileClass[_tileAt[second]]];
            }

            /// <summary>
            /// Admissible remaining-cost bound for every (cell, entered side) state, written into
            /// <paramref name="output"/>. With moves, a cell may use any movable tile at Manhattan
            /// distance d for d + turns: every swap costs 2 trace and moves two tiles one step, so
            /// swap trace covers the summed displacement. Without moves, each cell uses its own
            /// tile. Walks may revisit cells, which only lowers the bound.
            /// </summary>
            private int[] Bound(bool allowMoves, int[] output)
            {
                for (var cell = 0; cell < _cellCount; cell++)
                {
                    for (var pair = 0; pair < PairCount; pair++)
                        _cellCost[cell * PairCount + pair] = Unreachable;
                    if (_fixed[cell])
                        continue;

                    if (!allowMoves)
                    {
                        var own = _tileAt[cell];
                        for (var pair = 0; pair < PairCount; pair++)
                            _cellCost[cell * PairCount + pair] = _tileTurns[own, pair];
                        continue;
                    }

                    for (var source = 0; source < _cellCount; source++)
                    {
                        if (_fixed[source])
                            continue;

                        var distance = _distance[source, cell];
                        var tile = _tileAt[source];
                        for (var pair = 0; pair < PairCount; pair++)
                        {
                            var candidate = distance + _tileTurns[tile, pair];
                            if (candidate < _cellCost[cell * PairCount + pair])
                                _cellCost[cell * PairCount + pair] = candidate;
                        }
                    }
                }

                Array.Fill(output, Unreachable);
                Array.Clear(_done);
                foreach (var bucket in _buckets)
                    bucket.Clear();

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
                            var cost = _cellCost[cell * PairCount + _pairIndex[inSide * 4 + outSide]];
                            if (cost < output[state] && cost <= MaxBucket)
                            {
                                output[state] = cost;
                                _buckets[cost].Add(state);
                            }
                        }
                    }
                }

                // Reverse Dijkstra with integer buckets: state (cell, inSide) reaches
                // (next, opposite(outSide)) for the cell's cost. Bounds past MaxBucket stay
                // Unreachable, which only ever exceeds a real search limit.
                for (var distance = 0; distance <= MaxBucket; distance++)
                {
                    var bucket = _buckets[distance];
                    for (var item = 0; item < bucket.Count; item++)
                    {
                        var current = bucket[item];
                        if (_done[current] || output[current] != distance)
                            continue;

                        _done[current] = true;
                        var previous = _neighbor[current / 4, current % 4];
                        if (previous < 0 || _fixed[previous])
                            continue;

                        var previousOut = Opposite(current % 4);
                        for (var previousIn = 0; previousIn < 4; previousIn++)
                        {
                            if (previousIn == previousOut)
                                continue;

                            var state = previous * 4 + previousIn;
                            var candidate = distance + _cellCost[previous * PairCount + _pairIndex[previousIn * 4 + previousOut]];
                            if (!_done[state] && candidate < output[state] && candidate <= MaxBucket)
                            {
                                output[state] = candidate;
                                _buckets[candidate].Add(state);
                            }
                        }
                    }
                }

                return output;
            }

            private static int SingleSide(SlicingTile tile)
            {
                return SideOf(Slicing.GetConnections(tile));
            }
        }
    }
}
