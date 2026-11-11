using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>
    /// The qualities a set of attempts ended at, in whole percent as the outcome rules read them, with the attempts that made no item
    /// counted too. It answers how many enhancements a set of thresholds would transfer from those attempts.
    /// </summary>
    public sealed class QualityDistribution
    {
        public const int Levels = 101;

        private readonly int[] _counts;

        public QualityDistribution(IReadOnlyList<int> countsByPercent, int attempts)
        {
            if (countsByPercent == null)
                throw new ArgumentNullException(nameof(countsByPercent));
            if (countsByPercent.Count != Levels)
                throw new ArgumentException($"A quality distribution needs {Levels} counts, one for each whole percent from 0 to 100.", nameof(countsByPercent));

            _counts = countsByPercent.ToArray();
            Attempts = attempts;
            Successes = _counts.Sum();
            if (_counts.Any(count => count < 0) || Successes > attempts)
                throw new ArgumentException("The counts must not be negative or exceed the attempts.", nameof(countsByPercent));
        }

        /// <summary>The attempts played, including those that made no item.</summary>
        public int Attempts { get; }

        /// <summary>The attempts that made the item.</summary>
        public int Successes { get; }

        public IReadOnlyList<int> CountsByPercent => _counts;

        public static QualityDistribution From(IEnumerable<CohortCell> cells)
        {
            var counts = new int[Levels];
            var attempts = 0;
            foreach (var cell in cells)
            {
                attempts += cell.Runs;
                for (var percent = 0; percent < Levels; percent++)
                    counts[percent] += cell.QualityPercentCounts[percent];
            }

            return new QualityDistribution(counts, attempts);
        }

        public double SuccessShare => Attempts == 0 ? 0 : Successes / (double)Attempts;

        /// <summary>The share of all attempts that made the item at <paramref name="percent"/> quality or better.</summary>
        public double ShareAtLeast(int percent)
        {
            if (Attempts == 0)
                return 0;

            var atLeast = 0;
            for (var level = Math.Max(0, percent); level < Levels; level++)
                atLeast += _counts[level];

            return atLeast / (double)Attempts;
        }

        /// <summary>
        /// The standard error of <see cref="ExpectedTransfers(int)"/> as a mean over the attempts, which it treats as independent. It says how far
        /// the figure can move with another seed; a figure within a couple of errors of today's is a coin toss.
        /// </summary>
        public double TransfersStandardError(int slots)
        {
            if (Attempts <= 1)
                return 0;

            var thresholds = CraftOutcomeRules.GetEnhancementThresholds(slots);
            double sum = 0, sumOfSquares = 0;
            for (var percent = 0; percent < Levels; percent++)
            {
                var transferred = thresholds.Count(threshold => percent >= threshold);
                sum += (double)_counts[percent] * transferred;
                sumOfSquares += (double)_counts[percent] * transferred * transferred;
            }

            var mean = sum / Attempts;
            var variance = Math.Max(0, sumOfSquares / Attempts - mean * mean);
            return Math.Sqrt(variance / Attempts);
        }

        /// <summary>
        /// The expected number of enhancements transferred per attempt by an item with <paramref name="slots"/> filled slots: each slot's
        /// enhancement transfers when the item is made at or above that slot's threshold, as <c>CraftOutcomeRules.CalculateSuccess</c> decides.
        /// </summary>
        public double ExpectedTransfers(int slots, IReadOnlyList<int> thresholds)
        {
            var total = 0.0;
            for (var slot = 0; slot < slots; slot++)
                total += ShareAtLeast(thresholds[slot]);

            return total;
        }

        /// <summary><see cref="ExpectedTransfers"/> under the thresholds in force in <see cref="CraftOutcomeRules"/>.</summary>
        public double ExpectedTransfers(int slots) => ExpectedTransfers(slots, CraftOutcomeRules.GetEnhancementThresholds(slots));

        /// <summary>The lowest whole percent at or below which at least <paramref name="share"/> of the items made fall (0.5 is the median).</summary>
        public int Percentile(double share)
        {
            if (Successes == 0)
                return 0;

            var target = Math.Max(1, (int)Math.Ceiling(Math.Clamp(share, 0, 1) * Successes));
            var running = 0;
            for (var percent = 0; percent < Levels; percent++)
            {
                running += _counts[percent];
                if (running >= target)
                    return percent;
            }

            return Levels - 1;
        }

        /// <summary>The mean quality, in whole percent, of the items made.</summary>
        public double MeanPercent
        {
            get
            {
                if (Successes == 0)
                    return 0;

                long sum = 0;
                for (var percent = 0; percent < Levels; percent++)
                    sum += (long)percent * _counts[percent];

                return sum / (double)Successes;
            }
        }
    }
}
