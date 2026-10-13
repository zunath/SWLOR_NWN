using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class SpawnValidationTests
    {
        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [TestCase(0f)]
        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        public void Run_RejectsATimeStepThatWouldNeverEnd(float step)
        {
            var scenario = Scenario(TestCombatants.Dummy("Player"));
            scenario.TimeStepSeconds = step;

            var act = () => new Simulation(scenario, RuleSets.Current(), _catalog, 1).Run();

            act.Should().Throw<InvalidDataException>().WithMessage("*TimeStepSeconds*");
        }

        [TestCase(0f)]
        [TestCase(-5f)]
        public void Run_RejectsANonPositiveDuration(float duration)
        {
            var scenario = Scenario(TestCombatants.Dummy("Player"));
            scenario.DurationSeconds = duration;

            var act = () => new Simulation(scenario, RuleSets.Current(), _catalog, 1).Run();

            act.Should().Throw<InvalidDataException>().WithMessage("*DurationSeconds*");
        }

        [Test]
        public void Spawn_RejectsAnAbilityWhoseRecastCouldNotBeRead()
        {
            var catalog = AbilityCatalog.Load();
            var anger = catalog.Get(FeatType.Anger1);
            catalog.Add(new AbilityInfo { Feat = anger.Feat, Name = anger.Name, RecastGroup = anger.RecastGroup, STM = anger.STM, UnreadableTiming = "recast" });
            var use = TestCombatants.Use("Anger1", AbilityTarget.Enemy);
            use.Damage = new DamageSpec { Min = 1f, Max = 1f };

            var act = () => new Simulation(Scenario(TestCombatants.Medic("Beast", use)), RuleSets.Current(), catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Anger1*recast*game engine*");
        }

        [Test]
        public void Spawn_RejectsTwoCombatantsWithTheSameNameOnOneTeam()
        {
            var scenario = TestScenarios.TwoTeams(false, new[] { TestCombatants.Dummy("Fighter"), TestCombatants.Dummy("Fighter") }, new[] { TestCombatants.Dummy("Enemy", true) });

            var act = () => new Simulation(scenario, RuleSets.Current(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*First*Fighter*");
        }

        [Test]
        public void Spawn_AllowsTheSameNameOnOpposingTeams()
        {
            var scenario = TestScenarios.TwoTeams(false, new[] { TestCombatants.Dummy("Fighter") }, new[] { TestCombatants.Dummy("Fighter") });

            var act = () => new Simulation(scenario, RuleSets.Current(), _catalog, 1).Spawn();

            act.Should().NotThrow();
        }

        [Test]
        public void Spawn_RejectsAnUnknownPerk()
        {
            var player = TestCombatants.Dummy("Player");
            player.Perks = new List<string> { "NotARealPerk" };

            var act = () => new Simulation(Scenario(player), RuleSets.Current(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*NotARealPerk*");
        }

        private static ScenarioSpec Scenario(CombatantSpec player) =>
            TestScenarios.TwoTeams(false, new[] { player }, new[] { TestCombatants.Dummy("Enemy", true) });
    }
}
