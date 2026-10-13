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
    public class CasterDeathTests
    {
        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void ForceSanctuary_KeepsHealingAfterItsCasterDies()
        {
            HealingAfterCasterDeath("ForceSanctuary1", "ForceSanctuary").Should().BeGreaterThan(0,
                "the game's Force Sanctuary pulses have no caster-alive check");
        }

        [Test]
        public void KoltoMist_StopsHealingWhenItsCasterDies()
        {
            HealingAfterCasterDeath("KoltoMist1", "KoltoMist").Should().Be(0,
                "the game's Kolto Mist pulses stop once the caster is dead");
        }

        private double HealingAfterCasterDeath(string feat, string pulseKey)
        {
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[] { TestCombatants.Medic("Medic", TestCombatants.Use(feat, AbilityTarget.Self)), new CombatantSpec { Name = "Wounded", MaxHp = 1000 } },
                    new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog);
            var medic = simulation.Get("Medic");
            var wounded = simulation.Get("Wounded");
            wounded.Hp = 100;

            SimulationClock.RunUntil(simulation, () => medic.PartyPulses.ContainsKey(pulseKey), 10, "the zone placed");
            simulation.ApplyDamage(simulation.Get("Enemy"), medic, TestCombatants.EffectivelyUnlimited);
            medic.IsDead.Should().BeTrue();

            var before = wounded.Hp;
            SimulationClock.RunUntil(simulation, simulation.Now + 10);
            return wounded.Hp - before;
        }
    }
}
