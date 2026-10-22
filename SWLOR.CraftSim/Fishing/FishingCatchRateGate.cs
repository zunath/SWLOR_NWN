using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>
    /// The catch-rate half of the economy-parity gate for fishing: the fraction of bites that land the fish must stay near today's
    /// (<c>40 + 10 x (rank - fish level) + 2 x Willpower</c>, kept from 0 to 95). Today's rate depends on a character's Willpower,
    /// which the fight no longer reads, so it is a corridor: the gate takes the corridor between the Willpower scores in
    /// <see cref="FishingParityOptions"/> and asks that the adaptive player's rate sit inside it, give or take a tolerance, both by rank
    /// above the fish's level and for each rank over the pools of fish a rod can reach.
    /// </summary>
    public sealed class FishingCatchRateGate : IBalanceGate
    {
        private readonly FishingParity _parity;
        private readonly FishingParityOptions _options;

        public FishingCatchRateGate(FishingParity parity, FishingParityOptions options = null)
        {
            _parity = parity ?? throw new ArgumentNullException(nameof(parity));
            _options = options ?? FishingParityOptions.Default;
        }

        public string Name => GateNames.EconomyParity;

        /// <summary>Today's catch chance for a character of the given Willpower score, in percent.</summary>
        public static int Today(int delta, int willpower) => FishingCatchModel.CatchChancePercent(delta, 0, willpower);

        public GateResult Evaluate(GateContext context)
        {
            var scores = context.Attributes.Modifiers.Select(AttributeSweep.ScoreFor).ToList();
            var pools = FishingCatchModel.Pools();
            Prepare(pools);

            var byDelta = ByDelta(scores);
            var byRank = ByRank(scores, pools);
            var outside = byDelta.Outside.Concat(byRank.Outside).ToList();
            var tables = new[] { byDelta.Table, byRank.Table };
            var worst = Math.Max(byDelta.WorstPoints, byRank.WorstPoints);

            if (outside.Count == 0)
            {
                return new GateResult(
                    Name,
                    GateVerdict.Passed,
                    $"Catch rate per bite: the adaptive player stays within {Number(_options.TolerancePoints, 0)} points of today's Willpower " +
                    $"{_options.CorridorLowScore}-{_options.CorridorHighScore} corridor at every rank above the fish's level and at every rank over the real pools " +
                    $"(furthest outside: {Number(worst)} points).",
                    tables);
            }

            return new GateResult(
                Name,
                GateVerdict.Failed,
                $"Catch rate per bite: the adaptive player is more than {Number(_options.TolerancePoints, 0)} points outside today's corridor in " +
                $"{outside.Count} row(s): {string.Join("; ", outside)}.",
                tables);
        }

        private void Prepare(IReadOnlyList<FishingPool> pools)
        {
            var keys = new List<CatchKey>();
            foreach (var player in new[] { LinePlayer.Adaptive, LinePlayer.RuleOfThumb })
            {
                foreach (var rank in _options.ReferenceRanks)
                {
                    foreach (var delta in _options.Deltas.Where(delta => rank - delta >= 0))
                        keys.Add(new CatchKey(rank - delta, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), player, LineAnglerEffects.None));
                }

                foreach (var rank in _options.PooledRanks)
                {
                    foreach (var pool in Reachable(pools, rank))
                    {
                        foreach (var entry in pool.Entries)
                            keys.Add(new CatchKey(entry.Level, RarityOf(entry.Fish), rank, FishingRodTier.Tier(pool.Rod), player, LineAnglerEffects.None));
                    }
                }
            }

            _parity.Prepare(keys);
        }

        private (GateTable Table, IReadOnlyList<string> Outside, double WorstPoints) ByDelta(IReadOnlyList<int> scores)
        {
            var header = new List<string> { "Rank - fish level" };
            header.AddRange(scores.Select(score => $"Today, Willpower {score}"));
            header.AddRange(new[] { "Fight, adaptive", "Adaptive, by rank", "Fight, rule of thumb", "Adaptive turns", "Verdict" });
            var table = new EvidenceTable(header.ToArray());

            var outside = new List<string>();
            var worst = 0.0;
            foreach (var delta in _options.Deltas)
            {
                var played = _options.ReferenceRanks
                    .Where(rank => rank - delta >= 0)
                    .Select(rank => (Rank: rank, Adaptive: _parity.Catch(rank - delta, FishRarity.Common, rank, FishingParity.NominalRodTier(rank)),
                        Rule: _parity.Catch(rank - delta, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), LinePlayer.RuleOfThumb)))
                    .ToList();
                if (played.Count == 0)
                    continue;

                var adaptive = played.Average(p => p.Adaptive.LandedPercent);
                var low = Today(delta, _options.CorridorLowScore);
                var high = Today(delta, _options.CorridorHighScore);
                var miss = Math.Max(low - adaptive, adaptive - high);
                var near = miss <= _options.TolerancePoints;
                worst = Math.Max(worst, miss);
                if (!near)
                    outside.Add($"{Signed(delta)} ({Number(adaptive, 0)}% against {low}-{high}%)");

                var row = new List<string> { Signed(delta) };
                row.AddRange(scores.Select(score => Today(delta, score) + "%"));
                row.Add(Number(adaptive, 0) + "%");
                row.Add($"{Number(played.Min(p => p.Adaptive.LandedPercent), 0)}-{Number(played.Max(p => p.Adaptive.LandedPercent), 0)}%");
                row.Add(Number(played.Average(p => p.Rule.LandedPercent), 0) + "%");
                row.Add(Number(played.Average(p => p.Adaptive.MeanTurns)));
                row.Add(near ? "Near" : "Outside");
                table.Add(row.ToArray());
            }

            return (table.ToGateTable(
                    $"Landed per bite by rank above the fish's level, at ranks {string.Join(", ", _options.ReferenceRanks)} with the best rod each can use " +
                    $"({_parity.Runs} fights per fish; corridor Willpower {_options.CorridorLowScore}-{_options.CorridorHighScore})"),
                outside,
                worst);
        }

        private (GateTable Table, IReadOnlyList<string> Outside, double WorstPoints) ByRank(IReadOnlyList<int> scores, IReadOnlyList<FishingPool> pools)
        {
            var table = new EvidenceTable(
                "Agriculture rank",
                $"Today, Willpower {_options.CorridorLowScore}",
                $"Today, Willpower {_options.CorridorHighScore}",
                $"Today, Willpower {scores.Min()}-{scores.Max()}",
                "Fight, adaptive",
                "Fight, rule of thumb",
                "Adaptive turns",
                "Pools",
                "Verdict");

            var outside = new List<string>();
            var worst = 0.0;
            foreach (var rank in _options.PooledRanks)
            {
                var reachable = Reachable(pools, rank).ToList();
                if (reachable.Count == 0)
                    continue;

                double TodayAt(int score) => reachable.Average(pool => Weighted(pool, entry => FishingCatchModel.CatchChancePercent(rank, entry.Level, score)));
                double Fight(LinePlayer player, Func<LineCatchSample, double> read) => reachable.Average(pool => Weighted(pool, entry =>
                    read(_parity.Catch(entry.Level, RarityOf(entry.Fish), rank, FishingRodTier.Tier(pool.Rod), player))));

                var low = TodayAt(_options.CorridorLowScore);
                var high = TodayAt(_options.CorridorHighScore);
                var adaptive = Fight(LinePlayer.Adaptive, sample => sample.LandedPercent);
                var miss = Math.Max(low - adaptive, adaptive - high);
                var near = miss <= _options.TolerancePoints;
                worst = Math.Max(worst, miss);
                if (!near)
                    outside.Add($"rank {rank} ({Number(adaptive, 0)}% against {Number(low, 0)}-{Number(high, 0)}%)");

                var sweep = scores.Select(TodayAt).ToList();
                table.Add(
                    rank.ToString(),
                    Number(low, 0) + "%",
                    Number(high, 0) + "%",
                    $"{Number(sweep.Min(), 0)}-{Number(sweep.Max(), 0)}%",
                    Number(adaptive, 0) + "%",
                    Number(Fight(LinePlayer.RuleOfThumb, sample => sample.LandedPercent), 0) + "%",
                    Number(Fight(LinePlayer.Adaptive, sample => sample.MeanTurns)),
                    reachable.Count.ToString(),
                    near ? "Near" : "Outside");
            }

            return (table.ToGateTable(
                    "Landed per bite by Agriculture rank, over every rod and bait pool the angler can use " +
                    "(each location, rod and bait, daytime and nighttime counted separately; pools weighted equally, fish within a pool by frequency; " +
                    "the pool average mixes pools no single angler fishes, so it compares rules and is not a rank's own experience)"),
                outside,
                worst);
        }

        /// <summary>The pools whose rod an angler of this rank can use.</summary>
        internal static IEnumerable<FishingPool> Reachable(IReadOnlyList<FishingPool> pools, int rank) =>
            pools.Where(pool => FishingRodTier.RequiredRank(pool.Rod) <= rank);

        internal static FishRarity RarityOf(FishType fish) => fish.GetAttribute<FishType, FishAttribute>().Rarity;

        /// <summary>A pool's value for a stat of each fish, weighted by how often the pool offers the fish.</summary>
        private static double Weighted(FishingPool pool, Func<FishingPoolEntry, double> value)
        {
            var total = pool.Entries.Sum(entry => (double)entry.Frequency);
            return pool.Entries.Sum(entry => entry.Frequency / total * value(entry));
        }
    }
}
