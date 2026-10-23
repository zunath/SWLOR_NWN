using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.ScavengeService;

namespace SWLOR.CraftSim.Search
{
    /// <summary>
    /// The scavenging cohort of the XP-rate gate, as the plan pins it: one case for each site level 1 to 5, the scavenger's Gathering rank
    /// 10 times the level, XP per attempt (a quarter on failure), no tool. Both loops spend the same time to reach and open the site
    /// (<see cref="SearchParityOptions.SiteSeconds"/>); today's loop then takes one two-second animation for the visit whatever its attempts, and the board takes
    /// <see cref="SearchParityOptions.SecondsPerAction"/> an action. Today's loop is played exactly (the legacy model's expected XP and
    /// attempts at a Perception modifier of 0), and the board is played by the scavengers in <see cref="SearchParityOptions.XpPlayers"/>,
    /// the gate judging the one that earns least per hour, so both the careless and the skilled player reach today's rate.
    /// </summary>
    public sealed class SearchXpRateCohort : IXpRateCohort
    {
        /// <summary>How many sites each case stands for, so the expectations can be reported as whole runs.</summary>
        public const int SitesPerCase = 10_000;

        private readonly SearchParity _parity;
        private readonly SearchParityOptions _options;
        private readonly Func<int, double> _xpMultiplierFor;

        /// <param name="parity">The played boards.</param>
        /// <param name="options">The time of reaching a site and of an action.</param>
        /// <param name="xpMultiplierFor">
        /// The multiplier on an attempt's XP by the site's level. By default it is the one the game server applies (<c>CraftMinigameXp</c>);
        /// a study passes another to see what a different multiplier would do.
        /// </param>
        public SearchXpRateCohort(SearchParity parity, SearchParityOptions options = null, Func<int, double> xpMultiplierFor = null)
        {
            _parity = parity ?? throw new ArgumentNullException(nameof(parity));
            _options = options ?? SearchParityOptions.Default;
            _xpMultiplierFor = xpMultiplierFor ?? (level => CraftMinigameXp.MultiplierFor(CraftMinigameType.Search, level));
        }

        public string Name => "Scavenging";

        public string RunName => "site";

        /// <summary>Today's search takes the walk and then one two-second animation for the visit, however many attempts it makes (one action per site).</summary>
        public XpLoopTiming LegacyTiming => new(_options.LegacySeconds, _options.SiteSeconds);

        public XpLoopTiming ReplacementTiming => new(_options.SecondsPerAction, _options.SiteSeconds);

        /// <summary>The Gathering rank of a site level's case: 10 times the level.</summary>
        public static int RankOf(int siteLevel) => 10 * siteLevel;

        /// <summary>The cases the study plays, so a report can play them all in parallel first.</summary>
        public IEnumerable<SearchKey> Keys() =>
            _options.SiteLevels.SelectMany(level => _options.XpPlayers.Select(player => new SearchKey(RankOf(level), level, player, SearchEffects.None)));

        public IReadOnlyList<XpRateCase> Cases(GateContext context) =>
            _options.SiteLevels
                .Select(level => new XpRateCase($"Site level {level}", $"rank {RankOf(level)}, Hard Look level {CraftRankMilestones.HardLookLevelForRank(RankOf(level))}"))
                .ToList();

        public XpLoopSample PlayLegacy(XpRateCase xpCase, GateContext context)
        {
            var (level, rank) = Parse(xpCase);
            var tier = CraftRankMilestones.HardLookLevelForRank(rank);
            ServerXp.ForDelta(0);
            var xp = ScavengeModel.ExpectedXpPerSite(level, rank, tier, ScavengeQuickSearch.ReferencePerceptionModifier);
            // The old code played one animation for the visit, whatever its attempts, so a site is one action.
            return new XpLoopSample(xp * SitesPerCase, SitesPerCase, SitesPerCase);
        }

        public XpLoopSample PlayReplacement(XpRateCase xpCase, GateContext context)
        {
            var (level, rank) = Parse(xpCase);
            var worst = default(XpLoopSample);
            var worstRate = double.MaxValue;
            foreach (var player in _options.XpPlayers)
            {
                var sample = Play(level, rank, player);
                var rate = sample.XpPerHour(ReplacementTiming);
                if (rate < worstRate)
                {
                    worstRate = rate;
                    worst = sample;
                }
            }

            return worst;
        }

        /// <summary>The XP and actions of a board played by one player, as sites.</summary>
        public XpLoopSample Play(int siteLevel, int rank, SearchPlayerKind player)
        {
            var sample = _parity.Sample(rank, siteLevel, player);
            var xp = BoardXp(sample, siteLevel, rank);
            return new XpLoopSample(xp * SitesPerCase, sample.ActionsPerBoard * SitesPerCase, SitesPerCase);
        }

        /// <summary>The Gathering XP of an average board: each success pays what the site pays, each failure a quarter, times the multiplier.</summary>
        public double BoardXp(SearchSample sample, int siteLevel, int rank)
        {
            var delta = ServerXp.ForDelta(10 * siteLevel - rank);
            var multiplier = _xpMultiplierFor(siteLevel);
            return sample.SuccessesPerBoard * CraftMinigameXp.ScaleBaseXp(delta, multiplier) +
                   sample.FailuresPerBoard * CraftMinigameXp.ScaleBaseXp(delta / 4, multiplier);
        }

        private (int Level, int Rank) Parse(XpRateCase xpCase)
        {
            var level = int.Parse(xpCase.Group.Replace("Site level ", string.Empty));
            return (level, RankOf(level));
        }
    }
}
