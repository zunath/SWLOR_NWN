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
    /// A simulated scavenger for the Search board. Careless plays by the order of the spots. Skilled and Oracle spend the searches
    /// where they are expected to find most: every spot is a chain of actions (its Search, then its Digs), the best way to spend the
    /// searches left is the best split among the chains (<see cref="Position.Value"/>), and a Sift is worth taking when the ground it shows
    /// raises that value by more than the search it costs. Skilled does the sum on what it knows and the odds of the rest; Oracle
    /// knows every spot. None of them reads a preview of an unknown spot, which would show the hidden layout.
    /// </summary>
    public sealed class SearchPlayer : ICraftPolicy
    {
        /// <summary>A Sift must raise the value of the searches left by more than this, so the player does not Sift for a rounding error.</summary>
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
            var now = position.Value(remaining);

            if (_kind == SearchPlayerKind.Skilled && remaining > 1)
            {
                var bestSift = -1;
                var bestValue = now + SiftMargin;
                for (var spot = 0; spot < SearchBoard.Spots; spot++)
                {
                    var reach = board.UnknownAround(spot);
                    if (reach == 0)
                        continue;

                    var value = position.ValueAfterSift(SearchBoard.PopCount(reach), remaining - 1);
                    if (value > bestValue)
                    {
                        bestValue = value;
                        bestSift = spot;
                    }
                }

                if (bestSift >= 0)
                    return SearchActionIds.For(SearchActionIds.Sift, bestSift);
            }

            return position.BestFirstAction(remaining);
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

            /// <summary>
            /// The first action of the best split: the first step of the chain that keeps the value of the searches highest once it is
            /// taken (the step's own find plus the best split of the searches left), and of those the one that finds most itself. A chain is
            /// not required to be one the split cannot do without: with more equally good chains than searches, none is, and the player
            /// must still take one of the best.
            /// </summary>
            public string BestFirstAction(int searches)
            {
                var chances = SearchChances();
                var chains = Chains(chances);
                var bestSpot = -1;
                var bestValue = double.NegativeInfinity;
                var bestFirst = double.NegativeInfinity;
                for (var spot = 0; spot < Spots; spot++)
                {
                    if (chains[spot].Count == 0)
                        continue;

                    var saved = chains[spot];
                    chains[spot] = saved.Skip(1).ToList();
                    var value = saved[0] + Knapsack(chains, searches - 1);
                    chains[spot] = saved;
                    if (value > bestValue + 1e-12 || (value > bestValue - 1e-12 && saved[0] > bestFirst + 1e-12))
                    {
                        bestValue = value;
                        bestFirst = saved[0];
                        bestSpot = spot;
                    }
                }

                if (bestSpot < 0)
                {
                    // Nothing is worth a search: spend it on the first spot that can take one.
                    for (var spot = 0; spot < Spots; spot++)
                    {
                        if (chains[spot].Count > 0)
                            return Action(spot);
                    }

                    return null;
                }

                return Action(bestSpot);
            }

            private string Action(int spot) =>
                SearchActionIds.For(_searched[spot] ? SearchActionIds.DigDeeper : SearchActionIds.Search, spot);

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
