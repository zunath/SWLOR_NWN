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

        [Test]
        public void KoltoMist_ResumesHealingOnceItsCasterIsRevived()
        {
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[]
                    {
                        TestCombatants.Medic("Medic", TestCombatants.Use("KoltoMist1", AbilityTarget.Self, minIntervalSeconds: 600)),
                        TestCombatants.Medic("Reviver", TestCombatants.Use("Resuscitation1", AbilityTarget.DeadAlly)),
                        new CombatantSpec { Name = "Wounded", MaxHp = 1000 }
                    },
                    new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog);
            var medic = simulation.Get("Medic");
            var wounded = simulation.Get("Wounded");
            wounded.Hp = 100;

            SimulationClock.RunUntil(simulation, () => medic.PartyPulses.ContainsKey("KoltoMist"), 10, "the zone placed");
            simulation.ApplyDamage(simulation.Get("Enemy"), medic, TestCombatants.EffectivelyUnlimited);
            var ticksAtDeath = medic.PartyPulses["KoltoMist"].TicksLeft;
            var hpAtDeath = wounded.Hp;

            SimulationClock.RunUntil(simulation, () => !medic.IsDead, 15, "the medic revived");
            wounded.Hp.Should().Be(hpAtDeath, "pulses while the caster is dead heal no one");
            medic.PartyPulses["KoltoMist"].TicksLeft.Should().BeLessThan(ticksAtDeath, "those pulses are spent, not saved");

            var before = wounded.Hp;
            SimulationClock.RunUntil(simulation, simulation.Now + 10);
            (wounded.Hp - before).Should().BeGreaterThan(0, "the zone's remaining pulses land once its caster is alive again");
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
