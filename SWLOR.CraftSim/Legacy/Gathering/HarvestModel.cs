using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Rng;

namespace SWLOR.CraftSim.Legacy.Gathering
{
    /// <summary>
    /// Harvesting with a harvester as it was before Prospecting (<c>HarvesterItemDefinition.cs</c> at commit 64b712fc8): d4 harvests per node,
    /// one item each, plus a Might bonus item. The d4 pool and the XP per item are still in the game (<c>ProspectingNode</c>,
    /// <c>GatheringLevels</c>), and Quick Harvest plays exactly this loop at a Might modifier of 0. The Might bonus item was removed with the
    /// attribute dependency and is no longer checked against the source. Items are counted, not priced.
    /// </summary>
    public static class HarvestModel
    {
        public const int HarvestsDie = 4;
        public const int FirstNodeLevel = 1;
        public const int LastNodeLevel = 5;

        /// <summary>Harvests per node: the first harvest rolls a d4 and the node lasts that many (ProspectingNode.cs:67 rolls the pool now).</summary>
        public static double ExpectedHarvestsPerNode => (HarvestsDie + 1) / 2.0;

        /// <summary>The chance of the free second item: d100 &lt;= 5 x Might modifier x 5 (the removed bonus item of <c>HarvesterItemDefinition.cs</c>).</summary>
        public static double BonusItemChance(int mightModifier) => Math.Clamp(5 * mightModifier * 5, 0, 100) / 100.0;

        public static double ExpectedItemsPerHarvest(int mightModifier) => 1 + BonusItemChance(mightModifier);

        public static double ExpectedItemsPerNode(int mightModifier) => ExpectedHarvestsPerNode * ExpectedItemsPerHarvest(mightModifier);

        /// <summary>The level XP is measured at: 10 x (node level - 1) + 5 (GatheringLevels.cs:20).</summary>
        public static int VeinLevel(int nodeLevel) => 10 * (nodeLevel - 1) + 5;

        /// <summary>XP for each item gathered, from the server's own XP chart (GatheringLevels.cs:23).</summary>
        public static int XpPerItem(int nodeLevel, int gatheringRank) => ServerXp.ForDelta(VeinLevel(nodeLevel) - gatheringRank);

        public static double ExpectedXpPerNode(int nodeLevel, int gatheringRank, int mightModifier) =>
            ExpectedHarvestsPerNode * ExpectedItemsPerHarvest(mightModifier) * XpPerItem(nodeLevel, gatheringRank);

        public static HarvestNodeResult HarvestNode(SimRandom rng, int nodeLevel, int gatheringRank, int mightModifier)
        {
            var remaining = rng.D4();
            var harvests = 0;
            var items = 0;
            var xp = 0;
            var xpPerItem = XpPerItem(nodeLevel, gatheringRank);
            while (remaining > 0)
            {
                harvests++;
                remaining--;
                var gathered = 1;
                if (rng.D100() <= 5 * mightModifier * 5)
                    gathered++;

                items += gathered;
                xp += xpPerItem * gathered;
            }

            return new HarvestNodeResult(harvests, items, xp);
        }
    }
}
