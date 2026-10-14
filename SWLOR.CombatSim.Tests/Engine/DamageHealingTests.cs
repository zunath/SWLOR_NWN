using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class DamageHealingTests
    {
        private const int CasterMaxHp = 1000;
        private const int EnemyMaxHp = 10_000;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [TestCase(100, 40.0)]
        [TestCase(40, 50.0)]
        public void ForceDrain_HealsItsCasterForAShareOfTheDamage_MoreAgainstALowTarget(int enemyHpPercent, double expectedHeal)
        {
            var simulation = Spawn(100f);
            simulation.Get("Enemy").Hp = EnemyMaxHp * enemyHpPercent / 100.0;
            var caster = simulation.Get("Caster");

            CastOnce(simulation, caster);

            caster.Stats.HealingReceived.Should().BeApproximately(expectedHeal, 1e-6);
        }

        [Test]
        public void ForceDrain_HealsAtMostFifteenPercentOfMaxHpPerCast()
        {
            var simulation = Spawn(1000f);
            var caster = simulation.Get("Caster");

            CastOnce(simulation, caster);

            caster.Stats.HealingReceived.Should().BeApproximately(CasterMaxHp * 0.15, 1e-6);
        }

        [Test]
        public void ForceDrain_HealsNothingWhenItMisses()
        {
            var simulation = Spawn(100f, hitChance: 0f);
            var caster = simulation.Get("Caster");

            CastOnce(simulation, caster);

            caster.Stats.HealingReceived.Should().Be(0);
        }

        private Simulation Spawn(float damage, float hitChance = 1f)
        {
            var drain = TestCombatants.Use("ForceDrain3", AbilityTarget.Enemy, minIntervalSeconds: 600);
            drain.Damage = new DamageSpec { Min = damage, Max = damage, HitChance = hitChance, Type = DamageType.Force };
            var caster = TestCombatants.Medic("Caster", drain);
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { caster }, new[] { new CombatantSpec { Name = "Enemy", MaxHp = EnemyMaxHp, IsNpc = true } }),
                RuleSets.Current(),
                _catalog);
            simulation.Get("Caster").Hp = CasterMaxHp / 2.0;
            return simulation;
        }

        private static void CastOnce(Simulation simulation, Combatant caster) =>
            SimulationClock.RunUntil(simulation, () => caster.Stats.AbilitiesUsed == 1, 10, "Force Drain resolved");
    }
}
