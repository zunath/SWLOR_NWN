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

        private static readonly Lazy<HarvestingParity> ShippedValue = new(() =>
        {
            var parity = new HarvestingParity(20000, 1, Nodes, new HarvestingParityOptions { Ranks = new[] { 0, 25, 50 } });
            parity.Prepare();
            return parity;
        });

        /// <summary>
        /// The study the shipped report plays (20000 nodes a case, seed 1) at a few ranks, played once for every test that checks a gate
        /// against its caps. The caps are close enough that a smaller run only shows the noise of a plan's mean. A case takes its dice
        /// from its own stream of the seed, so these cases play the same nodes the report's do.
        /// </summary>
        public static HarvestingParity Shipped => ShippedValue.Value;

        /// <summary>A study of a few ranks and a few hundred nodes a case, small enough for a test and the same on every machine.</summary>
        public static HarvestingParity Parity(int runs = 600, int seed = 1, HarvestingParityOptions options = null) =>
            new(runs, seed, Nodes, options ?? new HarvestingParityOptions { Ranks = new[] { 0, 25, 50 } });
    }
}
