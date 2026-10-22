using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.FishingService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>
    /// The economy-parity gate for fishing, for Quick Reel: the choice that lands a fish at once by today's catch roll
    /// (<c>40 + 10 x (rank - fish level) + 2 x Willpower</c>, kept from 0 to 95) at a fixed Willpower score. Today's roll depends on a
    /// character's Willpower, which Quick Reel no longer reads, so the gate asks that Quick Reel's chance sit inside the corridor between the
    /// Willpower scores in <see cref="FishingParityOptions"/> at every rank above the fish's level and at every rank over the real pools, and
    /// that its fish and XP per hour match today's: it takes no fight time and pays today's XP for the bite. The fight is held to its own
    /// bound by <see cref="FishingFightBoundGate"/>.
    /// </summary>
    public sealed class FishingCatchRateGate : IBalanceGate
    {
        /// <summary>How far Quick Reel's fish or XP per hour may sit from today's at the reference Willpower, as a share.</summary>
        public const double PerHourTolerance = 0.005;

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

            var byDelta = ByDelta(scores);
            var byRank = ByRank(scores, pools);

            var cohort = new FishingXpRateCohort(_parity, _options);
            cohort.Prepare();
            var perHour = FishingPerHour.Rows(cohort, _parity, _options, context);
            var tables = new[] { byDelta.Table, byRank.Table, FishingPerHour.Table(perHour, _options) };

            var outside = byDelta.Outside.Concat(byRank.Outside).ToList();
            var fishRatios = perHour.Select(row => row.QuickReelFish / row.TodayFishReference).ToList();
            var xpRatios = perHour.Select(row => row.QuickReelXp / row.TodayXp).ToList();
            var offReference = fishRatios.Concat(xpRatios).Any(ratio => Math.Abs(ratio - 1) > PerHourTolerance);

            var corridor = perHour.SelectMany(row => new[] { row.QuickReelFish / row.TodayFishLow, row.QuickReelFish / row.TodayFishHigh }).ToList();
            var detail =
                $"Quick Reel lands {Number(fishRatios.Min(), 2)}x to {Number(fishRatios.Max(), 2)}x of today's fish per hour at Willpower {_options.QuickReelWillpowerScore} " +
                $"({Number(corridor.Min(), 2)}x to {Number(corridor.Max(), 2)}x across the Willpower {_options.CorridorLowScore}-{_options.CorridorHighScore} corridor, by band) " +
                $"and pays {Number(xpRatios.Min(), 2)}x to {Number(xpRatios.Max(), 2)}x of today's XP per hour.";

            if (outside.Count == 0 && !offReference)
            {
                return new GateResult(
                    Name,
                    GateVerdict.Passed,
                    $"Quick Reel rolls today's formula at Willpower {_options.QuickReelWillpowerScore} (pending the owner's attribute-median decision) and stays inside " +
                    $"today's Willpower {_options.CorridorLowScore}-{_options.CorridorHighScore} corridor at every rank above the fish's level and at every rank over the real pools. {detail}",
                    tables);
            }

            var reasons = new List<string>();
            if (outside.Count > 0)
                reasons.Add($"Quick Reel leaves today's corridor in {outside.Count} row(s): {string.Join("; ", outside)}");
            if (offReference)
                reasons.Add($"its per-hour fish or XP is more than {Number(PerHourTolerance * 100, 1)}% off today's at Willpower {_options.QuickReelWillpowerScore}");

            return new GateResult(Name, GateVerdict.Failed, string.Join("; ", reasons) + ". " + detail, tables);
        }

        private (GateTable Table, IReadOnlyList<string> Outside) ByDelta(IReadOnlyList<int> scores)
        {
            var header = new List<string> { "Rank - fish level" };
            header.AddRange(scores.Select(score => $"Today, Willpower {score}"));
            header.AddRange(new[] { $"Quick Reel (Willpower {_options.QuickReelWillpowerScore})", "Verdict" });
            var table = new EvidenceTable(header.ToArray());

            var outside = new List<string>();
            foreach (var delta in _options.Deltas)
            {
                var low = Today(delta, _options.CorridorLowScore);
                var high = Today(delta, _options.CorridorHighScore);
                var quickReel = _options.QuickReel(delta, 0);
                var inside = quickReel >= low && quickReel <= high;
                if (!inside)
                    outside.Add($"{Signed(delta)} ({quickReel}% against {low}-{high}%)");

                var row = new List<string> { Signed(delta) };
                row.AddRange(scores.Select(score => Today(delta, score) + "%"));
                row.Add(quickReel + "%");
                row.Add(inside ? "Inside" : "Outside");
                table.Add(row.ToArray());
            }

            return (table.ToGateTable(
                    $"Landed per bite by rank above the fish's level: today's catch roll by Willpower and Quick Reel (corridor Willpower {_options.CorridorLowScore}-{_options.CorridorHighScore})"),
                outside);
        }

        private (GateTable Table, IReadOnlyList<string> Outside) ByRank(IReadOnlyList<int> scores, IReadOnlyList<FishingPool> pools)
        {
            var table = new EvidenceTable(
                "Agriculture rank",
                $"Today, Willpower {_options.CorridorLowScore}",
                $"Today, Willpower {_options.CorridorHighScore}",
                $"Today, Willpower {scores.Min()}-{scores.Max()}",
                $"Quick Reel (Willpower {_options.QuickReelWillpowerScore})",
                "Pools",
                "Verdict");

            var outside = new List<string>();
            foreach (var rank in _options.PooledRanks)
            {
                var reachable = Reachable(pools, rank).ToList();
                if (reachable.Count == 0)
                    continue;

                double TodayAt(int score) => reachable.Average(pool => Weighted(pool, entry => FishingCatchModel.CatchChancePercent(rank, entry.Level, score)));

                var low = TodayAt(_options.CorridorLowScore);
                var high = TodayAt(_options.CorridorHighScore);
                var quickReel = reachable.Average(pool => Weighted(pool, entry => _options.QuickReel(rank, entry.Level)));
                var inside = quickReel >= low - 1e-9 && quickReel <= high + 1e-9;
                if (!inside)
                    outside.Add($"rank {rank} ({Number(quickReel, 0)}% against {Number(low, 0)}-{Number(high, 0)}%)");

                var sweep = scores.Select(TodayAt).ToList();
                table.Add(
                    rank.ToString(),
                    Number(low, 0) + "%",
                    Number(high, 0) + "%",
                    $"{Number(sweep.Min(), 0)}-{Number(sweep.Max(), 0)}%",
                    Number(quickReel, 0) + "%",
                    reachable.Count.ToString(),
                    inside ? "Inside" : "Outside");
            }

            return (table.ToGateTable(
                    "Landed per bite by Agriculture rank, over every rod and bait pool the angler can use " +
                    "(each location, rod and bait, daytime and nighttime counted separately; pools weighted equally, fish within a pool by frequency; " +
                    "the pool average mixes pools no single angler fishes, so it compares rules and is not a rank's own experience)"),
                outside);
        }

        /// <summary>The pools whose rod an angler of this rank can use.</summary>
        internal static IEnumerable<FishingPool> Reachable(IReadOnlyList<FishingPool> pools, int rank) =>
            pools.Where(pool => FishingRodTier.RequiredRank(pool.Rod) <= rank);

        internal static FishRarity RarityOf(FishType fish) => fish.GetAttribute<FishType, FishAttribute>().Rarity;

        /// <summary>A pool's value for a stat of each fish, weighted by how often the pool offers the fish.</summary>
        internal static double Weighted(FishingPool pool, Func<FishingPoolEntry, double> value)
        {
            var total = pool.Entries.Sum(entry => (double)entry.Frequency);
            return pool.Entries.Sum(entry => entry.Frequency / total * value(entry));
        }
    }
}
