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
    public class DamageReductionTests
    {
        private const double Hit = 100;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void PainSuppressant_CutsAllDamageTakenUntilItEnds()
        {
            var simulation = Spawn(TestCombatants.Use("PainSuppressant2", AbilityTarget.NamedAlly, targetName: "Ally", minIntervalSeconds: 600));
            var ally = simulation.Get("Ally");
            CastOnce(simulation);

            DamageTaken(simulation, ally, DamageType.Physical).Should().BeApproximately(85, 1e-6);
            DamageTaken(simulation, ally, DamageType.Other).Should().BeApproximately(85, 1e-6);

            SimulationClock.RunUntil(simulation, simulation.Now + 31);
            DamageTaken(simulation, ally, DamageType.Physical).Should().BeApproximately(Hit, 1e-6, "the reduction ends with the effect");
        }

        [TestCase(30, 75)]
        [TestCase(50, Hit)]
        public void RousingShout_CutsPhysicalAndForceDamageOnlyForATargetInDanger(int hpPercent, double expected)
        {
            var simulation = Spawn(TestCombatants.Use("RousingShout3", AbilityTarget.NamedAlly, targetName: "Ally", minIntervalSeconds: 600));
            var ally = simulation.Get("Ally");
            ally.Hp = ally.Spec.MaxHp * hpPercent / 100.0;
            CastOnce(simulation);

            DamageTaken(simulation, ally, DamageType.Physical).Should().BeApproximately(expected, 1e-6);
            DamageTaken(simulation, ally, DamageType.Force).Should().BeApproximately(expected, 1e-6);
            DamageTaken(simulation, ally, DamageType.Other).Should().BeApproximately(Hit, 1e-6, "other damage types are not reduced");
        }

        [Test]
        public void ForceSanctuary_CutsOnlyForceDamageWhileItPulses()
        {
            var simulation = Spawn(TestCombatants.Use("ForceSanctuary1", AbilityTarget.Self, minIntervalSeconds: 600));
            var ally = simulation.Get("Ally");
            SimulationClock.RunUntil(simulation, () => ally.DamageReductions.ContainsKey("ForceSanctuary"), 10, "a Sanctuary pulse reached the ally");

            DamageTaken(simulation, ally, DamageType.Force).Should().BeApproximately(95, 1e-6);
            DamageTaken(simulation, ally, DamageType.Physical).Should().BeApproximately(Hit, 1e-6);
        }

        private Simulation Spawn(AbilityUseSpec use)
        {
            var ally = new CombatantSpec { Name = "Ally", MaxHp = TestCombatants.EffectivelyUnlimited };
            return TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { TestCombatants.Medic("Medic", use), ally }, new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog);
        }

        private static void CastOnce(Simulation simulation)
        {
            var medic = simulation.Get("Medic");
            SimulationClock.RunUntil(simulation, () => medic.Stats.AbilitiesUsed == 1, 10, "the ability resolved");
        }

        /// <summary>Damage one hit does after reductions, counting what temporary HP absorbed.</summary>
        private static double DamageTaken(Simulation simulation, Combatant target, DamageType type)
        {
            var before = target.Stats.DamageTakenToHp + target.Stats.DamageAbsorbedByTemporaryHp;
            simulation.ApplyDamage(simulation.Get("Enemy"), target, Hit, type);
            return target.Stats.DamageTakenToHp + target.Stats.DamageAbsorbedByTemporaryHp - before;
        }
    }
}
