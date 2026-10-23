using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.ScavengeService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Search
{
    /// <summary>
    /// Gate 5 for scavenging: gathering yield per scavenge site at each Gathering rank stays near today's expected yield, Quick Search
    /// equals it, and a skilled session stays within +15% of it (<see cref="SearchParityOptions.SkilledMaximumRatio"/>). Yield is the
    /// loot-table rolls a site gives, one for every Search or Dig Deeper that finds something. Three things are checked:
    /// <list type="number">
    /// <item>Quick Search's expected rolls equal the legacy model's (<c>ScavengeModel</c>) at the parity reference, a Perception modifier
    /// of 0, exactly, and a played Quick Search agrees with them.</item>
    /// <item>On the board, a careless player finds near that, a skilled player at least as much and at most 15% more, and even a
    /// scavenger told the layout for free stays inside a wider ceiling.</item>
    /// <item>The same cap holds for a skilled player with the whole Treasure Seeker line.</item>
    /// </list>
    /// </summary>
    public sealed class SearchYieldGate : IBalanceGate
    {
        private readonly SearchParity _parity;
        private readonly SearchParityOptions _options;

        public SearchYieldGate(SearchParity parity, SearchParityOptions options = null)
        {
            _parity = parity ?? throw new ArgumentNullException(nameof(parity));
            _options = options ?? SearchParityOptions.Default;
        }

        public string Name => GateNames.EconomyParity;

        /// <summary>The most a skilled board may find against Quick Search: the unperked limit, or the plan's +15% for a gatherer with perks.</summary>
        private double SkilledLimit => _options.Loadout == GatheringLoadout.None ? _options.UnperkedSkilledMaximumRatio : _options.SkilledMaximumRatio;

        /// <summary>The cases the study plays, so a report can play them all in parallel first.</summary>
        public IEnumerable<SearchKey> Keys()
        {
            foreach (var rank in _options.Ranks)
            {
                foreach (var player in new[] { SearchPlayerKind.Careless, SearchPlayerKind.Skilled, SearchPlayerKind.Oracle })
                    yield return new SearchKey(rank, 1, player, _options.EffectsAt(rank));

                yield return new SearchKey(rank, 1, SearchPlayerKind.Skilled, _options.TreasureSeekerLine);
                yield return new SearchKey(rank, 1, SearchPlayerKind.Oracle, _options.TreasureSeekerLine);
            }
        }

        public GateResult Evaluate(GateContext context)
        {
            var failures = new List<string>();
            var tolerance = _options.Tolerance;

            var quick = new EvidenceTable(
                "Gathering rank", "Hard Look level", "Second attempt", "Today's rolls", "Quick Search rolls", "Quick Search played",
                "Today's attempts", "Quick Search attempts", "Verdict");
            var board = new EvidenceTable(
                "Gathering rank", "Searches", "Promising spots", "Quick Search", "Careless", "Skilled", "Oracle",
                "Careless / Quick", "Skilled / Quick", "Oracle / Quick", "Verdict");
            var play = new EvidenceTable(
                "Gathering rank", "Skilled Sifts", "Skilled Dig Deeper attempts", "Skilled spots spoiled", "Skilled Dig Deeper finds", "Skilled Search finds", "Skilled attempts");
            var line = new EvidenceTable(
                "Gathering rank", "Quick Search", "Skilled, whole line", "Oracle, whole line", "Skilled, whole line / Quick", "Oracle, whole line / Quick", "Verdict");

            foreach (var rank in _options.Ranks)
            {
                var tier = CraftRankMilestones.HardLookLevelForRank(rank);
                var today = ScavengeModel.ExpectedSuccessesPerSite(tier, ScavengeQuickSearch.ReferencePerceptionModifier);
                var quickRolls = ScavengeQuickSearch.ExpectedRolls(tier);
                var played = PlayQuickSearch(rank, tier);
                var quickInside = Math.Abs(today - quickRolls) <= 1e-9 && Math.Abs(played.Rolls - quickRolls) <= 0.02;
                if (!quickInside)
                    failures.Add($"Quick Search at rank {rank} ({Number(quickRolls, 3)} rolls against today's {Number(today, 3)}, played {Number(played.Rolls, 3)})");

                quick.Add(
                    rank.ToString(),
                    tier.ToString(),
                    Percent(ScavengeQuickSearch.SecondAttemptChance(tier) / 100.0),
                    Number(today, 3),
                    Number(quickRolls, 3),
                    Number(played.Rolls, 3),
                    Number(ScavengeModel.ExpectedAttemptsPerSite(tier, 0), 2),
                    Number(ScavengeQuickSearch.ExpectedAttempts(tier), 2),
                    quickInside ? "Equal" : "Different");

                var effects = _options.EffectsAt(rank);
                var careless = _parity.Sample(rank, 1, SearchPlayerKind.Careless, effects);
                var skilled = _parity.Sample(rank, 1, SearchPlayerKind.Skilled, effects);
                var oracle = _parity.Sample(rank, 1, SearchPlayerKind.Oracle, effects);
                var setup = SearchSetup.ForRank(rank, 1, effects);
                var carelessRatio = careless.Finds / quickRolls;
                var skilledRatio = skilled.Finds / quickRolls;
                var oracleRatio = oracle.Finds / quickRolls;
                var inside = carelessRatio >= _options.CarelessMinimumRatio - tolerance &&
                             skilledRatio >= _options.SkilledMinimumRatio - tolerance &&
                             skilledRatio <= SkilledLimit + tolerance &&
                             oracleRatio <= _options.OracleMaximumRatio + tolerance;
                if (!inside)
                {
                    failures.Add(
                        $"rank {rank} (careless {Number(carelessRatio, 2)}x, skilled {Number(skilledRatio, 2)}x, oracle {Number(oracleRatio, 2)}x)");
                }

                board.Add(
                    rank.ToString(),
                    setup.Searches.ToString(),
                    setup.PromisingSpots.ToString(),
                    Number(quickRolls, 3),
                    Number(careless.Finds, 3),
                    Number(skilled.Finds, 3),
                    Number(oracle.Finds, 3),
                    Number(carelessRatio, 2) + "x",
                    Number(skilledRatio, 2) + "x",
                    Number(oracleRatio, 2) + "x",
                    inside ? "Within" : "Outside");

                play.Add(
                    rank.ToString(),
                    Number(skilled.SiftsPerBoard, 2),
                    Number(skilled.DigsPerBoard, 2),
                    Number(skilled.SpoiledPerBoard, 2),
                    Number(skilled.Deeper, 3),
                    Number(skilled.Ordinary, 3),
                    Number(skilled.AttemptsPerBoard, 2));

                var lineSkilled = _parity.Sample(rank, 1, SearchPlayerKind.Skilled, _options.TreasureSeekerLine);
                var lineOracle = _parity.Sample(rank, 1, SearchPlayerKind.Oracle, _options.TreasureSeekerLine);
                var lineSkilledRatio = lineSkilled.Finds / quickRolls;
                var lineOracleRatio = lineOracle.Finds / quickRolls;
                var lineInside = lineSkilledRatio <= _options.SkilledMaximumRatio + tolerance;
                if (!lineInside)
                    failures.Add($"rank {rank} with the whole Treasure Seeker line (skilled {Number(lineSkilledRatio, 2)}x)");

                line.Add(
                    rank.ToString(),
                    Number(quickRolls, 3),
                    Number(lineSkilled.Finds, 3),
                    Number(lineOracle.Finds, 3),
                    Number(lineSkilledRatio, 2) + "x",
                    Number(lineOracleRatio, 2) + "x",
                    lineInside ? "Within" : "Outside");
            }

            var tables = new[]
            {
                quick.ToGateTable("Quick Search against today's expected rolls (a Perception modifier of 0, the parity reference)"),
                board.ToGateTable(
                    $"Finds per board (loot-table rolls) against Quick Search: careless at least {Number(_options.CarelessMinimumRatio, 2)}x, skilled " +
                    $"{Number(_options.SkilledMinimumRatio, 2)}x to {Number(SkilledLimit, 2)}x, oracle at most {Number(_options.OracleMaximumRatio, 2)}x"),
                play.ToGateTable("How the skilled player spends its searches"),
                line.ToGateTable($"The whole Treasure Seeker line against the cap (skilled at most {Number(_options.SkilledMaximumRatio, 2)}x)")
            };

            return failures.Count == 0
                ? new GateResult(
                    Name,
                    GateVerdict.Passed,
                    $"Quick Search equals today's expected rolls at every rank; a careless board finds at least {Number(_options.CarelessMinimumRatio, 2)}x, the skilled player " +
                    $"{Number(_options.SkilledMinimumRatio, 2)}x to {Number(_options.UnperkedSkilledMaximumRatio, 2)}x, the oracle at most {Number(_options.OracleMaximumRatio, 2)}x, and the whole Treasure Seeker line stays inside the cap.",
                    tables)
                : new GateResult(Name, GateVerdict.Failed, $"Yield falls outside its limits: {string.Join("; ", failures)}.", tables);
        }

        /// <summary>Plays <see cref="SearchParityOptions.QuickSearchRuns"/> Quick Searches and returns the rolls (successful attempts) per site.</summary>
        private (double Rolls, double Attempts) PlayQuickSearch(int rank, int tier)
        {
            var random = SimMinigameRandom.ForRun(_parity.Seed, SimStreams.Legacy, 5000 + rank);
            double rolls = 0, attempts = 0;
            for (var run = 0; run < _options.QuickSearchRuns; run++)
            {
                var result = ScavengeQuickSearch.Resolve(random, tier, 600);
                rolls += result.Successes;
                attempts += result.Attempts.Count;
            }

            return (rolls / _options.QuickSearchRuns, attempts / _options.QuickSearchRuns);
        }
    }
}
