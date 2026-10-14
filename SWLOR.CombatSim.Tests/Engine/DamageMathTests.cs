using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Engine;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class DamageMathTests
    {
        [TestCase(101, 15, 86)]
        [TestCase(100, 25, 75)]
        [TestCase(1, 50, 1)]
        [TestCase(100, 99, 5)]
        public void ApplyPercentCut_SubtractsTheRoundedUpShare(double damage, double cut, double expected)
        {
            DamageMath.ApplyPercentCut(damage, cut).Should().Be(expected);
        }

        [TestCase(101, 15, 86)]
        [TestCase(3, 50, 2)]
        [TestCase(100, 100, 15)]
        [TestCase(100, 0, 100)]
        public void ApplyGenericCut_RoundsTheResultUp_AndStopsAtEightyFivePercent(double damage, double cut, double expected)
        {
            DamageMath.ApplyGenericCut(damage, cut).Should().Be(expected);
        }

        [Test]
        public void ApplyGenericCut_HoldsTheCombinedCapAcrossAnEarlierStatusCut()
        {
            // 1,000 Force damage: Force Sanctuary's 5% leaves 950, and a 95% generic cut may only bring the total to 15%.
            var afterStatus = DamageMath.ApplyPercentCut(1000, 5);

            DamageMath.ApplyGenericCut(afterStatus, 95, DamageMath.CutMultiplier(5)).Should().Be(150);
        }
    }
}
