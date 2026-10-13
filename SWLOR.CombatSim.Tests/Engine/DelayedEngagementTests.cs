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

            SimulationClock.RunUntil(simulation, () => !simulation.Get("Ally").IsDead, 30, "ally revived");

            simulation.Get("Medic").Stats.BlockedRevives.Should().Be(0);
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
        public void Spawn_RejectsANegativeJoinTime()
        {
            var early = TestCombatants.Striker("Early");
            early.JoinAtSeconds = -1f;
            var simulation = new Simulation(
                TestScenarios.TwoTeams(false, new[] { early }, new[] { TestCombatants.Dummy("Enemy", true) }), RuleSets.Current(), _catalog, 1);

            var act = () => simulation.Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*JoinAtSeconds*");
        }

        private Simulation Spawn(IEnumerable<CombatantSpec> first, IEnumerable<CombatantSpec> second, bool isPvp = false, RuleSet rules = null) =>
            TestScenarios.Spawn(TestScenarios.TwoTeams(isPvp, first, second), rules ?? RuleSets.Current(), _catalog);
    }
}
