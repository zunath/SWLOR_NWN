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

        [TestCase("hitChance", 2f)]
        [TestCase("critChance", -0.1f)]
        [TestCase("critMultiplier", 0.5f)]
        [TestCase("min", float.NaN)]
        [TestCase("max", -1f)]
        [TestCase("hits", 0f)]
        public void Spawn_RejectsAnAbilityDamageProfileOutOfRange(string field, float value)
        {
            var strike = TestCombatants.Use("ShatteringStrike1", AbilityTarget.Enemy);
            strike.Damage = new DamageSpec { Min = 1, Max = 2 };
            switch (field)
            {
                case "hitChance": strike.Damage.HitChance = value; break;
                case "critChance": strike.Damage.CritChance = value; break;
                case "critMultiplier": strike.Damage.CritMultiplier = value; break;
                case "min": strike.Damage.Min = value; break;
                case "max": strike.Damage.Max = value; break;
                case "hits": strike.Damage.Hits = (int)value; break;
            }

            var act = () => new Simulation(Scenario(TestCombatants.Medic("Medic", strike)), RuleSets.Current(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*ShatteringStrike1*damage profile*");
        }

        [Test]
        public void Spawn_RejectsAnAutoattackProfileOutOfRange()
        {
            var striker = TestCombatants.Striker("Striker");
            striker.AutoAttack.Damage.HitChance = 1.5f;

            var act = () => new Simulation(Scenario(striker), RuleSets.Current(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Striker*autoattack*HitChance*");
        }

        [Test]
        public void Spawn_RejectsATimeStepLongerThanAnAbilityUsersReactionTime()
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use("Benevolence1", AbilityTarget.Self));
            medic.ReactionSeconds = 0.5f;
            var scenario = Scenario(medic);
            scenario.TimeStepSeconds = 1f;

            var act = () => new Simulation(scenario, RuleSets.Current(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Medic*ReactionSeconds*time step*");
        }

        [Test]
        public void Spawn_AllowsALongTimeStepForCombatantsWithoutAbilities()
        {
            var striker = TestCombatants.Striker("Striker");
            striker.ReactionSeconds = 0.5f;
            var scenario = Scenario(striker);
            scenario.TimeStepSeconds = 3f;

            var act = () => new Simulation(scenario, RuleSets.Current(), _catalog, 1).Spawn();

            act.Should().NotThrow("only ability decisions depend on the reaction time");
        }

        private static ScenarioSpec Scenario(CombatantSpec player) =>
            TestScenarios.TwoTeams(false, new[] { player }, new[] { TestCombatants.Dummy("Enemy", true) });
    }
}
