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
    public class DelayedEngagementTests
    {
        private const double FullHp = TestCombatants.EffectivelyUnlimited;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void ACombatantThatHasNotJoined_NeitherAttacksNorIsAttacked()
        {
            var latecomer = TestCombatants.Striker("Latecomer");
            latecomer.JoinAtSeconds = 20f;
            var simulation = Spawn(new[] { TestCombatants.Striker("Early") }, new[] { latecomer });

            SimulationClock.RunUntil(simulation, 19.5);

            simulation.Get("Latecomer").Hp.Should().Be(1000);
            simulation.Get("Latecomer").Stats.DamageDealt.Should().Be(0);
            simulation.Get("Early").Hp.Should().Be(1000);
            simulation.Get("Early").Stats.DamageDealt.Should().Be(0, "there was nobody to attack yet");
        }

        [Test]
        public void ACombatantFightsOnceItsJoinTimePasses()
        {
            var latecomer = TestCombatants.Striker("Latecomer");
            latecomer.JoinAtSeconds = 20f;
            var simulation = Spawn(new[] { TestCombatants.Striker("Early") }, new[] { latecomer });

            SimulationClock.RunUntil(simulation, 26);

            simulation.Get("Latecomer").Stats.DamageDealt.Should().BeGreaterThan(0);
            simulation.Get("Early").Stats.DamageDealt.Should().BeGreaterThan(0);
            simulation.Get("Latecomer").Hp.Should().BeLessThan(1000);
            simulation.Get("Early").Hp.Should().BeLessThan(1000);
        }

        [Test]
        public void ACombatantThatHasNotJoined_StillRegeneratesHp()
        {
            var latecomer = TestCombatants.Dummy("Latecomer", true);
            latecomer.JoinAtSeconds = 100f;
            latecomer.HpRegenPer30Seconds = 30f;
            var simulation = Spawn(new[] { TestCombatants.Dummy("Early", true) }, new[] { latecomer });
            simulation.Get("Latecomer").Hp = 500_000;

            SimulationClock.RunUntil(simulation, 31);

            simulation.Get("Latecomer").Hp.Should().BeGreaterThan(500_000);
        }

        [Test]
        public void ACombatantThatHasNotJoined_DoesNotCastAbilities()
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use("MedKit4", AbilityTarget.Self));
            medic.JoinAtSeconds = 15f;
            var simulation = Spawn(new[] { medic }, new[] { TestCombatants.Dummy("Enemy", true) });

            SimulationClock.RunUntil(simulation, 14.5);
            simulation.Get("Medic").Stats.AbilitiesUsed.Should().Be(0);
            simulation.Get("Medic").Casting.Should().BeNull();

            SimulationClock.RunUntil(simulation, 20);
            simulation.Get("Medic").Stats.AbilitiesUsed.Should().BeGreaterThan(0);
        }

        [Test]
        public void AreaDamage_SkipsEnemiesThatHaveNotJoined()
        {
            var use = TestCombatants.Use("QuickDraw4", AbilityTarget.Enemy);
            use.Damage = new DamageSpec { Min = 10f, Max = 10f, HitChance = 1f, Area = true };
            var caster = new CombatantSpec { Name = "Caster", MaxHp = 100, MaxStm = 100, Abilities = { use } };
            var present = TestCombatants.Dummy("Present", true);
            var absent = TestCombatants.Dummy("Absent", true);
            absent.JoinAtSeconds = 60f;
            var simulation = Spawn(new[] { caster }, new[] { present, absent });

            SimulationClock.RunUntil(simulation, () => simulation.Get("Caster").Stats.AbilitiesUsed == 1, 10, "area ability used");

            simulation.Get("Present").Hp.Should().Be(FullHp - 10);
            simulation.Get("Absent").Hp.Should().Be(FullHp);
        }

        [Test]
        public void ZoneHeals_SkipAlliesThatHaveNotJoined()
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use("ForceSanctuary1", AbilityTarget.Self));
            var present = new CombatantSpec { Name = "Present", MaxHp = 1000 };
            var absent = new CombatantSpec { Name = "Absent", MaxHp = 1000, JoinAtSeconds = 100f };
            var simulation = Spawn(new[] { medic, present, absent }, new[] { TestCombatants.Dummy("Enemy", true) });
            simulation.Get("Present").Hp = 500;
            simulation.Get("Absent").Hp = 500;

            SimulationClock.RunUntil(simulation, 12);

            simulation.Get("Present").Hp.Should().BeGreaterThan(500);
            simulation.Get("Absent").Hp.Should().Be(500);
        }

        [Test]
        public void PvpRevives_ArePermittedWhileEveryEnemyIsStillAway()
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use("Resuscitation2", AbilityTarget.DeadAlly));
            var ally = new CombatantSpec { Name = "Ally", MaxHp = 1000 };
            var enemy = TestCombatants.Dummy("Enemy");
            enemy.JoinAtSeconds = 100f;
            var simulation = Spawn(new[] { medic, ally }, new[] { enemy }, isPvp: true, RuleSets.Tuned());
            simulation.ApplyDamage(simulation.Get("Enemy"), simulation.Get("Ally"), 5000);
            simulation.Get("Ally").IsDead.Should().BeTrue();

            // The player kill itself holds revives for the party damage window; after that only enemies could.
            SimulationClock.RunUntil(simulation, RuleSets.Tuned().PvpReviveDamageWindowSeconds + 1);
            var blockedByTheKill = simulation.Get("Medic").Stats.BlockedRevives;
            SimulationClock.RunUntil(simulation, () => !simulation.Get("Ally").IsDead, 40, "ally revived");

            simulation.Get("Medic").Stats.BlockedRevives.Should().Be(blockedByTheKill, "an enemy that has not joined blocks nothing");
        }

        [Test]
        public void ATeamWhoseMembersHaveNotJoined_IsNotCountedAsBeaten()
        {
            var latecomer = TestCombatants.Dummy("Latecomer", true);
            latecomer.JoinAtSeconds = 100f;
            var scenario = TestScenarios.TwoTeams(false, new[] { TestCombatants.Striker("Early") }, new[] { latecomer }, durationSeconds: 30f);

            var result = new Simulation(scenario, RuleSets.Current(), _catalog, 1).Run();

            result.WinningTeam.Should().BeNull();
            result.EndedAt.Should().BeApproximately(30.0, 1e-6);
        }

        [Test]
        public void ALateJoinersFirstSwing_FallsAnywhereInItsFirstIntervalAfterJoining()
        {
            var firstSwings = new List<double>();
            for (var seed = 1; seed <= 8; seed++)
            {
                var striker = TestCombatants.Striker("Striker", attacksPerSecond: 0.1f, damage: 1f);
                striker.JoinAtSeconds = 60f;
                var simulation = TestScenarios.Spawn(
                    TestScenarios.TwoTeams(false, new[] { striker }, new[] { TestCombatants.Dummy("Enemy", true) }, durationSeconds: 200f),
                    RuleSets.Current(),
                    _catalog,
                    seed);
                var enemy = simulation.Get("Enemy");
                SimulationClock.RunUntil(simulation, () => enemy.Stats.DamageTakenToHp > 0, 90, "the first swing");
                firstSwings.Add(simulation.Now);
            }

            firstSwings.Should().OnlyContain(at => at >= 60 && at <= 70.2, "within one ten-second interval of joining");
            firstSwings.Max().Should().BeGreaterThan(61, "the first swing is not pinned to the join time");
        }

        [Test]
        public void Swings_AreNotMadeUpFromBeforeAnyEnemyArrived()
        {
            var striker = TestCombatants.Striker("Striker", attacksPerSecond: 20f, damage: 1f);
            var enemy = TestCombatants.Dummy("Enemy", true);
            enemy.JoinAtSeconds = 9.9f;
            var scenario = TestScenarios.TwoTeams(false, new[] { striker }, new[] { enemy }, durationSeconds: 10f);
            scenario.TimeStepSeconds = 10f;

            var result = new Simulation(scenario, RuleSets.Current(), _catalog, 1).Run();

            result.Combatants.Single(c => c.Name == "Enemy").Stats.DamageTakenToHp.Should().BeLessThanOrEqualTo(5,
                "only the tenth of a second after the enemy arrived is caught up, about two swings");
        }

        [Test]
        public void CaughtUpSwings_GoToEnemiesThatWereThereAtTheTime()
        {
            var striker = TestCombatants.Striker("Striker", attacksPerSecond: 20f, damage: 1f);
            var late = TestCombatants.Dummy("Late", true);
            late.JoinAtSeconds = 9.9f;
            var scenario = TestScenarios.TwoTeams(false, new[] { striker }, new[] { TestCombatants.Dummy("Early", true), late });
            scenario.Teams[0].TargetPolicy = TargetPolicy.Focus;
            scenario.Teams[0].FocusTarget = "Late";
            var simulation = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog);

            SimulationClock.RunUntil(simulation, 10.5, stepSeconds: 10.0);

            simulation.Get("Late").Stats.DamageTakenToHp.Should().BeLessThanOrEqualTo(5,
                "only swings from 9.9s on can reach the focus target that joined then");
            simulation.Get("Early").Stats.DamageTakenToHp.Should().BeGreaterThan(150, "earlier swings stay on the enemy who was there");
        }

        [Test]
        public void ACaughtUpZonePulse_ReachesOnlyAlliesWhoHadJoinedByThen()
        {
            var sanctuary = TestCombatants.Use("ForceSanctuary1", AbilityTarget.Self, minIntervalSeconds: 600);
            var medic = TestCombatants.Medic("Medic", sanctuary);
            var late = new CombatantSpec { Name = "Late", MaxHp = 1000, JoinAtSeconds = 12f };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { medic, late }, new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog);
            var caster = simulation.Get("Medic");
            var lateAlly = simulation.Get("Late");
            caster.Hp = 100;
            lateAlly.Hp = 100;
            SimulationClock.RunUntil(simulation, () => caster.Stats.AbilitiesUsed == 1, 5, "Force Sanctuary placed");
            caster.Hp = 100;

            // One coarse step catches up every pulse due before it, including those from before the ally joined.
            SimulationClock.RunUntil(simulation, simulation.Now + 20.5, stepSeconds: 20.0);

            lateAlly.Stats.HealingReceived.Should().BeGreaterThan(0, "pulses after it joined reach it");
            lateAlly.Stats.HealingReceived.Should().BeLessThan(caster.Stats.HealingReceived,
                "pulses from before it joined do not");
        }

        [Test]
        public void AFirstDecision_FallsAnywhereWithinTheReactionTime()
        {
            var firstCasts = new List<double>();
            for (var seed = 1; seed <= 8; seed++)
            {
                var rock = TestCombatants.Use("ThrowRock3", AbilityTarget.Enemy, minIntervalSeconds: 600);
                rock.Damage = new DamageSpec { Min = 1f, Max = 1f, HitChance = 1f };
                var thrower = TestCombatants.Medic("Thrower", rock);
                thrower.ReactionSeconds = 6f;
                var simulation = TestScenarios.Spawn(
                    TestScenarios.TwoTeams(false, new[] { thrower }, new[] { TestCombatants.Dummy("Enemy", true) }),
                    RuleSets.Current(),
                    _catalog,
                    seed);
                var caster = simulation.Get("Thrower");
                SimulationClock.RunUntil(simulation, () => caster.Stats.AbilitiesUsed > 0 || caster.Casting != null, 10, "the first cast");
                firstCasts.Add(simulation.Now);
            }

            firstCasts.Should().OnlyContain(at => at <= 6.2, "within one six-second reaction time");
            firstCasts.Max().Should().BeGreaterThan(1.5, "the first decision is spread over the reaction time, not the first second");
        }

        [Test]
        public void ALateJoiner_DoesNotCatchUpSwingsFromBeforeItJoined()
        {
            var striker = TestCombatants.Striker("Striker", attacksPerSecond: 20f, damage: 1f);
            striker.JoinAtSeconds = 9.9f;
            var scenario = TestScenarios.TwoTeams(false, new[] { striker }, new[] { TestCombatants.Dummy("Enemy", true) }, durationSeconds: 10f);
            scenario.TimeStepSeconds = 10f;

            var result = new Simulation(scenario, RuleSets.Current(), _catalog, 1).Run();

            result.Combatants.Single(c => c.Name == "Enemy").Stats.DamageTakenToHp.Should().BeLessThanOrEqualTo(5,
                "only the tenth of a second after joining is caught up, about two swings");
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Spawn_RejectsAJoinTimeThatIsNegativeOrNotFinite(float joinAt)
        {
            var early = TestCombatants.Striker("Early");
            early.JoinAtSeconds = joinAt;
            var simulation = new Simulation(
                TestScenarios.TwoTeams(false, new[] { early }, new[] { TestCombatants.Dummy("Enemy", true) }), RuleSets.Current(), _catalog, 1);

            var act = () => simulation.Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*JoinAtSeconds*");
        }

        private Simulation Spawn(IEnumerable<CombatantSpec> first, IEnumerable<CombatantSpec> second, bool isPvp = false, RuleSet rules = null) =>
            TestScenarios.Spawn(TestScenarios.TwoTeams(isPvp, first, second), rules ?? RuleSets.Current(), _catalog);
    }
}
