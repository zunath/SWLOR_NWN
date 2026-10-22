using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Scenario;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>What every gate is given: the game data, the run size and seed, and today's baseline to compare against.</summary>
    public sealed class GateContext
    {
        public GateContext(
            RecipeCatalog recipes,
            CraftingGearCatalog gear,
            int runs,
            int seed,
            AttributeSweep attributes,
            CraftingBaselineResults craftingBaseline = null,
            GatheringBaselineResults gatheringBaseline = null)
        {
            Recipes = recipes;
            Gear = gear;
            Runs = runs;
            Seed = seed;
            Attributes = attributes;
            CraftingBaseline = craftingBaseline;
            GatheringBaseline = gatheringBaseline;
        }

        public RecipeCatalog Recipes { get; }
        public CraftingGearCatalog Gear { get; }
        public int Runs { get; }
        public int Seed { get; }
        public AttributeSweep Attributes { get; }

        /// <summary>Today's old-minigame results, for the economy-parity gate; null when not played.</summary>
        public CraftingBaselineResults CraftingBaseline { get; }

        /// <summary>Today's gathering, refining and fishing results; null when not played.</summary>
        public GatheringBaselineResults GatheringBaseline { get; }
    }
}
