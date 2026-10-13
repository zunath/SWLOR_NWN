using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Effects;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class HybridAbilityTests
    {
        private const float WaveDamage = 50f;
        private const int AllyMaxHp = 1000;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void PurifyingWave_DamagesTheEnemyAndHealsThePartyInTheSameCast()
        {
            var simulation = SpawnWave(maxTargets: null, "Wounded");
            var wounded = simulation.Get("Wounded");
            wounded.Hp = AllyMaxHp / 2;

            CastOnce(simulation);

            simulation.Get("Enemy").Stats.DamageTakenToHp.Should().Be(WaveDamage);
            wounded.Hp.Should().Be(AllyMaxHp / 2 + ExpectedWaveHeal());
        }

        [Test]
        public void PurifyingWave_WithMaxTargets_HealsOnlyTheLowestAllies()
        {
            var simulation = SpawnWave(maxTargets: 1, "Wounded", "Scratched");
            simulation.Get("Wounded").Hp = 300;
            simulation.Get("Scratched").Hp = 900;

            CastOnce(simulation);

            simulation.Get("Wounded").Hp.Should().Be(300 + ExpectedWaveHeal());
            simulation.Get("Scratched").Hp.Should().Be(900);
        }

        [Test]
        public void PurifyingWave_StillHealsAndHitsAnotherEnemy_WhenItsTargetDiesMidCast()
        {
            var simulation = SpawnWave(maxTargets: null, new[] { "Wounded" }, "First", "Second");
            var medic = simulation.Get("Medic");
            var wounded = simulation.Get("Wounded");
            wounded.Hp = AllyMaxHp / 2;

            SimulationClock.RunUntil(simulation, () => medic.Casting != null, 10, "Purifying Wave started");
            var targeted = medic.Casting.Target;
            var other = simulation.Get(targeted.Name == "First" ? "Second" : "First");
            simulation.ApplyDamage(wounded, targeted, TestCombatants.EffectivelyUnlimited * 2.0);
            CastOnce(simulation);

            wounded.Hp.Should().Be(AllyMaxHp / 2 + ExpectedWaveHeal(), "the wave is centered on the caster, not the enemy");
            other.Stats.DamageTakenToHp.Should().Be(WaveDamage, "the damage goes to an enemy still in the fight");
        }

        [Test]
        public void Spawn_RejectsADamageProfileOnASingleTargetHeal()
        {
            var use = TestCombatants.Use("Benevolence1", AbilityTarget.Enemy);
            use.Damage = new DamageSpec { Min = 1f, Max = 1f };
            var scenario = TestScenarios.TwoTeams(false, new[] { TestCombatants.Medic("Medic", use) }, new[] { TestCombatants.Dummy("Enemy", true) });

            var act = () => new Simulation(scenario, RuleSets.Current(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Benevolence1*single-target effect*");
        }

        private Simulation SpawnWave(int? maxTargets, params string[] allies) =>
            SpawnWave(maxTargets, allies, "Enemy");

        private Simulation SpawnWave(int? maxTargets, string[] allies, params string[] enemies)
        {
            var wave = TestCombatants.Use("PurifyingWave1", AbilityTarget.Enemy, maxTargets: maxTargets);
            wave.Damage = new DamageSpec { Min = WaveDamage, Max = WaveDamage, HitChance = 1f };

            var members = new List<CombatantSpec> { TestCombatants.Medic("Medic", wave) };
            members.AddRange(allies.Select(name => new CombatantSpec { Name = name, MaxHp = AllyMaxHp }));
            return TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, members, enemies.Select(name => TestCombatants.Dummy(name, true))),
                RuleSets.Current(),
                _catalog);
        }

        private static void CastOnce(Simulation simulation)
        {
            var medic = simulation.Get("Medic");
            SimulationClock.RunUntil(simulation, () => medic.Stats.AbilitiesUsed == 1, 10, "Purifying Wave resolved");
        }

        // 8% of the ally's max HP, scaled by the medic's maximum Willpower and full Light affinity.
        private static int ExpectedWaveHeal()
        {
            EffectCatalog.TryGet(FeatType.PurifyingWave1, out var effect).Should().BeTrue();
            return HealingMath.PartyPulse(effect, AllyMaxHp, 26, 10, RuleSets.Current().ForceAffinityHealingCap, 0f, 0f);
        }
    }
}
