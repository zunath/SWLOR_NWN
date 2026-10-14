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

        [TestCase(true)]
        [TestCase(false)]
        public void Spawn_RejectsAQueuedAbilityWithNoAutoattackToCarryIt(bool hasAutoattack)
        {
            var strike = TestCombatants.Use("FortressStrike3", AbilityTarget.Enemy);
            strike.Damage = new DamageSpec { Min = StrikeDamage, Max = StrikeDamage };
            var fighter = TestCombatants.Medic("Fighter", strike);
            if (hasAutoattack)
                fighter.AutoAttack = new AutoAttackSpec { AttacksPerSecond = 0f, Damage = new DamageSpec { Min = 1f, Max = 1f } };
            var scenario = TestScenarios.TwoTeams(false, new[] { fighter }, new[] { TestCombatants.Dummy("Enemy", true) });

            var act = () => new Simulation(scenario, RuleSets.Current(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*FortressStrike3*autoattack*");
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
        public void ASwingDueWhenTheAbilityIsQueued_DoesNotCarryIt()
        {
            // A swing is due every step, so only the 0.1s resume delay keeps it from taking the ability at once.
            var simulation = Spawn(attacksPerSecond: 10f, strikeHitChance: 1f);
            var fighter = simulation.Get("Fighter");

            SimulationClock.RunUntil(simulation, () => fighter.Stats.AbilitiesUsed == 1, 10, "Fortress Strike queued");

            fighter.QueuedWeaponAbility.Should().NotBeNull("the game resumes the attack 0.1s after queueing a weapon ability");
        }

        [Test]
        public void AQueuedArea_LandsOnEveryRecipient_OnlyWhenTheSwingHits()
        {
            var strike = TestCombatants.Use("FortressStrike3", AbilityTarget.Enemy, minIntervalSeconds: 600);
            strike.Damage = new DamageSpec
            {
                Min = StrikeDamage, Max = StrikeDamage, HitChance = 1f, Targets = 3,
                HitChanceVs = new Dictionary<string, float> { ["Enemy 2"] = 0f, ["Enemy 3"] = 0f }
            };
            var fighter = TestCombatants.Striker("Fighter", 0.25f, SwingDamage);
            fighter.MaxFp = TestCombatants.EffectivelyUnlimited;
            fighter.MaxStm = TestCombatants.EffectivelyUnlimited;
            fighter.Abilities.Add(strike);
            var scenario = TestScenarios.TwoTeams(
                false, new[] { fighter }, Enumerable.Range(1, 3).Select(i => TestCombatants.Dummy($"Enemy {i}", true)));
            scenario.Teams[0].TargetPolicy = TargetPolicy.Focus;
            scenario.Teams[0].FocusTarget = "Enemy 1";
            var simulation = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog);
            var attacker = simulation.Get("Fighter");

            SimulationClock.RunUntil(simulation, () => attacker.Stats.AbilitiesUsed == 1, 10, "Fortress Strike queued");
            SimulationClock.RunUntil(simulation, () => attacker.QueuedWeaponAbility == null, 10, "the next swing");

            simulation.Get("Enemy 2").Stats.DamageTakenToHp.Should().Be(StrikeDamage,
                "the swing's hit carries the whole activation; secondary recipients take no hit roll of their own");
            simulation.Get("Enemy 3").Stats.DamageTakenToHp.Should().Be(StrikeDamage);
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
