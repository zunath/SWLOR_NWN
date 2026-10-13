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
            RuleSets.Names.Should().Equal("current", "tuned", "tuned-pvp");
        }

        [TestCase("current")]
        [TestCase("TUNED")]
        [TestCase("Tuned-PVP")]
        public void Get_FindsPresetsRegardlessOfCase(string name)
        {
            RuleSets.Get(name).Name.Should().Be(name.ToLowerInvariant());
        }

        [Test]
        public void Get_WithAnUnknownName_ListsTheKnownPresets()
        {
            var act = () => RuleSets.Get("nonexistent");

            act.Should().Throw<ArgumentException>().WithMessage("*nonexistent*current*tuned*tuned-pvp*");
        }

        [Test]
        public void Get_ReturnsAFreshInstanceEachTime()
        {
            var first = RuleSets.Get("tuned");
            first.RecastOverrides["Benevolence"] = 99f;

            RuleSets.Get("tuned").RecastOverrides["Benevolence"].Should().Be(10f);
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
        public void Tuned_ContainsThePveHealingCostAndFixChanges()
        {
            var rules = RuleSets.Tuned();

            rules.ForceAffinityHealingCap.Should().Be(1.25f);
            rules.RecastOverrides.Should().Contain("Benevolence", 10f).And.Contain("MedKit", 10.5f);
            rules.TemporaryHpCapPercent.Should().Be(50f);
            rules.ConvergenceDurationSeconds.Should().Be(20f);
            rules.HarmonyInstantHealsOnly.Should().BeTrue();
            rules.HarmonyCooldownSeconds.Should().Be(6f);
            rules.FpCostMultiplier.Should().Be(1.2f);
            rules.StmCostMultiplier.Should().Be(1f);
            rules.CostSkillThreshold.Should().Be(28);
            rules.Revives.Should().Be(ReviveRule.BlockedInPvpLimitedInPve);
        }

        [Test]
        public void Tuned_HasNoPvpHealingOrDamageRules()
        {
            var rules = RuleSets.Tuned();

            rules.PvpHealingMultiplier.Should().Be(1f);
            rules.PvpTemporaryHpMultiplier.Should().Be(1f);
            rules.PvpHealingRamp.Should().BeEmpty();
            rules.PvpDamageBackstop.Should().BeEmpty();
        }

        [Test]
        public void TunedPvp_IsTunedPlusThePvpRules()
        {
            var tuned = RuleSets.Tuned();
            var tunedPvp = RuleSets.TunedPvp();

            tunedPvp.ForceAffinityHealingCap.Should().Be(tuned.ForceAffinityHealingCap);
            tunedPvp.TemporaryHpCapPercent.Should().Be(tuned.TemporaryHpCapPercent);
            tunedPvp.ConvergenceDurationSeconds.Should().Be(tuned.ConvergenceDurationSeconds);
            tunedPvp.FpCostMultiplier.Should().Be(tuned.FpCostMultiplier);
            tunedPvp.Revives.Should().Be(tuned.Revives);

            tunedPvp.PvpHealingMultiplier.Should().Be(1f);
            tunedPvp.PvpTemporaryHpMultiplier.Should().Be(1f);
            tunedPvp.PvpHealingRamp.Select(s => (s.AtSeconds, s.Percent))
                .Should().Equal((120f, 10f), (150f, 20f), (180f, 30f), (210f, 45f), (240f, 60f));
            tunedPvp.PvpDamageBackstop.Select(s => (s.AtSeconds, s.Percent))
                .Should().Equal((180f, 20f), (210f, 40f), (240f, 60f), (270f, 80f), (300f, 100f));
        }
    }
}
