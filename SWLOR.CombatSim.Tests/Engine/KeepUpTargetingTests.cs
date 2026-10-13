using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Effects;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class KeepUpTargetingTests
    {
        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void LowestAllyKeepUp_MovesOnToTheLowestAllyWithoutTheEffect()
        {
            var renewal = TestCombatants.Use("Renewal1", AbilityTarget.LowestAlly, keepUp: true);
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[]
                    {
                        TestCombatants.Medic("Medic", renewal),
                        new CombatantSpec { Name = "Lowest", MaxHp = 1000 },
                        new CombatantSpec { Name = "Next", MaxHp = 1000 }
                    },
                    new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog);
            simulation.Get("Lowest").Hp = 200;
            simulation.Get("Next").Hp = 600;
            EffectCatalog.TryGet(FeatType.Renewal1, out var effect).Should().BeTrue();

            var medic = simulation.Get("Medic");
            SimulationClock.RunUntil(simulation, () => medic.Stats.AbilitiesUsed == 2, 40, "a second Renewal");

            simulation.Get("Lowest").HealsOverTime.Should().ContainKey(effect.Key);
            simulation.Get("Next").HealsOverTime.Should().ContainKey(effect.Key,
                "the lowest ally is already covered, so the next lowest gets the second cast");
        }
    }
}
