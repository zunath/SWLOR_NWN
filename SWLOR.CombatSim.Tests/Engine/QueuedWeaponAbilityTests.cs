using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class QueuedWeaponAbilityTests
    {
        private const float StrikeDamage = 100f;
        private const float SwingDamage = 10f;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void FortressStrike_IsAQueuedWeaponAbilityInTheGameData()
        {
            _catalog.Get(FeatType.FortressStrike3).IsQueuedWeapon.Should().BeTrue();
        }

        [Test]
        public void AQueuedAbility_LandsOnTheNextLandedSwing_InPlaceOfThatSwing()
        {
            var simulation = Spawn(attacksPerSecond: 0.25f, strikeHitChance: 1f);
            var fighter = simulation.Get("Fighter");
            var enemy = simulation.Get("Enemy");

            SimulationClock.RunUntil(simulation, () => fighter.Stats.AbilitiesUsed == 1, 10, "Fortress Strike queued");
            fighter.QueuedWeaponAbility.Should().NotBeNull("it waits for the next swing");
            var before = enemy.Stats.DamageTakenToHp;

            SimulationClock.RunUntil(simulation, () => fighter.QueuedWeaponAbility == null, 10, "the next swing");

            (enemy.Stats.DamageTakenToHp - before).Should().Be(StrikeDamage);
        }

        [Test]
        public void AQueuedAbility_KeepsWaitingThroughMisses_AndWearsOffAfterThirtySeconds()
        {
            var simulation = Spawn(attacksPerSecond: 1f, strikeHitChance: 0f);
            var fighter = simulation.Get("Fighter");
            var enemy = simulation.Get("Enemy");

            SimulationClock.RunUntil(simulation, () => fighter.Stats.AbilitiesUsed == 1, 10, "Fortress Strike queued");
            var queuedAt = simulation.Now;
            var before = enemy.Stats.DamageTakenToHp;

            SimulationClock.RunUntil(simulation, queuedAt + 29);
            fighter.QueuedWeaponAbility.Should().NotBeNull("missed swings leave it queued");
            enemy.Stats.DamageTakenToHp.Should().Be(before, "the queued ability's swings missed");

            SimulationClock.RunUntil(simulation, queuedAt + 31);
            fighter.QueuedWeaponAbility.Should().BeNull("it wears off 30 seconds after it was queued");
        }

        private Simulation Spawn(float attacksPerSecond, float strikeHitChance)
        {
            var strike = TestCombatants.Use("FortressStrike3", AbilityTarget.Enemy, minIntervalSeconds: 600);
            strike.Damage = new DamageSpec { Min = StrikeDamage, Max = StrikeDamage, HitChance = strikeHitChance };
            var fighter = TestCombatants.Striker("Fighter", attacksPerSecond, SwingDamage);
            fighter.MaxFp = TestCombatants.EffectivelyUnlimited;
            fighter.MaxStm = TestCombatants.EffectivelyUnlimited;
            fighter.Abilities.Add(strike);
            return TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { fighter }, new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog);
        }
    }
}
