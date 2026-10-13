using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Rules;

namespace SWLOR.CombatSim.Tests.Rules
{
    [TestFixture]
    public class RuleSetsTests
    {
        [Test]
        public void Names_ListTheThreePresets()
        {
            RuleSets.Names.Should().Equal("current", "wave1", "wave2");
        }

        [TestCase("current")]
        [TestCase("WAVE1")]
        [TestCase("Wave2")]
        public void Get_FindsPresetsRegardlessOfCase(string name)
        {
            RuleSets.Get(name).Name.Should().Be(name.ToLowerInvariant());
        }

        [Test]
        public void Get_WithAnUnknownName_ListsTheKnownPresets()
        {
            var act = () => RuleSets.Get("wave9");

            act.Should().Throw<ArgumentException>().WithMessage("*wave9*current*wave1*wave2*");
        }

        [Test]
        public void Get_ReturnsAFreshInstanceEachTime()
        {
            var first = RuleSets.Get("wave1");
            first.RecastOverrides["Benevolence"] = 99f;

            RuleSets.Get("wave1").RecastOverrides["Benevolence"].Should().Be(10f);
        }

        [Test]
        public void Current_MatchesTheLiveRules()
        {
            var rules = RuleSets.Current();

            rules.ForceAffinityHealingCap.Should().Be(1.5f);
            rules.RecastOverrides.Should().BeEmpty();
            rules.TemporaryHpCapPercent.Should().BeNull();
            rules.ConvergenceDurationSeconds.Should().Be(30f);
            rules.HarmonyInstantHealsOnly.Should().BeFalse();
            rules.HarmonyCooldownSeconds.Should().Be(0f);
            rules.FpCostMultiplier.Should().Be(1f);
            rules.StmCostMultiplier.Should().Be(1f);
            rules.Revives.Should().Be(ReviveRule.Unrestricted);
            rules.PvpHealingMultiplier.Should().Be(1f);
            rules.PvpHealingRamp.Should().BeEmpty();
            rules.PvpDamageBackstop.Should().BeEmpty();
        }

        [Test]
        public void Wave1_ContainsThePveHealingCostAndFixChanges()
        {
            var rules = RuleSets.Wave1();

            rules.ForceAffinityHealingCap.Should().Be(1.25f);
            rules.RecastOverrides.Should().Contain("Benevolence", 10f).And.Contain("MedKit", 10.5f);
            rules.TemporaryHpCapPercent.Should().Be(50f);
            rules.ConvergenceDurationSeconds.Should().Be(20f);
            rules.HarmonyInstantHealsOnly.Should().BeTrue();
            rules.HarmonyCooldownSeconds.Should().Be(6f);
            rules.FpCostMultiplier.Should().Be(1.2f);
            rules.StmCostMultiplier.Should().Be(1.2f);
            rules.CostSkillThreshold.Should().Be(28);
            rules.Revives.Should().Be(ReviveRule.BlockedInPvpLimitedInPve);
        }

        [Test]
        public void Wave1_HasNoPvpHealingOrDamageRules()
        {
            var rules = RuleSets.Wave1();

            rules.PvpHealingMultiplier.Should().Be(1f);
            rules.PvpTemporaryHpMultiplier.Should().Be(1f);
            rules.PvpHealingRamp.Should().BeEmpty();
            rules.PvpDamageBackstop.Should().BeEmpty();
        }

        [Test]
        public void Wave2_IsWave1PlusThePvpRules()
        {
            var wave1 = RuleSets.Wave1();
            var wave2 = RuleSets.Wave2();

            wave2.ForceAffinityHealingCap.Should().Be(wave1.ForceAffinityHealingCap);
            wave2.TemporaryHpCapPercent.Should().Be(wave1.TemporaryHpCapPercent);
            wave2.ConvergenceDurationSeconds.Should().Be(wave1.ConvergenceDurationSeconds);
            wave2.FpCostMultiplier.Should().Be(wave1.FpCostMultiplier);
            wave2.Revives.Should().Be(wave1.Revives);

            wave2.PvpHealingMultiplier.Should().Be(0.7f);
            wave2.PvpTemporaryHpMultiplier.Should().Be(0.7f);
            wave2.PvpHealingRamp.Select(s => (s.AtSeconds, s.Percent))
                .Should().Equal((120f, 10f), (150f, 30f), (180f, 50f), (210f, 70f), (240f, 90f));
            wave2.PvpDamageBackstop.Select(s => (s.AtSeconds, s.Percent))
                .Should().Equal((240f, 10f), (270f, 20f), (300f, 30f), (330f, 40f), (360f, 50f));
        }
    }
}
