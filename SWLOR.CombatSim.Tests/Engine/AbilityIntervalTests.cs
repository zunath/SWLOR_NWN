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
        public void FallingBackToACheaperAbility_StillCountsAsNotAffordingTheWantedOne()
        {
            var lightning = TestCombatants.Use("ForceLightning3", AbilityTarget.Enemy);
            lightning.Damage = new DamageSpec { Min = 1f, Max = 1f, HitChance = 1f };
            var caster = TestCombatants.Medic("Caster", lightning, TestCombatants.Use("MedKit1", AbilityTarget.Self, minIntervalSeconds: 5f));
            caster.MaxFp = 1;
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { caster }, new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog);
            var combatant = simulation.Get("Caster");

            SimulationClock.RunUntil(simulation, WindowSeconds);

            combatant.Stats.AbilitiesUsed.Should().BeGreaterThan(0, "the cheap fallback is cast");
            combatant.Stats.StarvedSeconds.Should().BeGreaterThan(WindowSeconds - 3,
                "Force Lightning is wanted first and never affordable, including on the steps the fallback is cast");
        }

        [Test]
        public void StarvedTime_NeverExceedsTheFight_WhenTheLastStepRunsPastItsEnd()
        {
            var lightning = TestCombatants.Use("ForceLightning3", AbilityTarget.Enemy);
            lightning.Damage = new DamageSpec { Min = 1f, Max = 1f, HitChance = 1f };
            var caster = TestCombatants.Medic("Caster", lightning);
            caster.MaxFp = 1;
            caster.ReactionSeconds = 0.1f;
            var scenario = TestScenarios.TwoTeams(false, new[] { caster }, new[] { TestCombatants.Dummy("Enemy", true) }, durationSeconds: 0.05f);

            var result = new Simulation(scenario, RuleSets.Current(), _catalog, 1).Run();

            result.Combatants.Single(c => c.Name == "Caster").Stats.StarvedSeconds.Should().BeLessThanOrEqualTo(0.05 + 1e-9);
        }

        [Test]
        public void StarvedTime_NeverExceedsTheFight_WhenTheFightIsDecidedMidStep()
        {
            var lightning = TestCombatants.Use("ForceLightning3", AbilityTarget.Enemy);
            lightning.Damage = new DamageSpec { Min = 1f, Max = 1f, HitChance = 1f };
            var caster = TestCombatants.Medic("Caster", lightning);
            caster.MaxFp = 1;
            caster.ReactionSeconds = 0.1f;
            caster.AutoAttack = new AutoAttackSpec { AttacksPerSecond = 1f, Damage = new DamageSpec { Min = 100f, Max = 100f, HitChance = 1f } };
            var enemy = TestCombatants.Dummy("Enemy", true);
            enemy.MaxHp = 1;
            var scenario = TestScenarios.TwoTeams(false, new[] { caster }, new[] { enemy }, durationSeconds: 60f);

            var result = new Simulation(scenario, RuleSets.Current(), _catalog, 1).Run();

            result.WinningTeam.Should().Be(0);
            result.Combatants.Single(c => c.Name == "Caster").Stats.StarvedSeconds.Should().BeLessThanOrEqualTo(result.EndedAt,
                "nothing is starved after the step that decides the fight");
        }

        [Test]
        public void MinIntervalSeconds_IsNotRaisedAsStarvation()
        {
            var (simulation, caster, _) = SpawnCaster(Rock(20f));

            SimulationClock.RunUntil(simulation, WindowSeconds);

            caster.Stats.StarvedSeconds.Should().Be(0);
            caster.Stats.FirstStarvedAt.Should().BeNull();
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Spawn_RejectsAnIntervalThatIsNegativeOrNotFinite(float interval)
        {
            var scenario = TestScenarios.TwoTeams(
                false,
                new[] { TestCombatants.Medic("Caster", Rock(interval)) },
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
