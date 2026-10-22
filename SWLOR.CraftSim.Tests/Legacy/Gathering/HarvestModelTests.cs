using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Rng;

namespace SWLOR.CraftSim.Tests.Legacy.Gathering
{
    [TestFixture]
    public class HarvestModelTests
    {
        [TestCase(-1, 0.0)]
        [TestCase(0, 0.0)]
        [TestCase(1, 0.25)]
        [TestCase(2, 0.50)]
        [TestCase(3, 0.75)]
        [TestCase(4, 1.0)]
        [TestCase(5, 1.0)]
        public void BonusItemChance_IsTwentyFivePercentPerMightModifierPoint(int might, double expected)
        {
            HarvestModel.BonusItemChance(might).Should().BeApproximately(expected, 1e-9);
        }

        [Test]
        public void ANodeHoldsAD4OfHarvestsSoTwoAndAHalfOnAverage()
        {
            HarvestModel.ExpectedHarvestsPerNode.Should().Be(2.5);
            HarvestModel.ExpectedItemsPerNode(0).Should().Be(2.5);
            HarvestModel.ExpectedItemsPerNode(2).Should().BeApproximately(3.75, 1e-9);
            HarvestModel.ExpectedItemsPerNode(5).Should().BeApproximately(5.0, 1e-9);
        }

        [TestCase(1, 5)]
        [TestCase(2, 15)]
        [TestCase(5, 45)]
        public void VeinLevel_IsTenPerNodeLevelAboveTheFirstPlusFive(int nodeLevel, int expected)
        {
            HarvestModel.VeinLevel(nodeLevel).Should().Be(expected);
        }

        [TestCase(1, 0, 1050)]
        [TestCase(1, 5, 600)]
        [TestCase(1, 6, 450)]
        [TestCase(1, 9, 76)]
        [TestCase(1, 10, 0)]
        [TestCase(2, 0, 1200)]
        [TestCase(5, 40, 1050)]
        public void XpPerItem_FollowsTheServersXpChart(int nodeLevel, int rank, int expected)
        {
            HarvestModel.XpPerItem(nodeLevel, rank).Should().Be(expected);
        }

        [Test]
        public void ANodeHasBetweenOneAndFourHarvestsAndTheMightBonusNeverAddsHarvests()
        {
            var rng = new SimRandom(3);
            var seen = new HashSet<int>();
            for (var node = 0; node < 2000; node++)
            {
                var result = HarvestModel.HarvestNode(rng, 3, 20, 5);
                result.Harvests.Should().BeInRange(1, 4);
                result.Items.Should().Be(result.Harvests * 2, "a +5 Might modifier always adds the second item");
                seen.Add(result.Harvests);
            }

            seen.Should().BeEquivalentTo(new[] { 1, 2, 3, 4 });
        }

        [Test]
        public void ANodePaysXpForEachItemGathered()
        {
            var result = HarvestModel.HarvestNode(new SimRandom(1), 1, 0, 5);

            result.Xp.Should().Be(result.Items * HarvestModel.XpPerItem(1, 0));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        public void PlayedNodesMatchTheExpectedItems(int might)
        {
            var rng = new SimRandom(100 + might);
            const int Nodes = 40_000;
            var items = 0.0;
            var harvests = 0.0;
            for (var node = 0; node < Nodes; node++)
            {
                var result = HarvestModel.HarvestNode(rng, 2, 10, might);
                items += result.Items;
                harvests += result.Harvests;
            }

            (items / Nodes).Should().BeApproximately(HarvestModel.ExpectedItemsPerNode(might), 0.04);
            (harvests / Nodes).Should().BeApproximately(2.5, 0.03);
        }

        [Test]
        public void ExpectedXpPerNode_IsHarvestsTimesItemsTimesXpPerItem()
        {
            HarvestModel.ExpectedXpPerNode(1, 0, 2).Should().BeApproximately(2.5 * 1.5 * 1050, 1e-6);
        }
    }
}
