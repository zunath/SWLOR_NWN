using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class DelayedImpactTests
    {
        private const float LeapDamage = 50f;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void Catalog_ReadsForceLeapsImpactDelay()
        {
            _catalog.Get(FeatType.ForceLeap1).ImpactDelaySeconds.Should().BeGreaterThan(0f);
        }

        [Test]
        public void ADelayedForceImpact_OpensConvergenceWhenItLands()
        {
            var leap = TestCombatants.Use("ForceLeap1", AbilityTarget.Enemy, minIntervalSeconds: 600);
            leap.Damage = new DamageSpec { Min = LeapDamage, Max = LeapDamage, HitChance = 1f };
            var leaper = TestCombatants.Medic("Leaper", leap);
            leaper.Perks = new List<string> { nameof(PerkType.ForceConvergence) };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { leaper }, new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.PreTuning(),
                _catalog);
            var caster = simulation.Get("Leaper");
            var delay = _catalog.Get(FeatType.ForceLeap1).ImpactDelaySeconds;

            SimulationClock.RunUntil(simulation, () => caster.Stats.AbilitiesUsed == 1, 10, "Force Leap activated");
            caster.ConvergenceAccuracyUntil.Should().BeLessThan(0, "Convergence opens with the impact, not the payment");

            SimulationClock.RunUntil(simulation, () => caster.Casting == null, delay + 1, "Force Leap landed");
            caster.ConvergenceAccuracyUntil.Should().BeGreaterThan(0, "the landed leap opens Convergence");
        }

        [Test]
        public void ADelayedImpact_IsPaidAtActivation_AndLandsAfterItsDelayWithoutSwingsInBetween()
        {
            var leap = TestCombatants.Use("ForceLeap1", AbilityTarget.Enemy, minIntervalSeconds: 600);
            leap.Damage = new DamageSpec { Min = LeapDamage, Max = LeapDamage, HitChance = 1f };
            var leaper = TestCombatants.Medic("Leaper", leap);
            leaper.AutoAttack = new AutoAttackSpec { AttacksPerSecond = 4f, Damage = new DamageSpec { Min = 1f, Max = 1f, HitChance = 1f } };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { leaper }, new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.PreTuning(),
                _catalog);
            var caster = simulation.Get("Leaper");
            var enemy = simulation.Get("Enemy");
            var delay = _catalog.Get(FeatType.ForceLeap1).ImpactDelaySeconds;

            SimulationClock.RunUntil(simulation, () => caster.Stats.AbilitiesUsed == 1, 10, "Force Leap activated");
            var activatedAt = simulation.Now;
            var damageAtActivation = enemy.Stats.DamageTakenToHp;

            caster.Stats.FpSpent.Should().BeGreaterThan(0, "the game pays for the leap when it activates");
            caster.Casting.Should().NotBeNull("the leaper is busy until the leap lands");

            SimulationClock.RunUntil(simulation, activatedAt + delay - 0.15);
            enemy.Stats.DamageTakenToHp.Should().Be(damageAtActivation, "no impact and no swings while the leap is in the air");

            SimulationClock.RunUntil(simulation, activatedAt + delay + 0.15);
            enemy.Stats.DamageTakenToHp.Should().BeGreaterThanOrEqualTo(damageAtActivation + LeapDamage);
            caster.Casting.Should().BeNull();
        }
    }
}
