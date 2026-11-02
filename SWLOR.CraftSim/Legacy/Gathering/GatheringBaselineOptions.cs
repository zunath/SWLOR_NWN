using SWLOR.CraftSim.Scenario;

namespace SWLOR.CraftSim.Legacy.Gathering
{
    public sealed class GatheringBaselineOptions
    {
        public int Trials { get; init; } = 20_000;
        public int Seed { get; init; } = 1;
        public AttributeSweep Attributes { get; init; } = AttributeSweep.Default;
    }
}
