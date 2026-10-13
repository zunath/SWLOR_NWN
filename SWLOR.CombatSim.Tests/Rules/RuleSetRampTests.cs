using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Rules;

namespace SWLOR.CombatSim.Tests.Rules
{
    [TestFixture]
    public class RuleSetRampTests
    {
        [TestCase(130, 10f)]
        [TestCase(250, 90f)]
        public void GetStepPercent_UsesTheHighestStepReached_WhateverOrderTheStepsAreListedIn(double seconds, float expected)
        {
            var reversed = RuleSets.TunedPvp().PvpHealingRamp.AsEnumerable().Reverse().ToList();

            RuleSet.GetStepPercent(reversed, seconds).Should().Be(expected);
        }

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
        public void GetStepPercent_UsesTheLatestStepReached_ForTheTunedPvpHealingRamp(double seconds, float expected)
        {
            RuleSet.GetStepPercent(RuleSets.TunedPvp().PvpHealingRamp, seconds).Should().Be(expected);
        }

        [TestCase(239.9, 0f)]
        [TestCase(240, 10f)]
        [TestCase(270, 20f)]
        [TestCase(300, 30f)]
        [TestCase(330, 40f)]
        [TestCase(360, 50f)]
        [TestCase(900, 50f)]
        public void GetStepPercent_UsesTheLatestStepReached_ForTheTunedPvpDamageBackstop(double seconds, float expected)
        {
            RuleSet.GetStepPercent(RuleSets.TunedPvp().PvpDamageBackstop, seconds).Should().Be(expected);
        }
    }
}
