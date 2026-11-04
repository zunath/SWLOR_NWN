using SWLOR.CraftSim.Gates;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Harvesting
{
    /// <summary>
    /// Gate 4 for harvesting, length: a node session takes 3 to 8 actions (the plan's length target for a gathering node). The skilled
    /// player's mean actions per node, at each tier and Gathering rank, must sit in that range. The report also shows the share of
    /// individual sessions inside it, because a node with one loose unit and no seam can only take a Strike or two.
    /// </summary>
    public sealed class HarvestingLengthGate : IBalanceGate
    {
        private readonly HarvestingParity _parity;

        public HarvestingLengthGate(HarvestingParity parity)
        {
            _parity = parity ?? throw new ArgumentNullException(nameof(parity));
        }

        public string Name => GateNames.Length;

        public GateResult Evaluate(GateContext context)
        {
            var options = _parity.Options;
            var table = new EvidenceTable(
                "Node tier", "Rank", "Skilled actions per node", "Skilled sessions of 3-8 actions", "Strike only, actions per node", "Skilled plan", "Verdict");

            var failures = new List<string>();
            double lowest = double.MaxValue, highest = 0;
            foreach (var tier in _parity.Tiers)
            {
                foreach (var rank in _parity.RanksFor(tier))
                {
                    var skilled = _parity.Skilled(tier, rank);
                    var casual = _parity.Mean(tier, rank, Harvesting.ProspectorPlan.Casual);
                    var passed = skilled.Mean.Actions >= options.LeastActions && skilled.Mean.Actions <= options.MostActions;
                    if (!passed)
                        failures.Add($"tier {tier} / rank {rank} ({Number(skilled.Mean.Actions, 2)})");

                    lowest = Math.Min(lowest, skilled.Mean.Actions);
                    highest = Math.Max(highest, skilled.Mean.Actions);
                    table.Add(
                        tier.ToString(),
                        rank.ToString(),
                        Number(skilled.Mean.Actions, 2),
                        Percent(skilled.Mean.ShareWithinLength),
                        Number(casual.Actions, 2),
                        skilled.Plan.Name,
                        passed ? "Within" : "Outside");
                }
            }

            var tables = new[]
            {
                table.ToGateTable(
                    $"Actions per node, target {Number(options.LeastActions, 0)}-{Number(options.MostActions, 0)} (Leave is not an action; every action takes a few seconds)")
            };

            return failures.Count > 0
                ? new GateResult(Name, GateVerdict.Failed,
                    $"The skilled player's actions per node fall outside {Number(options.LeastActions, 0)}-{Number(options.MostActions, 0)} in {failures.Count} case(s): {string.Join("; ", failures)}.",
                    tables)
                : new GateResult(Name, GateVerdict.Passed,
                    $"The skilled player's sessions last {Number(lowest, 2)} to {Number(highest, 2)} actions on average in every case (target {Number(options.LeastActions, 0)}-{Number(options.MostActions, 0)}).",
                    tables);
        }
    }
}
