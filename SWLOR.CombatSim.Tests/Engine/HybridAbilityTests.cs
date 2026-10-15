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
        public void PurifyingWave_StillHealsAndHitsAnEnemyLeft_WhenAnEnemyDiesMidCast()
        {
            var simulation = SpawnWave(maxTargets: null, new[] { "Wounded" }, "First", "Second");
            var medic = simulation.Get("Medic");
            var wounded = simulation.Get("Wounded");
            wounded.Hp = AllyMaxHp / 2;

            SimulationClock.RunUntil(simulation, () => medic.Casting != null, 10, "Purifying Wave started");
            var fallen = simulation.Get("First");
            var other = simulation.Get("Second");
            simulation.ApplyDamage(wounded, fallen, TestCombatants.EffectivelyUnlimited * 2.0);
            CastOnce(simulation);

            wounded.Hp.Should().Be(AllyMaxHp / 2 + ExpectedWaveHeal(), "the wave is centered on the caster, not the enemy");
            other.Stats.DamageTakenToHp.Should().Be(WaveDamage, "the damage goes to an enemy still in the fight");
        }

        [TestCase(true, 1)]
        [TestCase(false, 2)]
        public void AreaDamageCast_StillLands_WhenItsTargetDiesMidCast(bool area, int targets)
        {
            var burst = TestCombatants.Use("ForceBurst1", AbilityTarget.Enemy);
            burst.Damage = new DamageSpec { Min = WaveDamage, Max = WaveDamage, HitChance = 1f, Area = area, Targets = targets };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[] { TestCombatants.Medic("Medic", burst) },
                    new[] { TestCombatants.Dummy("First", true), TestCombatants.Dummy("Second", true) }),
                RuleSets.PreTuning(),
                _catalog);
            var medic = simulation.Get("Medic");

            SimulationClock.RunUntil(simulation, () => medic.Casting != null, 10, "Force Burst started");
            var targeted = medic.Casting.Target;
            var other = simulation.Get(targeted.Name == "First" ? "Second" : "First");
            simulation.ApplyDamage(medic, targeted, TestCombatants.EffectivelyUnlimited * 2.0);
            CastOnce(simulation);

            other.Stats.DamageTakenToHp.Should().Be(WaveDamage, "an area cast needs no particular enemy alive");
        }

        [TestCase(3, 3)]
        [TestCase(2, 2)]
        public void AreaRecipients_AreDistinctEnemies_EachHitOnce(int enemies, int expectedVictims)
        {
            var burst = TestCombatants.Use("ForceBurst1", AbilityTarget.Enemy, minIntervalSeconds: 600);
            burst.Damage = new DamageSpec { Min = WaveDamage, Max = WaveDamage, HitChance = 1f, Targets = 3 };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[] { TestCombatants.Medic("Medic", burst) },
                    Enumerable.Range(1, enemies).Select(i => TestCombatants.Dummy($"Enemy {i}", true))),
                RuleSets.PreTuning(),
                _catalog);

            CastOnce(simulation);

            var damaged = simulation.Combatants.Where(c => c.Stats.DamageTakenToHp > 0).ToList();
            damaged.Should().HaveCount(expectedVictims);
            damaged.Should().OnlyContain(c => c.Stats.DamageTakenToHp == WaveDamage, "no enemy takes a second share of the area");
        }

        [Test]
        public void ExtraTargets_TakeTheirShareOfTheDamage_AndTheChosenTargetTheFullHit()
        {
            var lightning = TestCombatants.Use("ForceLightning3", AbilityTarget.Enemy, minIntervalSeconds: 600);
            lightning.Damage = new DamageSpec { Min = WaveDamage, Max = WaveDamage, HitChance = 1f, Targets = 3, ExtraTargetDamagePercent = 50f };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[] { TestCombatants.Medic("Medic", lightning) },
                    Enumerable.Range(1, 3).Select(i => TestCombatants.Dummy($"Enemy {i}", true))),
                RuleSets.PreTuning(),
                _catalog);
            var medic = simulation.Get("Medic");
            SimulationClock.RunUntil(simulation, () => medic.Casting != null || medic.Stats.AbilitiesUsed == 1, 10, "Force Lightning started");
            var chosen = medic.Casting?.Target ?? medic.AttackTarget;

            CastOnce(simulation);

            chosen.Stats.DamageTakenToHp.Should().Be(WaveDamage);
            simulation.Combatants.Where(c => c.TeamIndex == 1 && c != chosen).Select(c => c.Stats.DamageTakenToHp)
                .Should().Equal(WaveDamage / 2, WaveDamage / 2);
        }

        [Test]
        public void AHostileCastWhoseTargetDiesMidCast_IsStillPaidForAndGoesOnRecast()
        {
            var lightning = TestCombatants.Use("ForceLightning3", AbilityTarget.Enemy, minIntervalSeconds: 600);
            lightning.Damage = new DamageSpec { Min = WaveDamage, Max = WaveDamage, HitChance = 1f };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[] { TestCombatants.Medic("Medic", lightning) },
                    new[] { TestCombatants.Dummy("First", true), TestCombatants.Dummy("Second", true) }),
                RuleSets.PreTuning(),
                _catalog);
            var medic = simulation.Get("Medic");
            SimulationClock.RunUntil(simulation, () => medic.Casting != null, 10, "Force Lightning started");
            var targeted = medic.Casting.Target;
            var other = simulation.Get(targeted.Name == "First" ? "Second" : "First");
            simulation.ApplyDamage(medic, targeted, TestCombatants.EffectivelyUnlimited * 2.0);

            CastOnce(simulation);

            medic.Stats.FpSpent.Should().BeGreaterThan(0, "the completed cast is paid for");
            medic.RecastReadyAt.Should().ContainKey("ForceLightning");
            other.Stats.DamageTakenToHp.Should().Be(0, "a single-target cast does not move to another enemy");
        }

        [Test]
        public void ASpreadCastWhosePrimaryFell_HitsOnlyTheOthersAtTheirShare()
        {
            var burst = TestCombatants.Use("ForceBurst1", AbilityTarget.Enemy, minIntervalSeconds: 600);
            burst.Damage = new DamageSpec { Min = 100f, Max = 100f, HitChance = 1f, Targets = 3, ExtraTargetDamagePercent = 50f };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[] { TestCombatants.Medic("Medic", burst) },
                    new[] { TestCombatants.Dummy("One", true), TestCombatants.Dummy("Two", true), TestCombatants.Dummy("Three", true) }),
                RuleSets.PreTuning(),
                _catalog);
            var medic = simulation.Get("Medic");
            SimulationClock.RunUntil(simulation, () => medic.Casting != null, 10, "the cast started");
            var primary = medic.Casting.Target;
            simulation.ApplyDamage(medic, primary, TestCombatants.EffectivelyUnlimited * 2.0);
            primary.IsDead.Should().BeTrue();
            var primaryDamage = primary.Stats.DamageTakenToHp;

            SimulationClock.RunUntil(simulation, () => medic.Stats.AbilitiesUsed == 1, 10, "the cast landed");

            primary.Stats.DamageTakenToHp.Should().Be(primaryDamage, "the fallen primary takes nothing more");
            simulation.Combatants.Where(c => c.TeamIndex == primary.TeamIndex && c != primary)
                .Select(c => c.Stats.DamageTakenToHp)
                .Should().AllBeEquivalentTo(50.0, "no survivor is promoted to the full primary hit");
        }

        [Test]
        public void DamageFromASelfTargetedUse_LandsOnAnEnemy()
        {
            var burst = TestCombatants.Use("SpinningWhirl1", AbilityTarget.Self, minIntervalSeconds: 600);
            burst.Damage = new DamageSpec { Min = WaveDamage, Max = WaveDamage, HitChance = 1f };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[] { TestCombatants.Medic("Medic", burst) },
                    new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.PreTuning(),
                _catalog);

            CastOnce(simulation);

            simulation.Get("Medic").Stats.DamageTakenToHp.Should().Be(0, "a self-centered area never hits its caster");
            simulation.Get("Enemy").Stats.DamageTakenToHp.Should().Be(WaveDamage);
        }

        [Test]
        public void Spawn_RejectsADamageProfileOnASingleTargetHeal()
        {
            var use = TestCombatants.Use("Benevolence1", AbilityTarget.Enemy);
            use.Damage = new DamageSpec { Min = 1f, Max = 1f };
            var scenario = TestScenarios.TwoTeams(false, new[] { TestCombatants.Medic("Medic", use) }, new[] { TestCombatants.Dummy("Enemy", true) });

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Benevolence1*single-target effect*");
        }

        private Simulation SpawnWave(int? maxTargets, params string[] allies) =>
            SpawnWave(maxTargets, allies, "Enemy");

        private Simulation SpawnWave(int? maxTargets, string[] allies, params string[] enemies)
        {
            var wave = TestCombatants.Use("PurifyingWave1", AbilityTarget.Self, maxTargets: maxTargets);
            wave.Damage = new DamageSpec { Min = WaveDamage, Max = WaveDamage, HitChance = 1f };

            var members = new List<CombatantSpec> { TestCombatants.Medic("Medic", wave) };
            members.AddRange(allies.Select(name => new CombatantSpec { Name = name, MaxHp = AllyMaxHp }));
            return TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, members, enemies.Select(name => TestCombatants.Dummy(name, true))),
                RuleSets.PreTuning(),
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
            return HealingMath.PartyPulse(effect, AllyMaxHp, 26, 10, RuleSets.PreTuning().ForceAffinityHealingCap, 0f, 0f);
        }
    }
}
