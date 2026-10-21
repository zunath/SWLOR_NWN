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

        [TestCase("damage")] [TestCase("accuracy")] [TestCase("chance")]
        [TestCase("target chance")] [TestCase("crit")] [TestCase("type")]
        public void WeaponPower_RejectsAmbiguousCalibratedOverrides(string field)
        {
            var damage = new DamageSpec { WeaponPower = new() { FlatDamage = 6 } };
            damage.GetProblems().Should().BeEmpty();
            switch (field)
            {
                case "damage": damage.Min = damage.Max = 1; break;
                case "accuracy": damage.Accuracy = 100; break;
                case "chance": damage.HitChance = .5f; break;
                case "target chance": damage.HitChanceVs["enemy"] = .5f; break;
                case "crit": damage.CritChance = .5f; break;
                case "type": damage.Type = DamageType.Force; break;
            }
            damage.GetProblems().Should().ContainSingle(p => p.Contains("do not combine"));
        }
    }
}
