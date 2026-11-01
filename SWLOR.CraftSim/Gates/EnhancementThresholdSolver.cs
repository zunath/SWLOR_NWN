namespace SWLOR.CraftSim.Gates
{
    /// <summary>
    /// A quality distribution that must transfer at least <see cref="RequiredTransfers"/> enhancements per attempt when the item has
    /// <see cref="Slots"/> filled slots: today's expected figure times the gate's required ratio.
    /// </summary>
    public sealed record ParityCase(string Name, int Slots, QualityDistribution Distribution, double RequiredTransfers);

    /// <summary>The thresholds, one per slot in slot order, that meet every case; <see cref="Reachable"/> is false when none do.</summary>
    public sealed record ThresholdSolution(bool Reachable, IReadOnlyList<int> Thresholds)
    {
        public static readonly ThresholdSolution Unreachable = new(false, Array.Empty<int>());

        /// <summary>The thresholds as "50% / 70% / 70%", or "none" when no thresholds meet the cases.</summary>
        public string Describe() => Reachable ? string.Join(" / ", Thresholds.Select(t => t + "%")) : "none";
    }

    /// <summary>
    /// Finds the enhancement thresholds that meet parity for quality distributions. A slot's enhancement transfers when the item is made
    /// at or above that slot's threshold, so a case with n slots gets the sum over slots 1..n of the share of attempts made at or above
    /// each threshold. The thresholds are shared by every case, never fall from one slot to the next, and are whole percentages (in
    /// steps of <c>step</c>). Of the thresholds that meet every case the solver returns the strictest: the largest sum, and among equal
    /// sums the largest first threshold, then second, then third. That keeps as much of the quality incentive as parity allows.
    /// </summary>
    public static class EnhancementThresholdSolver
    {
        private const double Tolerance = 1e-9;

        /// <param name="cases">The distributions and the transfers each must reach. The number of thresholds is the largest slot count among them.</param>
        /// <param name="step">The thresholds are multiples of this many percent (1 for exact, 5 for round figures).</param>
        public static ThresholdSolution Solve(IReadOnlyList<ParityCase> cases, int step = 1)
        {
            if (cases == null)
                throw new ArgumentNullException(nameof(cases));
            if (step < 1 || step > 100)
                throw new ArgumentOutOfRangeException(nameof(step), "The step is a whole number of percent from 1 to 100.");
            if (cases.Count == 0)
                return ThresholdSolution.Unreachable;
            if (cases.Any(c => c.Slots < 1))
                throw new ArgumentException("A case needs at least one slot.", nameof(cases));

            var slots = cases.Max(c => c.Slots);
            var levels = new List<int>();
            for (var level = 0; level <= 100; level += step)
                levels.Add(level);

            // share[c][level index] is the share of the case's attempts made at or above the level.
            var share = cases.Select(c => levels.Select(level => c.Distribution.ShareAtLeast(level)).ToArray()).ToArray();

            var current = new int[slots];
            int[] best = null;
            Search(0);
            return best == null ? ThresholdSolution.Unreachable : new ThresholdSolution(true, best.Select(index => levels[index]).ToList());

            // Fills the slots in order. A threshold never falls from one slot to the next, so a slot's level is at least the previous
            // slot's, and a prefix that cannot meet a case is dropped with everything under it.
            void Search(int slot)
            {
                if (slot == slots)
                {
                    if (IsBetter(current, best))
                        best = (int[])current.Clone();

                    return;
                }

                var lowest = slot == 0 ? 0 : current[slot - 1];
                for (var index = levels.Count - 1; index >= lowest; index--)
                {
                    current[slot] = index;
                    if (Feasible(slot, index))
                        Search(slot + 1);
                }
            }

            // After choosing slot j at index i, every case that already has all its slots is decided, and a case that needs more slots
            // can still reach its target only if the remaining slots at the lowest allowed level (index i) would. Tails never rise with
            // the level, so that is the best the remaining slots can do.
            bool Feasible(int slot, int index)
            {
                for (var c = 0; c < cases.Count; c++)
                {
                    var needed = cases[c].Slots;
                    var total = 0.0;
                    for (var s = 0; s < needed; s++)
                        total += share[c][s <= slot ? current[s] : index];

                    if (total + Tolerance < cases[c].RequiredTransfers)
                        return false;
                }

                return true;
            }

            static bool IsBetter(int[] candidate, int[] incumbent)
            {
                if (incumbent == null)
                    return true;

                var candidateSum = candidate.Sum();
                var incumbentSum = incumbent.Sum();
                if (candidateSum != incumbentSum)
                    return candidateSum > incumbentSum;

                for (var i = 0; i < candidate.Length; i++)
                {
                    if (candidate[i] != incumbent[i])
                        return candidate[i] > incumbent[i];
                }

                return false;
            }
        }
    }
}
