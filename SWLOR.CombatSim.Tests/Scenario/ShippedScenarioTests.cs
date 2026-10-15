using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;

namespace SWLOR.CombatSim.Tests.Scenario
{
    [TestFixture]
    public class ShippedScenarioTests
    {
        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        private static IEnumerable<string> ScenarioFiles()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "Scenarios");
            return Directory.Exists(directory)
                ? Directory.EnumerateFiles(directory, "*.json").OrderBy(f => f).ToList()
                : new List<string>();
        }

        [Test]
        public void TheScenarioFolderIsCopiedNextToTheTests_AndHoldsAtLeastOneScenario()
        {
            ScenarioFiles().Should().NotBeEmpty();
        }

        [TestCaseSource(nameof(ScenarioFiles))]
        public void NightmareField_DealsForceDamage_AsInTheGame(string path)
        {
            var profiles = ScenarioSpec.Load(path).Teams
                .SelectMany(team => team.Members)
                .SelectMany(member => member.Abilities)
                .Where(use => use.Feat.StartsWith("NightmareField") && use.Damage != null)
                .Select(use => use.Damage.Type);

            profiles.Should().OnlyContain(type => type == DamageType.Force);
        }

        [TestCaseSource(nameof(ScenarioFiles))]
        public void EveryShippedScenario_LoadsSpawnsAndRunsUnderEveryRuleSet(string path)
        {
            foreach (var ruleSetName in RuleSets.Names)
            {
                var scenario = ScenarioSpec.Load(path);
                scenario.DurationSeconds = 120f;

                var result = new Simulation(scenario, RuleSets.Get(ruleSetName), _catalog, 1).Run();

                result.Combatants.Should().NotBeEmpty($"{Path.GetFileName(path)} under {ruleSetName}");
                result.EndedAt.Should().BeInRange(0.0, 120.0 + 1e-6);
            }
        }
    }
}
