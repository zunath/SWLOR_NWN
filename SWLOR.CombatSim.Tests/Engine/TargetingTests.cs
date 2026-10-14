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
    public class TargetingTests
    {
        private const double FullHp = TestCombatants.EffectivelyUnlimited;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void LowestHp_BreaksTiesRandomlyInsteadOfTakingTheFirstListedEnemy()
        {
            var victims = new List<string>();
            for (var seed = 1; seed <= 30; seed++)
            {
                var simulation = Spawn(TargetPolicy.LowestHp, attackers: 1, enemies: 3, seed: seed);

                SimulationClock.RunUntil(simulation, 3);

                victims.Add(Damaged(simulation).Should().ContainSingle().Subject.Name);
            }

            victims.Distinct().Should().HaveCountGreaterThan(1);
            victims.Should().Contain(v => v != "Enemy 1");
        }

        [Test]
        public void LowestHp_StillGoesForTheMostWoundedEnemy()
        {
            for (var seed = 1; seed <= 10; seed++)
            {
                var simulation = Spawn(TargetPolicy.LowestHp, attackers: 2, enemies: 4, seed: seed);
                simulation.Get("Enemy 3").Hp = 5000;

                SimulationClock.RunUntil(simulation, 3);

                Damaged(simulation).Select(e => e.Name).Should().Equal("Enemy 3");
            }
        }

        [Test]
        public void Random_WithoutRetargeting_StaysOnOneEnemyForTheWholeFight()
        {
            var simulation = Spawn(TargetPolicy.Random, attackers: 1, enemies: 3);

            SimulationClock.RunUntil(simulation, 60);

            Damaged(simulation).Should().ContainSingle();
        }

        [Test]
        public void Random_WithRetargetSeconds_SpreadsAttacksAcrossEnemies()
        {
            var simulation = Spawn(TargetPolicy.Random, attackers: 1, enemies: 3, retargetSeconds: 2f);

            SimulationClock.RunUntil(simulation, 60);

            Damaged(simulation).Should().HaveCount(3);
        }

        [Test]
        public void LowestHp_WithoutRetargeting_KeepsHittingTheEnemyItStartedOn()
        {
            var simulation = Spawn(TargetPolicy.LowestHp, attackers: 1, enemies: 3);
            SimulationClock.RunUntil(simulation, 3);
            var started = Damaged(simulation).Single();
            var other = simulation.Combatants.First(c => c.TeamIndex == 1 && c != started);
            other.Hp = 1000;

            SimulationClock.RunUntil(simulation, 20);

            other.Hp.Should().Be(1000, "a target is kept until it dies");
        }

        [Test]
        public void LowestHp_WithRetargetSeconds_MovesToWhoeverIsLowestAtTheNextPick()
        {
            var simulation = Spawn(TargetPolicy.LowestHp, attackers: 1, enemies: 3, retargetSeconds: 5f);
            SimulationClock.RunUntil(simulation, 3);
            var started = Damaged(simulation).Single();
            var other = simulation.Combatants.First(c => c.TeamIndex == 1 && c != started);
            other.Hp = 1000;

            SimulationClock.RunUntil(simulation, 12);

            other.Hp.Should().BeLessThan(1000);
            simulation.Get("Striker").AttackTarget.Should().BeSameAs(other);
        }

        [Test]
        public void Coordinated_TheWholeTeamHitsTheSameEnemy_PickedAtRandomWhenAllAreEqual()
        {
            var victims = new List<string>();
            for (var seed = 1; seed <= 20; seed++)
            {
                var simulation = Spawn(TargetPolicy.Coordinated, attackers: 4, enemies: 3, seed: seed);

                SimulationClock.RunUntil(simulation, 10);

                var victim = Damaged(simulation).Should().ContainSingle().Subject;
                victims.Add(victim.Name);
                var dealt = simulation.Combatants.Where(c => c.TeamIndex == 0).Sum(c => c.Stats.DamageDealt);
                (FullHp - victim.Hp).Should().Be(dealt);
                simulation.Combatants.Where(c => c.TeamIndex == 0).Should().OnlyContain(c => c.Stats.DamageDealt > 0, "every attacker joined in");
            }

            victims.Distinct().Should().HaveCountGreaterThan(1);
        }

        [Test]
        public void Coordinated_FocusesTheLowestHpPercentAmongTheSample()
        {
            var simulation = Spawn(TargetPolicy.Coordinated, attackers: 4, enemies: 3);
            simulation.Get("Enemy 2").Hp = 900_000;

            SimulationClock.RunUntil(simulation, 10);

            Damaged(simulation).Select(e => e.Name).Should().Equal("Enemy 2");
            simulation.Get("Enemy 2").Hp.Should().BeLessThan(900_000);
        }

        [Test]
        public void Coordinated_OnlySeesARandomSampleOfThreeEnemies()
        {
            const int seeds = 300;
            var weakChosen = 0;
            for (var seed = 1; seed <= seeds; seed++)
            {
                var simulation = Spawn(TargetPolicy.Coordinated, attackers: 1, enemies: 9, seed: seed);
                simulation.Get("Enemy 9").Hp = 1000;

                SimulationClock.RunUntil(simulation, 3);

                if (simulation.Get("Enemy 9").Hp < 1000)
                    weakChosen++;
            }

            // The weakest enemy is picked only when it falls in the sample of three out of nine.
            (weakChosen / (double)seeds).Should().BeInRange(0.22, 0.45);
        }

        [Test]
        public void Coordinated_PicksANewEnemyTogetherWhenTheTargetDies()
        {
            var simulation = Spawn(TargetPolicy.Coordinated, attackers: 4, enemies: 3);
            simulation.Get("Enemy 1").Hp = 50;
            simulation.Get("Enemy 2").Hp = 500_000;

            SimulationClock.RunUntil(simulation, 10);

            simulation.Get("Enemy 1").IsDead.Should().BeTrue();
            simulation.Get("Enemy 2").Hp.Should().BeLessThan(500_000, "the team moved on to the lowest remaining enemy");
            simulation.Get("Enemy 3").Hp.Should().Be(FullHp);
        }

        [Test]
        public void Coordinated_WithoutRetargetSeconds_KeepsTheTargetUntilItDies()
        {
            var simulation = Spawn(TargetPolicy.Coordinated, attackers: 4, enemies: 3);
            simulation.Get("Enemy 1").Hp = 900_000;
            SimulationClock.RunUntil(simulation, 3);
            simulation.Get("Enemy 3").Hp = 100_000;

            SimulationClock.RunUntil(simulation, 20);

            simulation.Get("Enemy 3").Hp.Should().Be(100_000);
        }

        [Test]
        public void Coordinated_WithRetargetSeconds_ReconsidersTheTargetOnThatSchedule()
        {
            var simulation = Spawn(TargetPolicy.Coordinated, attackers: 4, enemies: 3, retargetSeconds: 5f);
            simulation.Get("Enemy 1").Hp = 900_000;
            SimulationClock.RunUntil(simulation, 3);
            simulation.Get("Enemy 3").Hp = 100_000;

            SimulationClock.RunUntil(simulation, 12);

            simulation.Get("Enemy 3").Hp.Should().BeLessThan(100_000);
        }

        [Test]
        public void Focus_SwitchesToTheFocusTargetOnceItJoinsTheFight()
        {
            var attackers = new[] { TestCombatants.Striker("Striker") };
            var enemies = new[]
            {
                TestCombatants.Dummy("Add", true),
                TestCombatants.Dummy("Boss", true)
            };
            enemies[1].JoinAtSeconds = 5f;
            var scenario = TestScenarios.TwoTeams(false, attackers, enemies);
            scenario.Teams[0].TargetPolicy = TargetPolicy.Focus;
            scenario.Teams[0].FocusTarget = "Boss";
            var simulation = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog);

            SimulationClock.RunUntil(simulation, 4.9);
            simulation.Get("Add").Hp.Should().BeLessThan(FullHp, "only the add could be hit before the boss arrived");
            simulation.Get("Boss").Hp.Should().Be(FullHp);

            // Swings due before the boss joins at 5 seconds may still land on the add.
            SimulationClock.RunUntil(simulation, 5.1);
            var addHp = simulation.Get("Add").Hp;
            SimulationClock.RunUntil(simulation, 12);

            simulation.Get("Boss").Hp.Should().BeLessThan(FullHp);
            simulation.Get("Add").Hp.Should().Be(addHp, "attacks moved to the boss for good");
        }

        [TestCase(null)]
        [TestCase("Bos")]
        [TestCase("Striker")]
        public void Spawn_RejectsAFocusTargetThatNamesNoOpposingCombatant(string focusTarget)
        {
            var scenario = TestScenarios.TwoTeams(false, new[] { TestCombatants.Striker("Striker") }, new[] { TestCombatants.Dummy("Boss", true) });
            scenario.Teams[0].TargetPolicy = TargetPolicy.Focus;
            scenario.Teams[0].FocusTarget = focusTarget;
            var simulation = new Simulation(scenario, RuleSets.Current(), _catalog, 1);

            var act = () => simulation.Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*FocusTarget*");
        }

        [Test]
        public void Spawn_AcceptsAFocusTargetThatMatchesNumberedCopies()
        {
            var bosses = TestCombatants.Dummy("Boss", true);
            bosses.Count = 2;
            var scenario = TestScenarios.TwoTeams(false, new[] { TestCombatants.Striker("Striker") }, new[] { bosses });
            scenario.Teams[0].TargetPolicy = TargetPolicy.Focus;
            scenario.Teams[0].FocusTarget = "Boss";
            var simulation = new Simulation(scenario, RuleSets.Current(), _catalog, 1);

            var act = () => simulation.Spawn();

            act.Should().NotThrow();
        }

        [Test]
        public void LowestAlly_CountsTemporaryHp()
        {
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[]
                    {
                        TestCombatants.Medic("Medic", TestCombatants.Use("Benevolence1", AbilityTarget.LowestAlly, minIntervalSeconds: 600)),
                        new CombatantSpec { Name = "Shielded", MaxHp = 1000 },
                        new CombatantSpec { Name = "Exposed", MaxHp = 1000 }
                    },
                    new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog);
            var shielded = simulation.Get("Shielded");
            shielded.Hp = 300;
            shielded.TemporaryHp.Add(new TemporaryHpPool { Key = "Shield", Amount = 500, ExpiresAt = 600 });
            simulation.Get("Exposed").Hp = 600;

            SimulationClock.RunUntil(simulation, () => simulation.Get("Medic").Stats.AbilitiesUsed == 1, 10, "Benevolence cast");

            simulation.Get("Exposed").Stats.HealingReceived.Should().BeGreaterThan(0);
            shielded.Stats.HealingReceived.Should().Be(0);
        }

        [Test]
        public void BelowHpPercent_CountsTemporaryHp()
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use("Benevolence1", AbilityTarget.Self, belowHpPercent: 50f));
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { medic }, new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog);
            var combatant = simulation.Get("Medic");
            combatant.Hp = 400;
            combatant.TemporaryHp.Add(new TemporaryHpPool { Key = "Shield", Amount = 200, ExpiresAt = 600 });

            SimulationClock.RunUntil(simulation, 5);

            combatant.Stats.AbilitiesUsed.Should().Be(0, "40% HP plus a 20% shield is above the 50% threshold");
        }

        [Test]
        public void Coordinated_CountsTemporaryHp_WhenPickingTheLowestTarget()
        {
            var simulation = Spawn(TargetPolicy.Coordinated, attackers: 1, enemies: 2);
            var shielded = simulation.Get("Enemy 1");
            var exposed = simulation.Get("Enemy 2");
            shielded.Hp = shielded.Spec.MaxHp * 0.30;
            shielded.TemporaryHp.Add(new TemporaryHpPool { Key = "Shield", Amount = shielded.Spec.MaxHp * 0.50, ExpiresAt = 600 });
            exposed.Hp = exposed.Spec.MaxHp * 0.60;

            SimulationClock.RunUntil(simulation, 3);

            exposed.Hp.Should().BeLessThan(exposed.Spec.MaxHp * 0.60, "the exposed enemy is the lower one counting the shield");
            shielded.TemporaryHpTotal.Should().Be(shielded.Spec.MaxHp * 0.50);
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Spawn_RejectsARetargetTimeThatIsNegativeOrNotFinite(float seconds)
        {
            var scenario = TestScenarios.TwoTeams(false, new[] { TestCombatants.Striker("Striker") }, new[] { TestCombatants.Dummy("Enemy", true) });
            scenario.Teams[0].RetargetSeconds = seconds;
            var simulation = new Simulation(scenario, RuleSets.Current(), _catalog, 1);

            var act = () => simulation.Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*RetargetSeconds*");
        }

        private Simulation Spawn(TargetPolicy policy, int attackers, int enemies, int seed = 1, float retargetSeconds = 0f)
        {
            var first = Enumerable.Range(1, attackers).Select(i => TestCombatants.Striker(attackers == 1 ? "Striker" : $"Striker {i}"));
            var second = Enumerable.Range(1, enemies).Select(i => TestCombatants.Dummy($"Enemy {i}", true));
            var scenario = TestScenarios.TwoTeams(false, first, second);
            scenario.Teams[0].TargetPolicy = policy;
            scenario.Teams[0].RetargetSeconds = retargetSeconds;
            return TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, seed);
        }

        private static List<Combatant> Damaged(Simulation simulation) =>
            simulation.Combatants.Where(c => c.TeamIndex == 1 && c.Hp < FullHp).ToList();
    }
}
