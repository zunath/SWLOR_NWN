using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class AttackResumeTests
    {
        private const float BurstDamage = 100f;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void ASwingDueDuringACast_WaitsUntilAfterTheImpact()
        {
            var burst = TestCombatants.Use("ForceBurst1", AbilityTarget.Enemy, minIntervalSeconds: 600);
            burst.Damage = new DamageSpec { Min = BurstDamage, Max = BurstDamage, HitChance = 1f };
            var caster = TestCombatants.Medic("Caster", burst);
            caster.AutoAttack = new AutoAttackSpec { AttacksPerSecond = 4f, Damage = new DamageSpec { Min = 1f, Max = 1f, HitChance = 1f } };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { caster }, new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.PreTuning(),
                _catalog);
            var attacker = simulation.Get("Caster");
            var enemy = simulation.Get("Enemy");

            SimulationClock.RunUntil(simulation, () => attacker.Casting != null, 10, "Force Burst cast started");
            var damageBeforeCast = enemy.Stats.DamageTakenToHp;

            SimulationClock.RunUntil(simulation, () => attacker.Stats.AbilitiesUsed == 1, 10, "Force Burst landed");
            enemy.Stats.DamageTakenToHp.Should().Be(damageBeforeCast + BurstDamage,
                "no swing during the cast, and the game resumes the attack 0.1s after the impact, not with it");

            SimulationClock.RunUntil(simulation, simulation.Now + 0.3);
            enemy.Stats.DamageTakenToHp.Should().BeGreaterThan(damageBeforeCast + BurstDamage, "the attack resumes right after");
        }
    }
}
