namespace SWLOR.CraftSim.Data
{
    /// <summary>
    /// The enhancements an attempt is made with, for the enhancement-parity gate: how many slots hold an enhancement and the progress
    /// penalty each adds, in the old craft window's progress units (the sum of the enhancement item's <c>ProgressPenalty</c> item
    /// properties, which <c>CraftViewModel</c> adds to the progress the piece needs). A minigame's scenario maps the total onto its own
    /// progress meter through its engine's enhancement handling, such as the Forge's extra Shape or the Kitchen's extra Doneness.
    /// </summary>
    public sealed record CraftEnhancementConfiguration
    {
        public static readonly CraftEnhancementConfiguration None = new(0, 0);

        public CraftEnhancementConfiguration(int count, int progressPenaltyEach)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "An attempt cannot hold fewer than no enhancements.");
            if (progressPenaltyEach < 0)
                throw new ArgumentOutOfRangeException(nameof(progressPenaltyEach), "A progress penalty cannot be negative.");

            Count = count;
            ProgressPenaltyEach = progressPenaltyEach;
        }

        /// <summary>The slots that hold an enhancement; they are slots 1 to <see cref="Count"/>, so the thresholds in force are the first <see cref="Count"/> of <c>CraftOutcomeRules</c>.</summary>
        public int Count { get; }

        /// <summary>The progress penalty of one enhancement, in the old craft window's progress units.</summary>
        public int ProgressPenaltyEach { get; }

        /// <summary>The progress every enhancement adds together, in the old craft window's progress units.</summary>
        public int TotalProgressPenalty => Count * ProgressPenaltyEach;

        public bool IsNone => Count == 0;

        /// <summary>A stable text for a memo key or a random stream name.</summary>
        public override string ToString() => IsNone ? "no enhancements" : $"{Count} x {ProgressPenaltyEach} progress penalty";
    }
}
