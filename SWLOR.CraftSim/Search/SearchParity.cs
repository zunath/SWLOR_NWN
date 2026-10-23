using System.Collections.Concurrent;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;

namespace SWLOR.CraftSim.Search
{
    /// <summary>What many boards played by one scavenger came to.</summary>
    /// <param name="Runs">Boards played.</param>
    /// <param name="OrdinaryFinds">Searches that found something, over all the boards.</param>
    /// <param name="DeeperFinds">Dig Deeper attempts that found something.</param>
    /// <param name="Attempts">Searches and Dig Deeper attempts made, which pay Gathering XP.</param>
    /// <param name="SuccessfulAttempts">Attempts that found something.</param>
    /// <param name="Sifts">Sifts made.</param>
    /// <param name="Digs">Dig Deeper attempts made.</param>
    /// <param name="Spoiled">Spots a dig ruined.</param>
    /// <param name="Actions">Actions taken, which is every search spent.</param>
    public readonly record struct SearchSample(
        int Runs, int OrdinaryFinds, int DeeperFinds, int Attempts, int SuccessfulAttempts, int Sifts, int Digs, int Spoiled, int Actions)
    {
        public double Finds => Runs == 0 ? 0 : (OrdinaryFinds + DeeperFinds) / (double)Runs;
        public double Ordinary => Runs == 0 ? 0 : OrdinaryFinds / (double)Runs;
        public double Deeper => Runs == 0 ? 0 : DeeperFinds / (double)Runs;
        public double AttemptsPerBoard => Runs == 0 ? 0 : Attempts / (double)Runs;
        public double SuccessesPerBoard => Runs == 0 ? 0 : SuccessfulAttempts / (double)Runs;
        public double FailuresPerBoard => Runs == 0 ? 0 : (Actions - SuccessfulAttempts) / (double)Runs;
        public double SiftsPerBoard => Runs == 0 ? 0 : Sifts / (double)Runs;
        public double DigsPerBoard => Runs == 0 ? 0 : Digs / (double)Runs;
        public double SpoiledPerBoard => Runs == 0 ? 0 : Spoiled / (double)Runs;
        public double ActionsPerBoard => Runs == 0 ? 0 : Actions / (double)Runs;
    }

    /// <summary>One case of the Search study: a Gathering rank, a site level, a scavenger's perks and a player.</summary>
    public readonly record struct SearchKey(int Rank, int SiteLevel, SearchPlayerKind Player, SearchEffects Effects);

    /// <summary>
    /// Plays the Search board against the sites of the game and remembers each case, so the tables of one report share their boards. Every
    /// case takes its dice from its own stream of the seed, so a table repeats exactly whatever order the cases ran in.
    /// </summary>
    public sealed class SearchParity
    {
        private readonly ConcurrentDictionary<SearchKey, SearchSample> _samples = new();

        public SearchParity(int runs, int seed)
        {
            if (runs < 1)
                throw new ArgumentOutOfRangeException(nameof(runs), "A case needs at least one board.");

            Runs = runs;
            Seed = seed;
        }

        /// <summary>Boards played for each case.</summary>
        public int Runs { get; }

        public int Seed { get; }

        public SearchSample Sample(int rank, int siteLevel, SearchPlayerKind player, SearchEffects effects = null) =>
            Sample(new SearchKey(rank, siteLevel, player, effects ?? SearchEffects.None));

        public SearchSample Sample(SearchKey key) => _samples.GetOrAdd(key, Play);

        /// <summary>Plays every case in parallel, so the tables that follow only read.</summary>
        public void Prepare(IEnumerable<SearchKey> keys)
        {
            Parallel.ForEach(keys.Distinct(), key => Sample(key));
        }

        /// <summary>One board played to its end.</summary>
        public static SearchEngine PlayBoard(SearchSetup setup, SearchPlayerKind player, IMinigameRandom random)
        {
            var engine = new SearchEngine(random, setup);
            CraftPlayer.Play(engine, new SearchPlayer(player), 40);
            return engine;
        }

        private SearchSample Play(SearchKey key)
        {
            var setup = SearchSetup.ForRank(key.Rank, key.SiteLevel, key.Effects);
            var stream = Stream(key);
            int ordinary = 0, deeper = 0, attempts = 0, successes = 0, sifts = 0, digs = 0, spoiled = 0, actions = 0;
            for (var run = 0; run < Runs; run++)
            {
                var random = SimMinigameRandom.ForRun(Seed, SimStreams.Adaptive, stream, run);
                var engine = PlayBoard(setup, key.Player, random);
                ordinary += engine.OrdinaryFinds;
                deeper += engine.DeeperFinds;
                attempts += engine.Attempts;
                successes += engine.SuccessfulAttempts;
                sifts += engine.Sifts;
                actions += engine.Turn;
                for (var spot = 0; spot < SearchBoard.Spots; spot++)
                {
                    digs += engine.DigsAt(spot);
                    if (engine.IsSpoiled(spot))
                        spoiled++;
                }
            }

            return new SearchSample(Runs, ordinary, deeper, attempts, successes, sifts, digs, spoiled, actions);
        }

        /// <summary>A number that names the case, the same on every machine: FNV-1a over the case's numbers.</summary>
        internal static int Stream(SearchKey key)
        {
            unchecked
            {
                var effects = key.Effects;
                var hash = 2166136261u;
                foreach (var part in new[]
                         {
                             key.Rank, key.SiteLevel, (int)key.Player, effects.ExtraPromisingSpots, effects.DigRiskPercent,
                             effects.DigFindBonus, effects.SearchChanceDelta, effects.DeeperWeightBonus
                         })
                {
                    for (var shift = 0; shift < 32; shift += 8)
                        hash = (hash ^ (uint)((part >> shift) & 0xFF)) * 16777619u;
                }

                return (int)hash;
            }
        }
    }
}
