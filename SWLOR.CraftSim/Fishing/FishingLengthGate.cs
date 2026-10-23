using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.FishingService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>
    /// The length gate for the fight: the adaptive player's mean number of turns stays within the plan's target for a fishing fight
    /// (<see cref="LengthTarget.FishingFight"/>, 3-6 turns) against fish from <see cref="LowestDelta"/> levels below the rank to
    /// <see cref="HighestDelta"/> above it, the range in which today's catch chance is neither zero nor capped. A fish far below the
    /// rank is landed in the fewest turns the line allows, and one far above it is lost quickly, so neither says how long a fight lasts.
    /// </summary>
    public sealed class FishingLengthGate : IBalanceGate
    {
        /// <summary>The fish this many levels above the rank are the hardest the gate judges.</summary>
        public const int LowestDelta = -6;

        /// <summary>The fish this many levels below the rank are the easiest the gate judges.</summary>
        public const int HighestDelta = 4;

        private readonly FishingParity _parity;
        private readonly FishingParityOptions _options;

        public FishingLengthGate(FishingParity parity, FishingParityOptions options = null)
        {
            _parity = parity ?? throw new ArgumentNullException(nameof(parity));
            _options = options ?? FishingParityOptions.Default;
        }

        public string Name => GateNames.Length;

        public GateResult Evaluate(GateContext context)
        {
            var target = LengthTarget.FishingFight;
            var table = new EvidenceTable("Rank - fish level", "Adaptive turns", "Rule-of-thumb turns", "Verdict");
            var outside = new List<string>();
            foreach (var delta in _options.Deltas.Where(delta => delta >= LowestDelta && delta <= HighestDelta))
            {
                var turns = Turns(delta, LinePlayer.Adaptive);
                var casual = Turns(delta, LinePlayer.RuleOfThumb);
                if (double.IsNaN(turns))
                    continue;

                var inside = target.Contains(turns);
                if (!inside)
                    outside.Add($"{Signed(delta)} ({Number(turns)} turns)");

                table.Add(Signed(delta), Number(turns), Number(casual), inside ? "Within" : "Outside");
            }

            var title = $"Fight length by rank above the fish's level (target {target}; mean over ranks {string.Join(", ", _options.ReferenceRanks)})";
            return outside.Count == 0
                ? new GateResult(Name, GateVerdict.Passed, $"The adaptive player's fights last {target} in every row.", new[] { table.ToGateTable(title) })
                : new GateResult(
                    Name,
                    GateVerdict.Failed,
                    $"The adaptive player's mean fight falls outside {target} in {outside.Count} row(s): {string.Join("; ", outside)}.",
                    new[] { table.ToGateTable(title) });
        }

        private double Turns(int delta, LinePlayer player)
        {
            var turns = _options.ReferenceRanks
                .Where(rank => rank - delta >= 0)
                .Select(rank => _parity.Catch(rank - delta, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), player).MeanTurns)
                .ToList();
            return turns.Count == 0 ? double.NaN : turns.Average();
        }
    }
}
