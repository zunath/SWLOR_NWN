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
    public class AbilityIntervalTests
    {
        private const double WindowSeconds = 55.0;
        private const double RockDamage = 10.0;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void AnAbilityOffRecast_IsUsedAgainAsSoonAsItsRecastEnds_WhenNoIntervalIsSet()
        {
            var (simulation, caster, enemy) = SpawnCaster(Rock(0f));

            SimulationClock.RunUntil(simulation, WindowSeconds);

            RocksThrown(enemy).Should().BeGreaterThanOrEqualTo(7, "Throw Rock recasts every six seconds");
            caster.Stats.AbilitiesUsed.Should().Be(RocksThrown(enemy));
        }

        [Test]
        public void MinIntervalSeconds_KeepsAnAbilityOffRecastFromBeingUsedMoreOften()
        {
            var (simulation, _, enemy) = SpawnCaster(Rock(20f));

            SimulationClock.RunUntil(simulation, WindowSeconds);

            RocksThrown(enemy).Should().Be(3, "one use roughly every 20 seconds in 55 seconds");
        }

        [Test]
        public void MinIntervalSeconds_DoesNotHoldBackTheRestOfThePriorityList()
        {
            var (simulation, caster, enemy) = SpawnCaster(Rock(20f), TestCombatants.Use("MedKit4", AbilityTarget.Self));

            SimulationClock.RunUntil(simulation, WindowSeconds);

            RocksThrown(enemy).Should().Be(3);
            (caster.Stats.AbilitiesUsed - RocksThrown(enemy)).Should().BeGreaterThanOrEqualTo(5, "Med Kit kept firing while the rock waited");
        }

        [Test]
        public void MinIntervalSeconds_IsCountedPerAbilityAcrossEntriesWithTheSameFeat()
        {
            var (simulation, caster, _) = SpawnCaster(
                TestCombatants.Use("MedKit4", AbilityTarget.Self, minIntervalSeconds: 20f),
                TestCombatants.Use("MedKit4", AbilityTarget.NamedAlly, targetName: "Ally", minIntervalSeconds: 20f));

            SimulationClock.RunUntil(simulation, WindowSeconds);

            caster.Stats.AbilitiesUsed.Should().Be(3, "the self and ally entries share one cadence");
        }

        [Test]
        public void MinIntervalSeconds_IsNotRaisedAsStarvation()
        {
            var (simulation, caster, _) = SpawnCaster(Rock(20f));

            SimulationClock.RunUntil(simulation, WindowSeconds);

            caster.Stats.StarvedSeconds.Should().Be(0);
            caster.Stats.FirstStarvedAt.Should().BeNull();
        }

        [Test]
        public void Spawn_RejectsANegativeInterval()
        {
            var scenario = TestScenarios.TwoTeams(
                false,
                new[] { TestCombatants.Medic("Caster", Rock(-1f)) },
                new[] { TestCombatants.Dummy("Enemy", true) });
            var simulation = new Simulation(scenario, RuleSets.Current(), _catalog, 1);

            var act = () => simulation.Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*MinIntervalSeconds*");
        }

        private static AbilityUseSpec Rock(float minIntervalSeconds)
        {
            var use = TestCombatants.Use("ThrowRock3", AbilityTarget.Enemy, minIntervalSeconds: minIntervalSeconds);
            use.Damage = new DamageSpec { Min = (float)RockDamage, Max = (float)RockDamage, HitChance = 1f };
            return use;
        }

        private (Simulation Simulation, Combatant Caster, Combatant Enemy) SpawnCaster(params AbilityUseSpec[] abilities)
        {
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[] { TestCombatants.Medic("Caster", abilities), new CombatantSpec { Name = "Ally", MaxHp = 1000 } },
                    new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog);
            return (simulation, simulation.Get("Caster"), simulation.Get("Enemy"));
        }

        private static int RocksThrown(Combatant enemy) =>
            (int)Math.Round((TestCombatants.EffectivelyUnlimited - enemy.Hp) / RockDamage);
    }
}
