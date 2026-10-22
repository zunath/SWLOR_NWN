namespace SWLOR.CraftSim.Legacy.Gathering
{
    /// <summary>The gathering baseline: every rule evaluated exactly, and the same rules played with a seed as a cross-check.</summary>
    public sealed class GatheringBaselineResults
    {
        public int Trials { get; init; }
        public int Seed { get; init; }
        public IReadOnlyList<HarvestRow> Harvest { get; init; }
        public IReadOnlyList<ScavengeRow> Scavenge { get; init; }
    }
}
