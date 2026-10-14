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
    public class TimeStepTests
    {
        private const double BaselineStep = 0.1;
        private const double EndSeconds = 63;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [TestCase(1.0)]
        [TestCase(3.0)]
        public void Autoattacks_AreAllMade_WhateverTheTimeStep(double step)
        {
            var baseline = AttackDamageWithStep(BaselineStep);

            baseline.Should().BeGreaterThan(1000, "two attacks a second for about a minute");
            AttackDamageWithStep(step).Should().Be(baseline);
        }

        [Test]
        public void Autoattacks_HonorTheirHitsAndArea()
        {
            var striker = TestCombatants.Striker("Striker", attacksPerSecond: 1f, damage: 10f);
            striker.AutoAttack.Damage.Hits = 3;
            striker.AutoAttack.Damage.Area = true;
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { striker }, new[] { TestCombatants.Dummy("First", true), TestCombatants.Dummy("Second", true) }),
                RuleSets.Current(),
                _catalog);

            RunThrough(simulation, BaselineStep);

            var perEnemy = simulation.Get("First").Stats.DamageTakenToHp;
            perEnemy.Should().BeGreaterThan(0);
            (perEnemy % 30).Should().Be(0, "every attack lands three hits of 10");
            simulation.Get("Second").Stats.DamageTakenToHp.Should().Be(perEnemy, "an area attack hits every enemy");
        }

        [TestCase(1.0)]
        [TestCase(7.0)]
        public void Regeneration_IsAllApplied_WhateverTheTimeStep(double step)
        {
            var baseline = RegenerationWithStep(BaselineStep);

            baseline.Fp.Should().Be(60, "two 30-second regen cycles");
            RegenerationWithStep(step).Should().Be(baseline);
        }

        private double AttackDamageWithStep(double step)
        {
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { TestCombatants.Striker("Striker", attacksPerSecond: 2f, damage: 10f) }, new[] { TestCombatants.Dummy("Target", true) }),
                RuleSets.Current(),
                _catalog);

            RunThrough(simulation, step);
            return simulation.Get("Target").Stats.DamageTakenToHp;
        }

        private (double Fp, double Stm) RegenerationWithStep(double step)
        {
            var regenerator = new CombatantSpec { Name = "Regenerator", MaxHp = 1000, MaxFp = 1000, FpRegenPer30Seconds = 30, MaxStm = 1000, StmRegenPer30Seconds = 30 };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { regenerator }, new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog);
            var spawned = simulation.Get("Regenerator");
            spawned.Fp = 0;
            spawned.Stm = 0;

            RunThrough(simulation, step);
            return (spawned.Fp, spawned.Stm);
        }

        // Steps so the last step processed is the one at EndSeconds, whatever the step size.
        private static void RunThrough(Simulation simulation, double step) =>
            SimulationClock.RunUntil(simulation, EndSeconds + step, step);
    }
}
