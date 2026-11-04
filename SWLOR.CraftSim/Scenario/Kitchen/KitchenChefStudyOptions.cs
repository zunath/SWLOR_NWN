namespace SWLOR.CraftSim.Scenario.Kitchen
{
    /// <summary>What the Chef study plays and how.</summary>
    public sealed class KitchenChefStudyOptions
    {
        /// <summary>Attempts per recipe, rank, gear tier and build.</summary>
        public int Runs { get; init; } = 40;

        public int Seed { get; init; } = 1;

        /// <summary>The recipes sampled from each at-level band.</summary>
        public int RecipesPerBand { get; init; } = 6;

        /// <summary>Plays only the styles' magnitudes table (the quick way to try a retune).</summary>
        public bool StylesOnly { get; init; }

        /// <summary>Plays the styles' magnitudes at mid gear only, which is quicker still.</summary>
        public bool MidGearOnly { get; init; }

        /// <summary>The style magnitudes to try; null tries the numbers the perk definitions give.</summary>
        public IReadOnlyList<KitchenChefStudy.StyleCandidate> Candidates { get; init; }
    }
}
