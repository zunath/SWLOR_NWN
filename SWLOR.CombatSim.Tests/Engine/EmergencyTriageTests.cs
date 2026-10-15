using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Effects;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class EmergencyTriageTests
    {
        private const int AllyMaxHp = 10_000;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [TestCase(0, 30f)]
        [TestCase(10, 40f)]
        public void LowHpDoubling_ReadsHpIncludingTemporaryHp(int shieldPercent, float hpPercentTheGameSees)
        {
            var triage = TestCombatants.Use("EmergencyTriage1", AbilityTarget.NamedAlly, targetName: "Ally", minIntervalSeconds: 600);
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[] { TestCombatants.Medic("Medic", triage), new CombatantSpec { Name = "Ally", MaxHp = AllyMaxHp } },
                    new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.PreTuning(),
                _catalog);
            var ally = simulation.Get("Ally");
            ally.Hp = AllyMaxHp * 0.30;
            if (shieldPercent > 0)
                ally.TemporaryHp.Add(new TemporaryHpPool { Key = "Existing", Amount = AllyMaxHp * shieldPercent / 100.0, ExpiresAt = 600 });

            var medic = simulation.Get("Medic");
            SimulationClock.RunUntil(simulation, () => medic.Stats.AbilitiesUsed == 1, 10, "Emergency Triage resolved");

            EffectCatalog.TryGet(FeatType.EmergencyTriage1, out var effect).Should().BeTrue();
            var expected = HealingMath.DirectHeal(effect, AllyMaxHp, hpPercentTheGameSees, false, 26, 10,
                RuleSets.PreTuning().ForceAffinityHealingCap, 0f, 0f);
            ally.Stats.HealingReceived.Should().Be(expected);
        }
    }
}
