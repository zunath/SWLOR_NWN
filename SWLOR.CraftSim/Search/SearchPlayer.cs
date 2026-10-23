using SWLOR.CraftSim.Play;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;

namespace SWLOR.CraftSim.Search
{
    /// <summary>Which simulated scavenger plays the board.</summary>
    public enum SearchPlayerKind
    {
        /// <summary>Searches the spots in reading order and never sifts or digs: a player who ignores everything but the Search button.</summary>
        Careless = 1,

        /// <summary>
        /// Plays the board from what the window shows: it knows the layout only through Sifts and Searches, values every search by the
        /// chances it would have, and Sifts when the ground it would show is worth more than the search it costs.
        /// </summary>
        Skilled = 2,

        /// <summary>A scavenger who is told where the promising ground is for free: no real player can reach it. It bounds what skill can find.</summary>
        Oracle = 3
    }

    /// <summary>
    /// A simulated scavenger for the Search board. Careless plays by the order of the spots. Skilled and Oracle take, at each step, the
    /// action with the highest expected finds: the action's own find plus the best split of the searches left on the board each of its
    /// outcomes leaves (<see cref="Position.ValueOfSearch"/>, <see cref="Position.ValueOfDig"/>, <see cref="Position.ValueAfterSift"/>).
    /// That split treats every spot as a chain of actions (its Search, then its Digs) and is a plan made before the outcomes are
    /// known, so the player is one step of look-ahead, not an optimal player: it can only find less than an optimal one does. Skilled
    /// does the sum on what it knows and the odds of the rest; Oracle knows every spot. None of them reads a preview of an unknown
    /// spot, which would show the hidden layout.
    /// </summary>
    public sealed class SearchPlayer : ICraftPolicy
    {
        /// <summary>Two actions whose values differ by less than this are tied, and the tie goes to a Search, then a Dig Deeper, then a Sift.</summary>
        public const double SiftMargin = 1e-9;

        private readonly SearchPlayerKind _kind;

        public SearchPlayer(SearchPlayerKind kind)
        {
            _kind = kind;
        }

        public SearchPlayerKind Kind => _kind;

        public string Name => _kind switch
        {
            SearchPlayerKind.Careless => "Careless",
            SearchPlayerKind.Skilled => "Skilled",
            _ => "Oracle"
        };

        public string Choose(ICraftMinigameEngine engine)
        {
            if (engine is not SearchEngine board)
                throw new ArgumentException("The scavenger plays the Search board.", nameof(engine));

            if (board.SearchesLeft <= 0)
                return null;

            return _kind == SearchPlayerKind.Careless ? Careless(board) : Informed(board);
        }

        private static string Careless(SearchEngine board)
        {
            for (var spot = 0; spot < SearchBoard.Spots; spot++)
            {
                if (!board.IsSearched(spot) && !board.IsSpoiled(spot))
                    return SearchActionIds.For(SearchActionIds.Search, spot);
            }

            return FirstDig(board);
        }

        private static string FirstDig(SearchEngine board)
        {
            for (var spot = 0; spot < SearchBoard.Spots; spot++)
            {
                if (board.IsSearched(spot) && !board.IsSpoiled(spot) && board.DigsAt(spot) < SearchBoard.MaxDigsPerSpot)
                    return SearchActionIds.For(SearchActionIds.DigDeeper, spot);
            }

            return null;
        }

        private string Informed(SearchEngine board)
        {
            var position = Position.From(board, _kind == SearchPlayerKind.Oracle);
            var remaining = board.SearchesLeft;

            // One step of look-ahead over the outcomes of each action: the action's own expected find, plus the best split of the
            // searches left on the board each outcome leaves (a spoiled spot is gone, a surviving dig is one dig deeper, a searched
            // spot's ground is known). The action with the highest total is taken; on a tie a Search (which shows the ground) comes
            // before a Dig Deeper, and a Dig Deeper before a Sift.
            string best = null;
            var bestValue = double.NegativeInfinity;
            var bestPriority = int.MaxValue;
            var bestImmediate = double.NegativeInfinity;

            // Actions of equal total (an optimal plan can start with any of them) go to a Search before a Dig Deeper before a Sift, and
            // within a kind to the one that finds most itself, so the sure thing is taken first and the ground it shows is learned sooner.
            void Consider(string action, double value, int priority, double immediate)
            {
                var better = value > bestValue + SiftMargin ||
                             (value > bestValue - SiftMargin &&
                              (priority < bestPriority || (priority == bestPriority && immediate > bestImmediate + SiftMargin)));
                if (!better)
                    return;

                best = action;
                bestValue = Math.Max(value, bestValue);
                bestPriority = priority;
                bestImmediate = immediate;
            }

            for (var spot = 0; spot < SearchBoard.Spots; spot++)
            {
                if (!board.IsSearched(spot) && !board.IsSpoiled(spot))
                    Consider(SearchActionIds.For(SearchActionIds.Search, spot), position.ValueOfSearch(spot, remaining - 1), 0, position.ChanceOfSearch(spot));
            }

            for (var spot = 0; spot < SearchBoard.Spots; spot++)
            {
                if (board.IsSearched(spot) && !board.IsSpoiled(spot) && board.DigsAt(spot) < SearchBoard.MaxDigsPerSpot)
                    Consider(SearchActionIds.For(SearchActionIds.DigDeeper, spot), position.ValueOfDig(spot, remaining - 1), 1, position.ChanceOfDig(spot));
            }

            if (_kind == SearchPlayerKind.Skilled && remaining > 1)
            {
                for (var spot = 0; spot < SearchBoard.Spots; spot++)
                {
                    var reach = board.UnknownAround(spot);
                    if (reach != 0)
                        Consider(SearchActionIds.For(SearchActionIds.Sift, spot), position.ValueAfterSift(SearchBoard.PopCount(reach), remaining - 1), 2, 0);
                }
            }

            return best;
        }

        /// <summary>
        /// The board as the scavenger reasons about it: for every spot the actions still open there, and how likely the ground that is not
        /// known is to be promising.
        /// </summary>
        internal sealed class Position
        {
            private const int Spots = SearchBoard.Spots;

            private readonly SearchSetup _setup;
            private readonly bool[] _searched = new bool[Spots];
            private readonly bool[] _spoiled = new bool[Spots];
            private readonly int[] _digs = new int[Spots];
            private readonly SearchSpotKnowledge[] _knowledge = new SearchSpotKnowledge[Spots];

            private Position(SearchSetup setup)
            {
                _setup = setup;
            }

            /// <summary>How many of the spots with unknown ground are promising.</summary>
            public int UnknownPromising { get; private set; }

            /// <summary>How many spots have unknown ground.</summary>
            public int UnknownSpots { get; private set; }

            public static Position From(SearchEngine board, bool knowsEverything)
            {
                var position = new Position(board.Setup);
                var knownPromising = 0;
                for (var spot = 0; spot < Spots; spot++)
                {
                    position._searched[spot] = board.IsSearched(spot);
                    position._spoiled[spot] = board.IsSpoiled(spot);
                    position._digs[spot] = board.DigsAt(spot);
                    position._knowledge[spot] = knowsEverything
                        ? board.Board.IsPromising(spot) ? SearchSpotKnowledge.Promising : SearchSpotKnowledge.Ordinary
                        : board.Knowledge(spot);
                    if (position._knowledge[spot] == SearchSpotKnowledge.Unknown)
                        position.UnknownSpots++;
                    else if (position._knowledge[spot] == SearchSpotKnowledge.Promising)
                        knownPromising++;
                }

                position.UnknownPromising = Math.Max(0, board.Setup.PromisingSpots - knownPromising);
                return position;
            }

            /// <summary>The chance, from 0 to 1, that a search of the spot finds something, from what is known about it.</summary>
            private double SearchChance(int spot)
            {
                var promising = _setup.PromisingChance / 100.0;
                var ordinary = _setup.OrdinaryChance / 100.0;
                switch (_knowledge[spot])
                {
                    case SearchSpotKnowledge.Promising:
                        return promising;
                    case SearchSpotKnowledge.Ordinary:
                        return ordinary;
                    default:
                        var share = UnknownSpots == 0 ? 0 : Math.Min(1.0, UnknownPromising / (double)UnknownSpots);
                        return ordinary + (promising - ordinary) * share;
                }
            }

            /// <summary>The expected finds of each next action at a spot, in order: its Search if it has none yet, then its Digs.</summary>
            private List<double> Chain(int spot, double[] searchChances)
            {
                var chain = new List<double>(4);
                if (_spoiled[spot])
                    return chain;

                if (!_searched[spot])
                    chain.Add(searchChances[spot]);

                var surviving = 1.0;
                var find = _setup.DigFindChance / 100.0;
                for (var dig = _digs[spot]; dig < SearchBoard.MaxDigsPerSpot; dig++)
                {
                    var risk = _setup.DigRisk(dig) / 100.0;
                    chain.Add(surviving * (1 - risk) * find);
                    surviving *= 1 - risk;
                }

                return chain;
            }

            private List<double>[] Chains(double[] searchChances)
            {
                var chains = new List<double>[Spots];
                for (var spot = 0; spot < Spots; spot++)
                    chains[spot] = Chain(spot, searchChances);

                return chains;
            }

            private double[] SearchChances()
            {
                var chances = new double[Spots];
                for (var spot = 0; spot < Spots; spot++)
                    chances[spot] = SearchChance(spot);

                return chances;
            }

            /// <summary>
            /// The expected finds of spending the searches best: every spot is a chain of actions, and the best split of the searches among
            /// the chains is a small knapsack.
            /// </summary>
            public double Value(int searches) => Knapsack(Chains(SearchChances()), searches);

            private static double Knapsack(IReadOnlyList<List<double>> chains, int searches)
            {
                var best = new double[searches + 1];
                foreach (var chain in chains)
                {
                    var next = new double[searches + 1];
                    for (var budget = 0; budget <= searches; budget++)
                    {
                        var value = best[budget];
                        var prefix = 0.0;
                        for (var taken = 1; taken <= chain.Count && taken <= budget; taken++)
                        {
                            prefix += chain[taken - 1];
                            value = Math.Max(value, best[budget - taken] + prefix);
                        }

                        next[budget] = value;
                    }

                    best = next;
                }

                return best[searches];
            }

            /// <summary>
            /// The value of the searches left after a Sift that shows <paramref name="revealed"/> spots of unknown ground, averaged over how
            /// many of them are promising (the odds follow from the unknown ground there is).
            /// </summary>
            public double ValueAfterSift(int revealed, int searchesLeft)
            {
                var revealedSpots = Math.Min(revealed, UnknownSpots);
                if (revealedSpots == 0)
                    return Value(searchesLeft);

                double total = 0;
                for (var promising = 0; promising <= revealedSpots; promising++)
                {
                    var odds = Hypergeometric(UnknownSpots, UnknownPromising, revealedSpots, promising);
                    if (odds <= 0)
                        continue;

                    total += odds * ValueWithRevealed(revealedSpots, promising, searchesLeft);
                }

                return total;
            }

            private double ValueWithRevealed(int revealed, int promising, int searches)
            {
                // The revealed spots are interchangeable, so give the first ones the promising ground.
                var knowledge = (SearchSpotKnowledge[])_knowledge.Clone();
                var given = 0;
                for (var spot = 0; spot < Spots && given < revealed; spot++)
                {
                    if (knowledge[spot] != SearchSpotKnowledge.Unknown)
                        continue;

                    knowledge[spot] = given < promising ? SearchSpotKnowledge.Promising : SearchSpotKnowledge.Ordinary;
                    given++;
                }

                var rebuilt = new Position(_setup);
                Array.Copy(_searched, rebuilt._searched, Spots);
                Array.Copy(_spoiled, rebuilt._spoiled, Spots);
                Array.Copy(_digs, rebuilt._digs, Spots);
                Array.Copy(knowledge, rebuilt._knowledge, Spots);
                rebuilt.UnknownSpots = UnknownSpots - revealed;
                rebuilt.UnknownPromising = UnknownPromising - promising;
                return rebuilt.Value(searches);
            }

            /// <summary>The chance, from 0 to 1, that a Search of the spot finds something, from what is known about it.</summary>
            public double ChanceOfSearch(int spot) => SearchChance(spot);

            /// <summary>The chance, from 0 to 1, that a Dig Deeper at the spot finds something now (the engine's rounding).</summary>
            public double ChanceOfDig(int spot) => ((100 - _setup.DigRisk(_digs[spot])) * _setup.DigFindChance + 50) / 100 / 100.0;

            private Position Copy()
            {
                var copy = new Position(_setup);
                Array.Copy(_searched, copy._searched, Spots);
                Array.Copy(_spoiled, copy._spoiled, Spots);
                Array.Copy(_digs, copy._digs, Spots);
                Array.Copy(_knowledge, copy._knowledge, Spots);
                copy.UnknownSpots = UnknownSpots;
                copy.UnknownPromising = UnknownPromising;
                return copy;
            }

            /// <summary>
            /// The expected finds of a Search of the spot now and then the best split of <paramref name="searchesLeftAfter"/> searches on
            /// the board it leaves. A Search shows the spot's ground, so ground not yet known is promising with the odds the unknown ground
            /// gives and each case is valued on its own board.
            /// </summary>
            public double ValueOfSearch(int spot, int searchesLeftAfter)
            {
                var promising = _setup.PromisingChance / 100.0;
                var ordinary = _setup.OrdinaryChance / 100.0;
                if (_knowledge[spot] != SearchSpotKnowledge.Unknown)
                {
                    var after = Copy();
                    after._searched[spot] = true;
                    return (_knowledge[spot] == SearchSpotKnowledge.Promising ? promising : ordinary) + after.Value(searchesLeftAfter);
                }

                var share = UnknownSpots == 0 ? 0 : Math.Min(1.0, UnknownPromising / (double)UnknownSpots);
                double total = 0;
                foreach (var isPromising in new[] { true, false })
                {
                    var odds = isPromising ? share : 1 - share;
                    if (odds <= 0)
                        continue;

                    var after = Copy();
                    after._searched[spot] = true;
                    after._knowledge[spot] = isPromising ? SearchSpotKnowledge.Promising : SearchSpotKnowledge.Ordinary;
                    after.UnknownSpots--;
                    if (isPromising)
                        after.UnknownPromising--;

                    total += odds * ((isPromising ? promising : ordinary) + after.Value(searchesLeftAfter));
                }

                return total;
            }

            /// <summary>
            /// The expected finds of a Dig Deeper at the spot now and then the best split of <paramref name="searchesLeftAfter"/> searches
            /// on the board each outcome leaves: a dig that ruins the spot takes its chain off the board and gives the search back to the
            /// others, a dig that does not leaves the spot one dig deeper whether or not it found.
            /// </summary>
            public double ValueOfDig(int spot, int searchesLeftAfter)
            {
                var riskPercent = _setup.DigRisk(_digs[spot]);
                var findPercent = ((100 - riskPercent) * _setup.DigFindChance + 50) / 100;
                var spoiled = Copy();
                spoiled._spoiled[spot] = true;
                var deeper = Copy();
                deeper._digs[spot]++;
                return findPercent / 100.0 +
                       riskPercent / 100.0 * spoiled.Value(searchesLeftAfter) +
                       (100 - riskPercent) / 100.0 * deeper.Value(searchesLeftAfter);
            }

            private static double Hypergeometric(int population, int successes, int draws, int observed)
            {
                if (observed < 0 || observed > successes || draws - observed > population - successes)
                    return 0;

                return Choose(successes, observed) * Choose(population - successes, draws - observed) / Choose(population, draws);
            }

            private static double Choose(int n, int k)
            {
                if (k < 0 || k > n)
                    return 0;

                double result = 1;
                for (var index = 1; index <= k; index++)
                    result = result * (n - k + index) / index;

                return result;
            }
        }
    }
}
