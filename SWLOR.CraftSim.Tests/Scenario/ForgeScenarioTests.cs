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
            found.SecondsPerAction.Should().Be(CraftMinigameXp.ActionSeconds, "the gates model the pace the server holds a craft's XP to");
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
        public void TheStylePairsComparePlainStylesAndFullLines_AndEachStyleCarriesItsLoadout()
        {
            var pairs = new ForgeScenario().StylePairs;

            pairs.Should().HaveCount(2).And.OnlyContain(pair => pair.MinimumRank == ForgePerkRules.StyleRank);
            pairs.Select(pair => pair.Name).Should().Equal(
                "Hot Forging (no lines) vs Cold Working (no lines)", "Hot Forging (both lines) vs Cold Working (both lines)");
            ((ForgeLoadout)pairs[0].First.Configuration).Should().Be(new ForgeLoadout(ForgeStyle.HotForging, false, false));
            ((ForgeLoadout)pairs[1].Second.Configuration).Should().Be(new ForgeLoadout(ForgeStyle.ColdWorking, true, true));
        }

        [Test]
        public void AStyleBuildsTheEngineWithThePerksItsRankAllows()
        {
            var recipe = Recipe();
            var gear = GearLevel.None(SkillType.Smithery);
            var hot = new ForgeLoadout(ForgeStyle.HotForging, true, true).ToStyle("Hot Forging (both lines)");

            var plain = (ForgeEngine)Scenario.CreateEngine(new CraftEngineRequest(recipe, 30, gear, null, 1, new SeededMinigameRandom(1)));
            var early = (ForgeEngine)Scenario.CreateEngine(new CraftEngineRequest(recipe, 4, gear, hot, 1, new SeededMinigameRandom(1)));
            var perked = (ForgeEngine)Scenario.CreateEngine(new CraftEngineRequest(recipe, 30, gear, hot, 1, new SeededMinigameRandom(1)));
            var full = (ForgeEngine)Scenario.CreateEngine(new CraftEngineRequest(recipe, 50, gear, hot, 1, new SeededMinigameRandom(1)));

            plain.Setup.Perks.Should().Be(ForgePerkStats.None, "no style means no perks");
            early.Setup.Perks.Should().Be(ForgePerkStats.None, "no purchase opens before Smithery 5");
            perked.Setup.Perks.RangeShift.Should().Be(ForgePerkRules.HotForgingRangeShift, "Hot Forging is open from rank 20");
            perked.Setup.Perks.OverdrivePressPercent.Should().BePositive("Overdrive Press opens at 15");
            perked.Setup.Perks.RedLineCharges.Should().Be(0, "Red Line is the rank 50 capstone");
            full.Setup.Perks.RedLineCharges.Should().Be(1);
            full.Setup.Perks.PerfectTemperPercent.Should().BePositive();
            perked.Actions.Should().Contain(action => action.Id == ForgeActionIds.OverdrivePress);
            plain.Actions.Should().NotContain(action => action.Id == ForgeActionIds.OverdrivePress);
        }

        [TestCase("forge-forgemaster", ForgeStyle.HotForging, true, false)]
        [TestCase("forge-artisan", ForgeStyle.ColdWorking, false, true)]
        [TestCase("forge-both", ForgeStyle.HotForging, true, true)]
        [TestCase("forge-both-cold", ForgeStyle.ColdWorking, true, true)]
        [TestCase("forge-hot", ForgeStyle.HotForging, false, false)]
        [TestCase("forge-cold", ForgeStyle.ColdWorking, false, false)]
        public void ThePerkedScenariosPlayTheirLoadoutWithoutAStyleAndOfferNoStylePairs(string name, ForgeStyle style, bool forgemaster, bool artisan)
        {
            var scenario = ScenarioRegistry.Discover().Find(name);
            var engine = (ForgeEngine)scenario.CreateEngine(
                new CraftEngineRequest(Recipe(), 50, GearLevel.None(SkillType.Smithery), null, 1, new SeededMinigameRandom(1)));

            scenario.StylePairs.Should().BeEmpty();
            scenario.Skill.Should().Be(SkillType.Smithery);
            engine.Setup.Perks.Should().Be(new ForgeLoadout(style, forgemaster, artisan).StatsAt(50));
            (engine.Setup.Perks.RedLineCharges > 0).Should().Be(forgemaster);
            (engine.Setup.Perks.PerfectTemperPercent > 0).Should().Be(artisan);
        }

        [TestCase("forge-forgemaster")]
        [TestCase("forge-artisan")]
        [TestCase("forge-both")]
        [TestCase("forge-both-cold")]
        [TestCase("forge-hot")]
        [TestCase("forge-cold")]
        public void AnyPerkedBuildKeepsTheEightActionFloor_WhileTheUnperkedForgeKeepsTen(string name)
        {
            ScenarioRegistry.Discover().Find(name).LengthTarget.Should().Be(LengthTarget.PerkedItemCraft);
            ScenarioRegistry.Discover().Find("forge").LengthTarget.Should().Be(LengthTarget.ItemCraft);
        }

        [Test]
        public void ThePerkedScenariosSayWhichLoadoutTheyPlay_AndThePlainOneDoesNot()
        {
            var both = ScenarioRegistry.Discover().Find("forge-both");
            var artisan = ScenarioRegistry.Discover().Find("forge-artisan");
            var plain = ScenarioRegistry.Discover().Find("forge");

            both.Limitations.Should().Contain(
                "Every craft in this scenario is played by a crafter who bought both perk lines in full (every purchase their Smithery rank allows) and plays Hot Forging, so no gate here is an unperked result.");
            artisan.Limitations.Should().Contain(line => line.Contains("the Artisan line in full") && line.Contains("plays Cold Working"));
            plain.Limitations.Should().NotContain(line => line.Contains("Every craft in this scenario"));
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
