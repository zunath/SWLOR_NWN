using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Scenario.Fabricator;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Scenario
{
    [TestFixture]
    public class FabricatorScenarioTests
    {
        private static RecipeEntry Recipe(int level) =>
            SharedData.Recipes.ForSkill(SkillType.Fabrication).First(recipe => recipe.Level >= level);

        private static CraftEngineRequest Request(RecipeEntry recipe, GearLevel gear, int rank, int seed = 7) =>
            new(recipe, rank, gear, null, seed, SimMinigameRandom.ForRun(seed, 1, (int)recipe.Type));

        [Test]
        public void TheScenarioIsFoundByNameAndDescribesTheFabricator()
        {
            var scenario = ScenarioRegistry.Discover().Find("fabricator");

            scenario.Should().BeOfType<FabricatorScenario>();
            scenario.Skill.Should().Be(SkillType.Fabrication);
            scenario.Minigame.Should().Be(CraftMinigameType.Fabricator);
            scenario.SecondsPerAction.Should().Be(8.0, "the starting estimate until client timing replaces it");
            scenario.LengthTarget.Should().Be(LengthTarget.ItemCraft);
            scenario.StylePairs.Select(pair => pair.Name).Should().Equal(
                "Production Run vs Fine Tolerance", "Production Run (full line) vs Fine Tolerance (full line)");
            scenario.StylePairs.Should().OnlyContain(pair => pair.MinimumRank == 20, "the styles open at Fabrication 20");
            scenario.Enhancements.Should().Be(EnhancementSupport.Modeled);
            scenario.XpMultiplierFor(5).Should().Be(FabricatorRules.XpMultiplierLevels1To10);
            scenario.XpMultiplierFor(45).Should().Be(FabricatorRules.XpMultiplierLevels41Up);
            scenario.AdaptiveOptions.Heuristic.Should().BeOfType<FabricatorLeafHeuristic>();
            scenario.Cohort.Recipes(SharedData.Recipes).Should().OnlyContain(recipe => recipe.Skill == SkillType.Fabrication);
        }

        [Test]
        public void TheEngineIsBuiltFromTheRealRecipe_ItsParametersAndTheGear()
        {
            var scenario = new FabricatorScenario();
            var recipe = Recipe(25);
            var gear = SharedData.Gear.Levels(SkillType.Fabrication).Last();

            var engine = (FabricatorEngine)scenario.CreateEngine(Request(recipe, gear, 25));

            engine.Turn.Should().Be(0);
            engine.Status.Should().Be(MinigameStatus.InProgress);
            engine.Setup.Parameters.Should().BeSameAs(recipe.Detail.MinigameParameters);
            engine.Setup.Craftsmanship.Should().Be(gear.Craftsmanship);
            engine.Setup.Control.Should().Be(gear.Control);
            engine.Setup.GearCp.Should().Be(gear.CpBonus);
            engine.Setup.Rank.Should().Be(25);
            engine.Setup.RecipeLevel.Should().Be(recipe.Level);
            engine.Meters.Cp.Max.Should().Be(engine.Setup.MaxCp);
        }

        [Test]
        public void TheRequestsEnhancementPenaltyRaisesTheProgressEverySubAssemblyNeeds_InProportionToItsSize()
        {
            var scenario = new FabricatorScenario();
            var recipe = Recipe(25);
            var gear = SharedData.Gear.Levels(SkillType.Fabrication).Last();
            var plain = (FabricatorEngine)scenario.CreateEngine(Request(recipe, gear, 25));

            var request = new CraftEngineRequest(
                recipe, 25, gear, null, 7, SimMinigameRandom.ForRun(7, 1, (int)recipe.Type), new CraftEnhancementConfiguration(3, 12));
            var enhanced = (FabricatorEngine)scenario.CreateEngine(request);

            enhanced.Setup.EnhancementProgressPenalty.Should().Be(36);
            enhanced.Meters.Progress.Max.Should().Be(plain.Meters.Progress.Max + 36 * FabricatorRules.ProgressScale);
            foreach (var part in FabricatorHeads.AllParts)
                enhanced.Setup.SizeOf(part).Should().BeGreaterThan(plain.Setup.SizeOf(part));
            enhanced.Setup.RunGain(true, false).Should().Be(plain.Setup.RunGain(true, false), "an enhancement adds Runs, it does not weaken one");
        }

        [Test]
        public void TheEngineUsesTheRequestsSourceOfChance_SoAnAttemptRepeatsFromItsSeed()
        {
            var scenario = new FabricatorScenario();
            var recipe = Recipe(30);
            var gear = SharedData.Gear.Levels(SkillType.Fabrication)[1];

            string Line(int seed)
            {
                var engine = scenario.CreateEngine(Request(recipe, gear, 30, seed));
                var policy = new AdaptivePolicy(scenario.AdaptiveOptions, seed);
                var result = CraftPlayer.Play(engine, policy);
                return $"{result.Status} {result.Actions} {result.QualityFraction:0.000}";
            }

            Line(3).Should().Be(Line(3));
        }

        [Test]
        public void AHandWrittenRecipeWithoutParametersPlaysWithTheFallback()
        {
            var entry = new RecipeEntry(
                RecipeType.Invalid, SkillType.Fabrication, 10, "x", RecipeCategoryType.Bed, new Dictionary<string, int> { ["wood"] = 1 });
            var gear = GearLevel.None(SkillType.Fabrication);

            var engine = (FabricatorEngine)new FabricatorScenario().CreateEngine(Request(entry, gear, 10));

            engine.Setup.Parameters.Should().BeSameAs(FabricatorParameters.Fallback);
        }

        [TestCase(5, 0)]
        [TestCase(25, 1)]
        [TestCase(45, 2)]
        public void TheAdaptivePlayerMakesAnItemOfARealRecipe_AtEveryGearLevel(int level, int gearIndex)
        {
            var scenario = new FabricatorScenario();
            var recipe = Recipe(level);
            var gear = SharedData.Gear.Levels(SkillType.Fabrication)[gearIndex];
            var engine = scenario.CreateEngine(Request(recipe, gear, Math.Min(recipe.Level, 50)));

            var result = CraftPlayer.Play(engine, new AdaptivePolicy(scenario.AdaptiveOptions, 11));

            result.Succeeded.Should().BeTrue("the adaptive player finishes with Assemble: it neither dithers nor breaks the machine");
            result.Actions.Should().BeInRange(8, 40);
            result.QualityFraction.Should().BeGreaterThan(0.3);
        }

        [Test]
        public void TheLeafHeuristicValuesAFinishedItemAboveTheSameItemStillToBeAssembled()
        {
            var heuristic = new FabricatorLeafHeuristic();
            var meters = new MinigameMeters(new MinigameMeter(100, 100), new MinigameMeter(80, 100), new MinigameMeter(50, 80), new MinigameMeter(10, 20));

            var finished = heuristic.Evaluate(new LeafPosition(meters, MinigameStatus.Succeeded, FinishEstimate.None));
            var unassembled = heuristic.Evaluate(new LeafPosition(meters, MinigameStatus.InProgress, FinishEstimate.None));
            var broken = heuristic.Evaluate(new LeafPosition(meters, MinigameStatus.Failed, FinishEstimate.None));

            finished.Should().BeGreaterThan(unassembled);
            unassembled.Should().BeGreaterThan(broken);
            broken.Should().Be(0);
            DefaultLeafHeuristic.Instance.Evaluate(new LeafPosition(meters, MinigameStatus.InProgress, FinishEstimate.None))
                .Should().BeGreaterThan(DefaultLeafHeuristic.Instance.Evaluate(new LeafPosition(meters, MinigameStatus.Succeeded, FinishEstimate.None)),
                    "the generic heuristic would rank the unassembled item higher, which is why the Fabricator needs its own");
        }
    }
}
