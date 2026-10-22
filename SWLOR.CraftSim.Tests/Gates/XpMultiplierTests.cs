using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Tests.Support;
using static SWLOR.CraftSim.Tests.Support.BenchHarness;

namespace SWLOR.CraftSim.Tests.Gates
{
    [TestFixture]
    public class XpMultiplierTests
    {
        private static double NewXp(double multiplier)
        {
            var scenario = new BenchScenario { XpMultiplier = multiplier };
            var context = Context(new[] { 25 }, runs: 10, perLevel: 1);
            var suite = Suite(scenario);
            var xpCase = suite.XpCohort.Cases(context).First();

            return suite.XpCohort.PlayReplacement(xpCase, context).TotalXp;
        }

        [Test]
        public void AScenariosXpMultiplierScalesTheNewLoopsXpOnly()
        {
            var plain = NewXp(1.0);
            var doubled = NewXp(2.0);

            doubled.Should().BeApproximately(plain * 2, plain * 0.01, "every craft's base XP is doubled before the bonuses");
        }

        [Test]
        public void TheReportNamesTheMultiplierWhenThereIsOne()
        {
            var context = Context(new[] { 25 }, runs: 10, perLevel: 1);

            var plain = Suite(new BenchScenario()).Inputs(context);
            var scaled = Suite(new BenchScenario { XpMultiplier = 3.0 }).Inputs(context);

            plain.Should().NotContain(line => line.Contains("XP multiplier"));
            scaled.Should().Contain(line => line.Contains("XP multiplier") && line.Contains("3"));
        }
    }
}
