using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Harvesting;

namespace SWLOR.CraftSim.Tests.Harvesting
{
    [TestFixture]
    public class HarvestNodeCatalogTests
    {
        [Test]
        public void TheModuleHasElevenKindsOfNodeInFiveTiers()
        {
            var nodes = HarvestingFixture.Nodes;

            nodes.Select(node => node.Resref).Should().BeEquivalentTo(
                "veldite_vein", "tree", "scordspar_vein", "oak_tree", "plagionite_vein", "ancient_tree", "keromber_vein", "aracia_tree",
                "jasioclase_vein", "arkoxit_vein", "hyphae_tree");
            nodes.GroupBy(node => node.Tier).OrderBy(group => group.Key).Select(group => group.Count()).Should().Equal(2, 2, 2, 2, 3);
            nodes.Select(node => node.Tier).Distinct().OrderBy(tier => tier).Should().Equal(1, 2, 3, 4, 5);
        }

        [Test]
        public void EveryNodeHoldsOneGemExceptArkoxit_AndTheGemShareIsItsWeightOverTheTables()
        {
            var nodes = HarvestingFixture.Nodes;

            nodes.Where(node => node.Resref != "arkoxit_vein").Should().OnlyContain(node => node.HasGems && node.GemChance > 0 && node.GemChance < 0.2);
            var arkoxit = nodes.Single(node => node.Resref == "arkoxit_vein");
            arkoxit.HasGems.Should().BeFalse();
            arkoxit.GemChance.Should().Be(0);
            arkoxit.Tier.Should().Be(5);

            // HARVESTING_VELDITE: raw_veldite 50 and jade 5.
            nodes.Single(node => node.Resref == "veldite_vein").GemChance.Should().BeApproximately(5.0 / 55, 1e-9);
            // HARVESTING_JASIOCLASE: raw_jasioclase 50 and emerald 1.
            nodes.Single(node => node.Resref == "jasioclase_vein").GemChance.Should().BeApproximately(1.0 / 51, 1e-9);
        }

        [Test]
        public void ATiersGemShareIsTheShareOfItsNodesThatCanHoldAGemPocket()
        {
            var parity = HarvestingFixture.Parity();

            foreach (var tier in new[] { 1, 2, 3, 4 })
                parity.GemShare(tier).Should().Be(1.0);

            parity.GemShare(5).Should().BeApproximately(2.0 / 3, 1e-9, "arkoxit has no gem entries");
            parity.TodayGemsPerNode(1).Should().BeGreaterThan(parity.TodayGemsPerNode(5), "the higher the tier, the rarer its gem");
        }

        [Test]
        public void AFolderThatIsMissingIsRefused()
        {
            var act = () => HarvestNodeCatalog.Read(Path.Combine(Path.GetTempPath(), "no-such-folder-" + Guid.NewGuid()));

            act.Should().Throw<DirectoryNotFoundException>();
        }
    }
}
