using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Search
{
    /// <summary>
    /// The length gate for the board: the mean number of actions of a board stays within the plan's target for a gathering node
    /// (<see cref="LengthTarget.GatheringNode"/>, 3-8 actions) at every Gathering rank, for every simulated player. Every action costs a
    /// search, so a board lasts as many actions as the scavenger has searches; the gate reads them from the played boards.
    /// </summary>
    public sealed class SearchLengthGate : IBalanceGate
    {
        private readonly SearchParity _parity;
        private readonly SearchParityOptions _options;

        public SearchLengthGate(SearchParity parity, SearchParityOptions options = null)
        {
            _parity = parity ?? throw new ArgumentNullException(nameof(parity));
            _options = options ?? SearchParityOptions.Default;
        }

        public string Name => GateNames.Length;

        public GateResult Evaluate(GateContext context)
        {
            var target = LengthTarget.GatheringNode;
            var table = new EvidenceTable("Gathering rank", "Searches", "Careless actions", "Skilled actions", "Oracle actions", "Verdict");
            var outside = new List<string>();
            foreach (var rank in _options.Ranks)
            {
                var careless = _parity.Sample(rank, 1, SearchPlayerKind.Careless).ActionsPerBoard;
                var skilled = _parity.Sample(rank, 1, SearchPlayerKind.Skilled).ActionsPerBoard;
                var oracle = _parity.Sample(rank, 1, SearchPlayerKind.Oracle).ActionsPerBoard;
                var inside = new[] { careless, skilled, oracle }.All(actions => target.Contains(actions));
                if (!inside)
                    outside.Add($"rank {rank} ({Number(careless)}, {Number(skilled)}, {Number(oracle)} actions)");

                table.Add(
                    rank.ToString(),
                    SearchSetup.ForRank(rank, 1).Searches.ToString(),
                    Number(careless, 2),
                    Number(skilled, 2),
                    Number(oracle, 2),
                    inside ? "Within" : "Outside");
            }

            var gateTable = table.ToGateTable($"Actions per board by Gathering rank (target {target})");
            return outside.Count == 0
                ? new GateResult(Name, GateVerdict.Passed, $"Every board lasts {target}, for every player, at every rank.", new[] { gateTable })
                : new GateResult(Name, GateVerdict.Failed, $"A board falls outside {target} at {outside.Count} rank(s): {string.Join("; ", outside)}.", new[] { gateTable });
        }
    }
}
