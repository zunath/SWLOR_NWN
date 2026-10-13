using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Rules;

namespace SWLOR.CombatSim.Tests.Rules
{
    [TestFixture]
    public class RuleSetRampTests
    {
        [TestCase(130, 10f)]
        [TestCase(250, 60f)]
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
        [TestCase(150, 20f)]
        [TestCase(180, 30f)]
        [TestCase(210, 45f)]
        [TestCase(239.9, 45f)]
        [TestCase(240, 60f)]
        [TestCase(3600, 60f)]
        public void GetStepPercent_UsesTheLatestStepReached_ForTheTunedPvpHealingRamp(double seconds, float expected)
        {
            RuleSet.GetStepPercent(RuleSets.TunedPvp().PvpHealingRamp, seconds).Should().Be(expected);
        }

        [TestCase(179.9, 0f)]
        [TestCase(180, 20f)]
        [TestCase(210, 40f)]
        [TestCase(240, 60f)]
        [TestCase(270, 80f)]
        [TestCase(300, 100f)]
        [TestCase(900, 100f)]
        public void GetStepPercent_UsesTheLatestStepReached_ForTheTunedPvpDamageBackstop(double seconds, float expected)
        {
            RuleSet.GetStepPercent(RuleSets.TunedPvp().PvpDamageBackstop, seconds).Should().Be(expected);
        }
    }
}
