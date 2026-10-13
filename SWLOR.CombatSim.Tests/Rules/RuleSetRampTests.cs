using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Rules;

namespace SWLOR.CombatSim.Tests.Rules
{
    [TestFixture]
    public class RuleSetRampTests
    {
        [Test]
        public void GetStepPercent_WithNoSteps_IsZero()
        {
            RuleSet.GetStepPercent(new List<RampStep>(), 500).Should().Be(0f);
        }

        [TestCase(0, 0f)]
        [TestCase(119.9, 0f)]
        [TestCase(120, 10f)]
        [TestCase(149.9, 10f)]
        [TestCase(150, 30f)]
        [TestCase(180, 50f)]
        [TestCase(210, 70f)]
        [TestCase(239.9, 70f)]
        [TestCase(240, 90f)]
        [TestCase(3600, 90f)]
        public void GetStepPercent_UsesTheLatestStepReached_ForTheWave2HealingRamp(double seconds, float expected)
        {
            RuleSet.GetStepPercent(RuleSets.Wave2().PvpHealingRamp, seconds).Should().Be(expected);
        }

        [TestCase(239.9, 0f)]
        [TestCase(240, 10f)]
        [TestCase(270, 20f)]
        [TestCase(300, 30f)]
        [TestCase(330, 40f)]
        [TestCase(360, 50f)]
        [TestCase(900, 50f)]
        public void GetStepPercent_UsesTheLatestStepReached_ForTheWave2DamageBackstop(double seconds, float expected)
        {
            RuleSet.GetStepPercent(RuleSets.Wave2().PvpDamageBackstop, seconds).Should().Be(expected);
        }
    }
}
