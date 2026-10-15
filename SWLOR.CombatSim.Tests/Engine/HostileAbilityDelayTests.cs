using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class HostileAbilityDelayTests
    {
        private const float TouchDamage = 1f;
        private const float PushDamage = 1000f;
        private const double LimitSeconds = 5.0;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void Current_MakesASecondHostileAbilityWaitForTheDelay()
        {
            var (simulation, _, enemy) = Spawn(RuleSets.Current(), Hostile("ForceTouch1", TouchDamage), Hostile("ForcePush1", PushDamage));

            var (firstAt, secondAt) = HostileHitTimes(simulation, enemy);

            (secondAt - firstAt).Should().BeGreaterThanOrEqualTo(HostileAbilityDelay.Seconds - 1e-6,
                "the caster is ready to act every 0.1 seconds, so only the shared delay holds the second ability back");
        }

        [Test]
        public void PreTuning_HasNoDelayBetweenHostileAbilities()
        {
            var (simulation, _, enemy) = Spawn(RuleSets.PreTuning(), Hostile("ForceTouch1", TouchDamage), Hostile("ForcePush1", PushDamage));

            var (firstAt, secondAt) = HostileHitTimes(simulation, enemy);

            (secondAt - firstAt).Should().BeLessThan(HostileAbilityDelay.Seconds);
        }

        [Test]
        public void Current_DoesNotDelayAnAbilityThatIsNotHostile()
        {
            var (simulation, caster, enemy) = Spawn(RuleSets.Current(), Hostile("ForceTouch1", TouchDamage), TestCombatants.Use("EmergencyTriage1", AbilityTarget.Self));

            SimulationClock.RunUntil(simulation, () => enemy.Hp < TestCombatants.EffectivelyUnlimited, LimitSeconds, "the hostile ability to land");
            var hostileAt = simulation.Now;
            SimulationClock.RunUntil(simulation, () => caster.Stats.AbilitiesUsed >= 2, LimitSeconds, "the second ability to be used");

            (simulation.Now - hostileAt).Should().BeLessThan(HostileAbilityDelay.Seconds);
        }

        [Test]
        public void HostileAbilityDelaySeconds_RejectsANegativeValue()
        {
            var rules = RuleSets.Current();
            rules.HostileAbilityDelaySeconds = -1f;

            rules.GetProblems().Should().ContainMatch("*HostileAbilityDelaySeconds*");
        }

        private static AbilityUseSpec Hostile(string feat, float damage)
        {
            var use = TestCombatants.Use(feat, AbilityTarget.Enemy);
            use.Damage = new DamageSpec { Min = damage, Max = damage, HitChance = 1f };
            return use;
        }

        private static (double FirstAt, double SecondAt) HostileHitTimes(Simulation simulation, Combatant enemy)
        {
            SimulationClock.RunUntil(simulation, () => enemy.Hp < TestCombatants.EffectivelyUnlimited, LimitSeconds, "the first hostile ability to land");
            var firstAt = simulation.Now;
            SimulationClock.RunUntil(simulation, () => enemy.Hp <= TestCombatants.EffectivelyUnlimited - TouchDamage - PushDamage, LimitSeconds, "the second hostile ability to land");
            return (firstAt, simulation.Now);
        }

        private (Simulation Simulation, Combatant Caster, Combatant Enemy) Spawn(RuleSet rules, params AbilityUseSpec[] abilities)
        {
            var caster = TestCombatants.Medic("Caster", abilities);
            caster.ReactionSeconds = 0.1f;
            // Affinity would scale the Force damage this test reads.
            caster.ForceAffinity = 0;
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { caster }, new[] { TestCombatants.Dummy("Enemy", true) }),
                rules,
                _catalog);
            return (simulation, simulation.Get("Caster"), simulation.Get("Enemy"));
        }
    }
}
