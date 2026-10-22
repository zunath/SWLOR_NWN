using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Scenario.Circuit;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Scenario
{
    [TestFixture]
    public class CircuitScenarioTests
    {
        private static RecipeEntry Recipe(int level) =>
            SharedData.Recipes.ForSkill(SkillType.Engineering).First(recipe => recipe.Level == level && recipe.Detail?.MinigameParameters is CircuitParameters);

        [Test]
        public void TheCommandLineFindsTheScenarioByName()
        {
            var scenario = ScenarioRegistry.Discover().Find("circuit");

            scenario.Should().BeOfType<CircuitScenario>();
            scenario.Skill.Should().Be(SkillType.Engineering);
            scenario.Minigame.Should().Be(CraftMinigameType.Circuit);
            scenario.SecondsPerAction.Should().Be(8.0, "the starting estimate for the new window until the client evidence measures it");
            ((ICraftScenario)scenario).XpMultiplierFor(5).Should().Be(CircuitRules.XpMultiplierFor(5), "the scenario plays the multiplier the game pays");
            scenario.LengthTarget.Should().Be(LengthTarget.ItemCraft);
            scenario.StylePairs.Should().BeEmpty("the Circuit has no styles until its perks exist");
        }

        [Test]
        public void ItsCohortIsEveryActiveEngineeringRecipeAtItsOwnLevel()
        {
            var scenario = new CircuitScenario();
            var cohort = ((ICraftScenario)scenario).Cohort;

            var recipes = cohort.Recipes(SharedData.Recipes);

            recipes.Should().NotBeEmpty().And.OnlyContain(recipe => recipe.Skill == SkillType.Engineering && recipe.Level >= 1);
            cohort.RankFor(Recipe(30)).Should().Be(30);
            cohort.RankFor(recipes.Last()).Should().BeLessThanOrEqualTo(50);
        }

        [Test]
        public void TheEngineIsBuiltFromTheRecipesOwnParametersTheLevelChartTheRankAndTheGear()
        {
            var recipe = Recipe(30);
            var parameters = (CircuitParameters)recipe.Detail.MinigameParameters;
            var level = Craft.GetRecipeLevelDetail(30);
            var gear = SharedData.Gear.Levels(SkillType.Engineering).Last();
            var request = new CraftEngineRequest(recipe, 30, gear, null, 1, new SeededMinigameRandom(1));

            var engine = (CircuitEngine)new CircuitScenario().CreateEngine(request);

            var expected = new CircuitSetup(
                parameters, 30, level.Progress, level.Quality, level.Durability, 30, gear.Craftsmanship, gear.Control, gear.CpBonus);
            engine.Setup.MaxAssembly.Should().Be(expected.MaxAssembly);
            engine.Setup.MaxCalibration.Should().Be(expected.MaxCalibration);
            engine.Setup.MaxCp.Should().Be(expected.MaxCp).And.BeGreaterThan(0);
            engine.Setup.OverloadLine.Should().Be(parameters.OverloadLine);
            engine.Status.Should().Be(MinigameStatus.InProgress);
            engine.Turn.Should().Be(0);
            gear.CpBonus.Should().BeGreaterThan(0, "the best gear adds CP");
        }

        [Test]
        public void TwoEnginesBuiltForTheSameRequestPlayTheSameAttempt()
        {
            var request = new CraftEngineRequest(Recipe(20), 20, GearLevel.None(SkillType.Engineering), null, 1, new SeededMinigameRandom(7));
            var other = new CraftEngineRequest(Recipe(20), 20, GearLevel.None(SkillType.Engineering), null, 1, new SeededMinigameRandom(7));
            var first = new CircuitScenario().CreateEngine(request);
            var second = new CircuitScenario().CreateEngine(other);

            foreach (var action in new[] { CircuitActionIds.Wire, CircuitActionIds.Tune, CircuitActionIds.Wire, CircuitActionIds.DischargeCalibration })
            {
                first.Perform(action);
                second.Perform(action);
            }

            first.DescribeMeters().Should().Equal(second.DescribeMeters());
            first.NextOpportunity.Should().Be(second.NextOpportunity);
        }
    }
}
