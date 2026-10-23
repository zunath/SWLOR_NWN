using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Scenario.Forge;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Scenario
{
    [TestFixture]
    public class ForgeScenarioTests
    {
        private static readonly ICraftScenario Scenario = new ForgeScenario();

        private static RecipeEntry Recipe(SkillType skill = SkillType.Smithery) =>
            SharedData.Recipes.Recipes.First(recipe => recipe.Skill == skill && recipe.Level >= 30 && recipe.Detail?.MinigameParameters is ForgeParameters);

        private static CraftEngineRequest Request(RecipeEntry recipe, int rank, GearLevel gear) =>
            new(recipe, rank, gear, null, 1, new ScriptedRandom(Enumerable.Repeat(100, 50).ToArray()));

        [Test]
        public void TheScenarioIsFoundByNameAndPlaysSmitheryInThePlasmaForge()
        {
            var found = ScenarioRegistry.Discover().Find("forge");

            found.Should().BeOfType<ForgeScenario>();
            found.Skill.Should().Be(SkillType.Smithery);
            found.Minigame.Should().Be(CraftMinigameType.Forge);
            found.SecondsPerAction.Should().Be(8.0, "the starting estimate for a window nobody has played");
            found.LengthTarget.Should().Be(LengthTarget.ItemCraft);
        }

        [Test]
        public void TheScenariosXpMultiplierIsTheOneTheGameRouteApplies()
        {
            Scenario.XpMultiplier.Should().Be(ForgeRules.XpMultiplierLevels1To10, "the lowest band's, the largest");
            Scenario.XpMultiplierFor(5).Should().Be(ForgeRules.XpMultiplierLevels1To10);
            Scenario.XpMultiplierFor(45).Should().Be(ForgeRules.XpMultiplierLevels41Up);
            Scenario.XpMultiplierFor(25).Should().Be(CraftMinigameXp.MultiplierFor(CraftMinigameType.Forge, 25));
        }

        [Test]
        public void TheEngineIsBuiltFromTheRecipesOwnParametersTheRankAndTheGear()
        {
            var recipe = Recipe();
            var parameters = (ForgeParameters)recipe.Detail.MinigameParameters;
            var gear = SharedData.Gear.Levels(SkillType.Smithery).Last();

            var engine = (ForgeEngine)Scenario.CreateEngine(Request(recipe, 40, gear));

            engine.Setup.Parameters.Should().BeSameAs(parameters);
            engine.Setup.Rank.Should().Be(40);
            engine.Setup.RecipeLevel.Should().Be(recipe.Level);
            engine.Setup.Craftsmanship.Should().Be(gear.Craftsmanship);
            engine.Setup.Control.Should().Be(gear.Control);
            engine.Setup.GearCp.Should().Be(gear.CpBonus);
            engine.Meters.Cp.Max.Should().Be(ForgeRules.MaxCp(gear.CpBonus, 40));
            engine.Thermal.Should().Be(parameters.StartingThermal);
        }

        [Test]
        public void TheScenarioModelsEnhancementsThroughTheEnginesShapeRequirement()
        {
            Scenario.Enhancements.Should().Be(EnhancementSupport.Modeled);

            var recipe = Recipe();
            var gear = GearLevel.None(SkillType.Smithery);
            var penalty = new CraftEnhancementConfiguration(2, 40);
            var plain = (ForgeEngine)Scenario.CreateEngine(Request(recipe, 40, gear));
            var enhanced = (ForgeEngine)Scenario.CreateEngine(
                new CraftEngineRequest(recipe, 40, gear, null, 1, new ScriptedRandom(100), penalty));

            plain.Setup.EnhancementShapePercent.Should().Be(0);
            enhanced.Setup.EnhancementShapePercent.Should().Be(ForgeRules.EnhancementShapePercent(80, recipe.Level)).And.BePositive();
            enhanced.Meters.Progress.Max.Should().BeGreaterThan(plain.Meters.Progress.Max);
        }

        [Test]
        public void TheEngineForksSoTheGatesCanSimulateIt()
        {
            var engine = Scenario.CreateEngine(Request(Recipe(), 40, GearLevel.None(SkillType.Smithery)));
            var fork = engine.Fork(new ScriptedRandom(100, 100));

            fork.Perform(ForgeActionIds.HydroPress).Performed.Should().BeTrue();

            engine.Turn.Should().Be(0, "an action on the copy leaves the original attempt alone");
            fork.Turn.Should().Be(1);
        }

        [Test]
        public void ARecipeWithoutForgeParametersIsRefused()
        {
            var recipe = new RecipeEntry(
                (RecipeType)95999, SkillType.Smithery, 10, "r", RecipeCategoryType.Cloak, new Dictionary<string, int> { ["a"] = 1 });

            var act = () => Scenario.CreateEngine(Request(recipe, 10, GearLevel.None(SkillType.Smithery)));

            act.Should().Throw<InvalidOperationException>().WithMessage("*Plasma Forge parameters*");
        }

        [Test]
        public void EverySmitheryRecipeInTheCohortHasParameters()
        {
            var recipes = Scenario.Cohort.Recipes(SharedData.Recipes);

            recipes.Should().NotBeEmpty();
            recipes.Should().OnlyContain(recipe => recipe.Detail.MinigameParameters is ForgeParameters);
        }
    }
}
