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
                RuleSets.PreTuning(),
                _catalog);
            var medic = simulation.Get("Medic");
            var wounded = simulation.Get("Wounded");
            wounded.Hp = 100;

            SimulationClock.RunUntil(simulation, () => medic.GetZone("KoltoMist") != null, 10, "the zone placed");
            simulation.ApplyDamage(simulation.Get("Enemy"), medic, TestCombatants.EffectivelyUnlimited);
            var ticksAtDeath = medic.GetZone("KoltoMist").TicksLeft;
            var hpAtDeath = wounded.Hp;

            SimulationClock.RunUntil(simulation, () => !medic.IsDead, 15, "the medic revived");
            wounded.Hp.Should().Be(hpAtDeath, "pulses while the caster is dead heal no one");
            medic.GetZone("KoltoMist").TicksLeft.Should().BeLessThan(ticksAtDeath, "those pulses are spent, not saved");

            var before = wounded.Hp;
            SimulationClock.RunUntil(simulation, simulation.Now + 10);
            (wounded.Hp - before).Should().BeGreaterThan(0, "the zone's remaining pulses land once its caster is alive again");
        }

        [Test]
        public void KoltoMist_RecastBeforeItsZoneEnds_KeepsBothZonesPulsing()
        {
            var rules = RuleSets.PreTuning();
            rules.RecastOverrides["KoltoMist"] = 3f;
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[]
                    {
                        TestCombatants.Medic("Medic", TestCombatants.Use("KoltoMist1", AbilityTarget.Self, minIntervalSeconds: 10)),
                        new CombatantSpec { Name = "Wounded", MaxHp = 1000 }
                    },
                    new[] { TestCombatants.Dummy("Enemy", true) }),
                rules,
                _catalog);
            simulation.Get("Wounded").Hp = 100;
            var medic = simulation.Get("Medic");

            SimulationClock.RunUntil(simulation, () => medic.Stats.AbilitiesUsed == 2, 20, "the second zone placed");

            medic.PartyPulses.Count(pulse => pulse.Effect.Key == "KoltoMist").Should().Be(2,
                "each cast schedules its own pulses, as the game's delayed callbacks do");
        }

        private double HealingAfterCasterDeath(string feat, string pulseKey)
        {
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[] { TestCombatants.Medic("Medic", TestCombatants.Use(feat, AbilityTarget.Self)), new CombatantSpec { Name = "Wounded", MaxHp = 1000 } },
                    new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.PreTuning(),
                _catalog);
            var medic = simulation.Get("Medic");
            var wounded = simulation.Get("Wounded");
            wounded.Hp = 100;

            SimulationClock.RunUntil(simulation, () => medic.GetZone(pulseKey) != null, 10, "the zone placed");
            simulation.ApplyDamage(simulation.Get("Enemy"), medic, TestCombatants.EffectivelyUnlimited);
            medic.IsDead.Should().BeTrue();

            var before = wounded.Hp;
            SimulationClock.RunUntil(simulation, simulation.Now + 10);
            return wounded.Hp - before;
        }
    }
}
