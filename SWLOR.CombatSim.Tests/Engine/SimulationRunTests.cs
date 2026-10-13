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
    public class SimulationRunTests
    {
        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void Run_EndsWhenOneTeamIsLeftStanding()
        {
            var scenario = TestScenarios.TwoTeams(true, new[] { Striker() }, new[] { new CombatantSpec { Name = "Target", MaxHp = 100 } });

            var result = new Simulation(scenario, RuleSets.Current(), _catalog, 1).Run();

            result.WinningTeam.Should().Be(0);
            result.EndedAt.Should().BeInRange(0.1, 10);
            result.Deaths.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Name = "Target", TeamIndex = 1, KilledBy = "Striker" },
                options => options.ExcludingMissingMembers());
            result.FirstDeathAt.Should().Be(result.Deaths[0].At);
        }

        [Test]
        public void Run_EndsAtTheTimeLimitWithNoWinnerWhenNobodyCanHurtAnybody()
        {
            var scenario = TestScenarios.TwoTeams(true,
                new[] { TestCombatants.Dummy("Left") }, new[] { TestCombatants.Dummy("Right") }, durationSeconds: 5f);

            var result = new Simulation(scenario, RuleSets.Current(), _catalog, 1).Run();

            result.WinningTeam.Should().BeNull();
            result.EndedAt.Should().BeApproximately(5.0, 1e-6);
            result.Deaths.Should().BeEmpty();
            result.FirstDeathAt.Should().BeNull();
            result.Revives.Should().Be(0);
        }

        [Test]
        public void Run_ProducesIdenticalResultsForTheSameSeed()
        {
            var first = RunFight(seed: 7);
            var second = RunFight(seed: 7);

            second.EndedAt.Should().Be(first.EndedAt);
            second.Combatants.Select(c => c.Stats.DamageDealt).Should().Equal(first.Combatants.Select(c => c.Stats.DamageDealt));
            second.Combatants.Select(c => c.Hp).Should().Equal(first.Combatants.Select(c => c.Hp));
        }

        [Test]
        public void Run_ProducesDifferentResultsForDifferentSeeds()
        {
            var damageBySeed = Enumerable.Range(1, 10)
                .Select(seed => RunFight(seed).Combatants.Sum(c => c.Stats.DamageDealt))
                .Distinct()
                .ToList();

            damageBySeed.Should().HaveCountGreaterThan(1);
        }

        [Test]
        public void Spawn_NumbersCombatantsWhenCountIsAboveOne()
        {
            var raiders = new CombatantSpec { Name = "Raider", Count = 3, MaxHp = 100 };
            var lone = new CombatantSpec { Name = "Lone", MaxHp = 100 };
            var scenario = TestScenarios.TwoTeams(false, new[] { lone }, new[] { raiders });

            var simulation = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog);

            simulation.Combatants.Select(c => c.Name).Should().Equal("Lone", "Raider 1", "Raider 2", "Raider 3");
            simulation.Combatants.Select(c => c.TeamIndex).Should().Equal(0, 1, 1, 1);
        }

        [Test]
        public void Spawn_StartsCombatantsAtFullHpFpAndStamina()
        {
            var spec = new CombatantSpec { Name = "Full", MaxHp = 800, MaxFp = 50, MaxStm = 70 };

            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { spec }, new[] { TestCombatants.Dummy("Enemy", true) }), RuleSets.Current(), _catalog);

            var combatant = simulation.Get("Full");
            combatant.Hp.Should().Be(800);
            combatant.Fp.Should().Be(50);
            combatant.Stm.Should().Be(70);
            combatant.HpPercent.Should().Be(100f);
        }

        [Test]
        public void Spawn_RejectsAnUnknownAbilityName()
        {
            var spec = new CombatantSpec { Name = "Typo", MaxHp = 100, Abilities = { TestCombatants.Use("NotAnAbility9", AbilityTarget.Enemy) } };
            var simulation = new Simulation(
                TestScenarios.TwoTeams(false, new[] { spec }, new[] { TestCombatants.Dummy("Enemy", true) }), RuleSets.Current(), _catalog, 1);

            var act = () => simulation.Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*NotAnAbility9*");
        }

        [Test]
        public void Spawn_RejectsAnAbilityWithNeitherAModeledEffectNorADamageProfile()
        {
            var spec = new CombatantSpec { Name = "Unmodeled", MaxHp = 100, Abilities = { TestCombatants.Use("QuickDraw4", AbilityTarget.Enemy) } };
            var simulation = new Simulation(
                TestScenarios.TwoTeams(false, new[] { spec }, new[] { TestCombatants.Dummy("Enemy", true) }), RuleSets.Current(), _catalog, 1);

            var act = () => simulation.Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*QuickDraw4*");
        }

        [Test]
        public void AreaDamageAbility_HitsEveryLivingEnemyOnce()
        {
            var use = TestCombatants.Use("QuickDraw4", AbilityTarget.Enemy);
            use.Damage = new DamageSpec { Min = 10f, Max = 10f, HitChance = 1f, Area = true };
            var caster = new CombatantSpec { Name = "Caster", MaxHp = 100, MaxStm = 100, Abilities = { use } };
            var enemies = new CombatantSpec { Name = "Enemy", Count = 3, MaxHp = 1000, IsNpc = true };
            var simulation = TestScenarios.Spawn(TestScenarios.TwoTeams(false, new[] { caster }, new[] { enemies }), RuleSets.Current(), _catalog);
            var spawned = simulation.Get("Caster");

            SimulationClock.RunUntil(simulation, () => spawned.Stats.AbilitiesUsed == 1, 10, "area ability used");

            simulation.Combatants.Where(c => c.Name.StartsWith("Enemy")).Select(c => c.Hp).Should().Equal(990.0, 990.0, 990.0);
            spawned.Stats.DamageDealt.Should().Be(30);
        }

        [Test]
        public void ApplyDamage_ShieldsAbsorbBeforeHpAndDamageReductionComesFirst()
        {
            var victim = new CombatantSpec { Name = "Victim", MaxHp = 1000, DamageReductionPercent = 50f };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { victim }, new[] { TestCombatants.Dummy("Enemy", true) }), RuleSets.Current(), _catalog);
            var target = simulation.Get("Victim");
            var attacker = simulation.Get("Enemy");
            target.TemporaryHp.Add(new TemporaryHpPool { Key = "Shield", Amount = 100, ExpiresAt = 100 });

            simulation.ApplyDamage(attacker, target, 300);

            target.TemporaryHp.Should().BeEmpty();
            target.Stats.DamageAbsorbedByTemporaryHp.Should().Be(100);
            target.Stats.DamageTakenToHp.Should().Be(50);
            target.Hp.Should().Be(950);
        }

        [Test]
        public void ApplyHealing_NeverGoesPastMaxHpAndCountsTheExcessAsOverhealing()
        {
            var spec = new CombatantSpec { Name = "Patient", MaxHp = 1000 };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { spec }, new[] { TestCombatants.Dummy("Enemy", true) }), RuleSets.Current(), _catalog);
            var patient = simulation.Get("Patient");
            patient.Hp = 900;

            simulation.ApplyHealing(null, patient, 250, HealKind.InstantAbility);

            patient.Hp.Should().Be(1000);
            patient.Stats.HealingReceived.Should().Be(100);
            patient.Stats.Overhealing.Should().Be(150);
        }

        [Test]
        public void ApplyHealing_AppliesTheTargetsHealingReceivedBonus_ButNotToNaturalRegeneration()
        {
            var spec = new CombatantSpec { Name = "Patient", MaxHp = 10_000, HealingReceivedPercent = 10f };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { spec }, new[] { TestCombatants.Dummy("Enemy", true) }), RuleSets.Current(), _catalog);
            var patient = simulation.Get("Patient");
            patient.Hp = 1000;

            simulation.ApplyHealing(null, patient, 100, HealKind.InstantAbility);
            patient.Hp.Should().Be(1110);

            simulation.ApplyHealing(null, patient, 100, HealKind.NaturalRegeneration);
            patient.Hp.Should().Be(1210);
        }

        private SimulationResult RunFight(int seed)
        {
            var scenario = TestScenarios.TwoTeams(
                true,
                new[] { RandomFighter("Left") },
                new[] { RandomFighter("Right") },
                durationSeconds: 60f);

            return new Simulation(scenario, RuleSets.Current(), _catalog, seed).Run();
        }

        private static CombatantSpec RandomFighter(string name) => new()
        {
            Name = name,
            MaxHp = 2000,
            AutoAttack = new AutoAttackSpec
            {
                AttacksPerSecond = 1f,
                Damage = new DamageSpec { Min = 10f, Max = 30f, HitChance = 0.5f, CritChance = 0.2f }
            }
        };

        private static CombatantSpec Striker() => new()
        {
            Name = "Striker",
            MaxHp = 1000,
            AutoAttack = new AutoAttackSpec
            {
                AttacksPerSecond = 2f,
                Damage = new DamageSpec { Min = 50f, Max = 50f, HitChance = 1f }
            }
        };
    }
}
