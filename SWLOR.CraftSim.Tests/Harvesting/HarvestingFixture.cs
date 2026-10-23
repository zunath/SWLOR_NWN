using SWLOR.CraftSim.Harvesting;
using SWLOR.CraftSim.Tests.Support;

namespace SWLOR.CraftSim.Tests.Harvesting
{
    /// <summary>The module's nodes and a small study of them, built once for the harvesting tests.</summary>
    internal static class HarvestingFixture
    {
        private static readonly Lazy<IReadOnlyList<HarvestNodeType>> NodesValue =
            new(() => HarvestNodeCatalog.Read(System.IO.Path.Combine(SharedData.Root, "Module", "utp")));

        public static IReadOnlyList<HarvestNodeType> Nodes => NodesValue.Value;

        /// <summary>A study of a few ranks and a few hundred nodes a case, small enough for a test and the same on every machine.</summary>
        public static HarvestingParity Parity(int runs = 600, int seed = 1, HarvestingParityOptions options = null) =>
            new(runs, seed, Nodes, options ?? new HarvestingParityOptions { Ranks = new[] { 0, 25, 50 } });
    }
}
