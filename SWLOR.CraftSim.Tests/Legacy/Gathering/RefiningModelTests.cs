using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Legacy.Gathering;

namespace SWLOR.CraftSim.Tests.Legacy.Gathering
{
    [TestFixture]
    public class RefiningModelTests
    {
        [TestCase(0, 3)]
        [TestCase(1, 4)]
        [TestCase(6, 9)]
        public void ACoreRefinesThreePlusRefineryManagementUnits(int refineryManagement, int expected)
        {
            RefiningModel.ItemsPerCore(refineryManagement).Should().Be(expected);
        }

        [TestCase(0, 0, 0)]
        [TestCase(1, 0, 1)]
        [TestCase(3, 0, 1)]
        [TestCase(4, 0, 2)]
        [TestCase(10, 1, 3)]
        [TestCase(9, 6, 1)]
        [TestCase(100, 0, 34)]
        [TestCase(100, 6, 12)]
        public void CoresRequired_RoundsUp(int units, int refineryManagement, int expected)
        {
            RefiningModel.CoresRequired(units, refineryManagement).Should().Be(expected);
        }

        [Test]
        public void TheOresAreTheRefinerysAvailableTwelveWithFlatXpByTier()
        {
            var ores = RefiningModel.Ores();

            ores.Should().HaveCount(12);
            ores.Should().OnlyContain(o => o.RequiredLevel >= 1 && o.RequiredLevel <= 5);
            ores.Select(o => (o.Ore, o.XpPerUnit)).Should().Contain(new[]
            {
                ("raw_veldite", 25), ("ore_tilarium", 25), ("raw_scordspar", 50), ("ore_currian", 50), ("raw_plagionite", 75),
                ("ore_idailia", 75), ("raw_keromber", 100), ("ore_barinium", 100), ("raw_jasioclase", 125), ("ore_gostian", 125),
                ("raw_arkoxit", 150), ("ore_arda", 150)
            });
            ores.Should().NotContain(o => o.Ore.Contains("ochne") || o.Ore.Contains("croknor") || o.Ore.Contains("hemorgite") || o.Ore.Contains("bisteiss"));
            RefiningModel.Xp(ores.Single(o => o.Ore == "raw_veldite"), 9).Should().Be(225);
        }
    }
}
