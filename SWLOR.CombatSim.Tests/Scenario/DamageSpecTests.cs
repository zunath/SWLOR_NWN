using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Scenario;

namespace SWLOR.CombatSim.Tests.Scenario
{
    [TestFixture]
    public class DamageSpecTests
    {
        [Test]
        public void GetHitChance_UsesTheDefaultWhenNoTargetSpecificChanceExists()
        {
            var damage = new DamageSpec { HitChance = 0.4f };

            damage.GetHitChance("Anyone").Should().Be(0.4f);
        }

        [Test]
        public void GetHitChance_PrefersAChanceSetForTheNamedTarget()
        {
            var damage = new DamageSpec
            {
                HitChance = 0.4f,
                HitChanceVs = new Dictionary<string, float> { ["Tank"] = 0.1f }
            };

            damage.GetHitChance("Tank").Should().Be(0.1f);
            damage.GetHitChance("Medic").Should().Be(0.4f);
        }

        [Test]
        public void GetHitChance_ToleratesANullTargetTable()
        {
            var damage = new DamageSpec { HitChance = 0.7f, HitChanceVs = null };

            damage.GetHitChance("Tank").Should().Be(0.7f);
        }

        [Test]
        public void GetProblems_RejectsHitsBeyondTheBound()
        {
            new DamageSpec { Min = 1f, Max = 1f, Hits = DamageSpec.MaximumHits + 1 }.GetProblems().Should().ContainSingle(p => p.Contains("Hits"));
            new DamageSpec { Min = 1f, Max = 1f, Hits = DamageSpec.MaximumHits }.GetProblems().Should().BeEmpty();
        }

        [Test]
        public void Defaults_AlwaysHitOnceWithoutCrits()
        {
            var damage = new DamageSpec();

            damage.HitChance.Should().Be(1f);
            damage.Hits.Should().Be(1);
            damage.CritChance.Should().Be(0f);
            damage.CritMultiplier.Should().Be(1.5f);
            damage.Area.Should().BeFalse();
        }
    }
}
