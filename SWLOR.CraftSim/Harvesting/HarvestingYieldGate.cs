using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Harvesting
{
    /// <summary>
    /// Gate 5 for harvesting, the economy parity of a node's yield: at each node tier and Gathering rank, Quick Harvest equals today's
    /// expected items per node, and a skilled session stays within +15% of it (and never below it, so playing is never worse than Quick
    /// Harvest). "Today" is the attribute reference the owner set: a Might modifier of 0, so no bonus item. Items are counted, not
    /// priced; gems are counted as items here and compared with today's gems in the report's evidence.
    /// </summary>
    public sealed class HarvestingYieldGate : IBalanceGate
    {
        private readonly HarvestingParity _parity;

        public HarvestingYieldGate(HarvestingParity parity)
        {
            _parity = parity ?? throw new ArgumentNullException(nameof(parity));
        }

        public string Name => GateNames.EconomyParity;

        public GateResult Evaluate(GateContext context)
        {
            var options = _parity.Options;
            var today = HarvestingParity.TodayItemsPerNode;
            var quick = new EvidenceTable(
                "Node tier", "Rank", "Today, items per node", "Today, played", "Quick Harvest, items per node", "Quick Harvest, played",
                "Quick Harvest, XP per node", "Today, XP per node", "Quick Harvest, seconds per node", "Today, seconds per node", "Verdict");
            var table = new EvidenceTable(
                "Node tier", "Rank", "Today", "Strike only", "Skilled session", "Skilled / today", "Skilled plan", "Highest of any plan / today", "Verdict");

            var failures = new List<string>();
            var highest = 0.0;
            var highestCase = string.Empty;
            var lowest = double.MaxValue;
            var lowestCase = string.Empty;
            foreach (var tier in _parity.Tiers)
            {
                foreach (var rank in _parity.RanksFor(tier))
                {
                    var label = $"tier {tier} / rank {rank}";
                    var skilled = _parity.Skilled(tier, rank);
                    var casual = _parity.Mean(tier, rank, ProspectorPlan.Casual);
                    var ceiling = _parity.Ceiling(tier, rank).Mean.Items / today;
                    var ratio = skilled.Mean.Items / today;
                    var passed = ceiling <= 1 + options.SkilledCapShare && ratio >= options.SkilledFloorShare;
                    if (!passed)
                        failures.Add($"{label} ({Number(ratio, 3)}x, ceiling {Number(ceiling, 3)}x)");

                    if (ceiling > highest)
                    {
                        highest = ceiling;
                        highestCase = label;
                    }

                    if (ratio < lowest)
                    {
                        lowest = ratio;
                        lowestCase = label;
                    }

                    table.Add(
                        tier.ToString(),
                        rank.ToString(),
                        Number(today, 2),
                        Number(casual.Items, 2),
                        Number(skilled.Mean.Items, 2),
                        Number(ratio, 3),
                        skilled.Plan.Name,
                        Number(ceiling, 3),
                        passed ? "Passed" : "Failed");
                }
            }

            var quickFailures = new List<string>();
            foreach (var tier in _parity.Tiers)
            {
                var rank = Math.Min(50, GatheringLevels.ForTier(tier));
                var mean = _parity.QuickHarvest(tier, rank);
                var xpPerUnit = HarvestingParity.QuickHarvestXpPerUnit(tier, rank);
                var todayXp = HarvestModel.ExpectedXpPerNode(tier, rank, 0);
                var quickXp = ProspectingQuickHarvest.ExpectedUnits * xpPerUnit;
                var quickSeconds = ProspectingQuickHarvest.ExpectedUnits * ProspectingRules.HarvestSeconds;
                var todaySeconds = HarvestModel.ExpectedHarvestsPerNode * options.SecondsPerHarvest;
                var expectedUnits = ProspectingQuickHarvest.ExpectedUnits;

                // The rule is the same rule: the units, the XP and the seconds are equal as formulas. The played means show it from the seed.
                var equal = Math.Abs(expectedUnits - today) < 1e-9 &&
                            Math.Abs(quickXp - todayXp) < 1e-6 &&
                            Math.Abs(quickSeconds - todaySeconds) < 1e-9 &&
                            Math.Abs(mean.Items - today) <= PlayedTolerance(_parity.Runs);
                if (!equal)
                    quickFailures.Add($"tier {tier} / rank {rank}");

                quick.Add(
                    tier.ToString(),
                    rank.ToString(),
                    Number(today, 2),
                    Number(PlayedToday(tier, rank), 2),
                    Number(expectedUnits, 2),
                    Number(mean.Items, 2),
                    Number(quickXp, 0),
                    Number(todayXp, 0),
                    Number(quickSeconds, 1),
                    Number(todaySeconds, 1),
                    equal ? "Passed" : "Failed");
            }

            var tables = new[]
            {
                quick.ToGateTable(
                    $"Quick Harvest against today's harvests, rank = the vein's level (the cohort's rank), per node at a Might modifier of 0 ({_parity.Runs} nodes played per row)"),
                table.ToGateTable(
                    $"Items per node by node tier and Gathering rank: a skilled session against today ({_parity.Runs} nodes per plan; the skilled player is the careful plan with the most items, and the last column is the highest of every plan, Heavy Strike plans included)")
            };

            if (quickFailures.Count > 0)
            {
                return new GateResult(Name, GateVerdict.Failed,
                    $"Quick Harvest does not equal today's harvests in {quickFailures.Count} case(s): {string.Join("; ", quickFailures)}.", tables);
            }

            if (failures.Count > 0)
            {
                return new GateResult(Name, GateVerdict.Failed,
                    $"A skilled session leaves the band of {Percent(options.SkilledFloorShare)} to {Percent(1 + options.SkilledCapShare)} of today's items per node in {failures.Count} case(s): {string.Join("; ", failures)}.",
                    tables);
            }

            return new GateResult(Name, GateVerdict.Passed,
                $"Quick Harvest equals today's {Number(today, 2)} items, {Number(ProspectingQuickHarvest.ExpectedUnits * ProspectingRules.HarvestSeconds, 1)} s and charges per node, with the same XP per item. " +
                $"A skilled session yields {Number(lowest, 3)}x (lowest, {lowestCase}) of today's items per node, and no plan, careful or fast, reaches more than {Number(highest, 3)}x ({highestCase}); the band is {Percent(options.SkilledFloorShare)} to {Percent(1 + options.SkilledCapShare)}.",
                tables);
        }

        /// <summary>
        /// The mean items per node of today's harvests played from the seed, over the module's kinds of node of the tier. Today's rule at a
        /// Might modifier of 0 is the same for every kind, so one run of the legacy loop stands for all.
        /// </summary>
        private double PlayedToday(int tier, int rank)
        {
            var rng = new Rng.SimRandom(_parity.Seed + 7919 * tier + rank);
            double items = 0;
            for (var node = 0; node < _parity.Runs; node++)
                items += HarvestModel.HarvestNode(rng, tier, rank, 0).Items;

            return items / _parity.Runs;
        }

        /// <summary>The most a mean of played nodes can stray from the exact mean: four standard errors of a d4 (standard deviation 1.12).</summary>
        private static double PlayedTolerance(int runs) => 4 * 1.12 / Math.Sqrt(runs);
    }
}
