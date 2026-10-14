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
    }
}
