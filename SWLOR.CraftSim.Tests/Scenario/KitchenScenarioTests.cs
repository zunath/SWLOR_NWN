using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Scenario.Kitchen;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Scenario
{
    /// <summary>Covers the Kitchen's plug into the harness: the real engine from a real recipe, enhancements, and its leaf heuristic.</summary>
    public class KitchenScenarioTests
    {
        private static readonly ICraftScenario Scenario = new KitchenScenario();

        private static RecipeEntry Recipe(RecipeType type) =>
            SharedData.Recipes.Recipes.Single(recipe => recipe.Type == type);

        private static CraftEngineRequest Request(
            RecipeEntry recipe, int rank, GearLevel gear, int seed = 1, CraftEnhancementConfiguration enhancements = null) =>
            new(recipe, rank, gear, null, seed, SimMinigameRandom.ForRun(seed), enhancements);

        [Test]
        public void TheKitchenIsFoundByNameAndPlaysAgricultureAtEightSecondsAnAction()
        {
            ScenarioRegistry.Discover().Scenarios.Select(scenario => scenario.Name).Should().Contain("kitchen");
            Scenario.Skill.Should().Be(SkillType.Agriculture);
            Scenario.Minigame.Should().Be(CraftMinigameType.Kitchen);
            Scenario.SecondsPerAction.Should().Be(8);
            Scenario.Enhancements.Should().Be(EnhancementSupport.Modeled);
            Scenario.LengthTarget.Should().Be(LengthTarget.ItemCraft);
        }

        [Test]
        public void TheScenarioReportsTheMultiplierTheGameServerAppliesInEveryLevelBand()
        {
            foreach (var level in new[] { 1, 10, 11, 20, 21, 30, 31, 40, 41, 53 })
            {
                Scenario.XpMultiplierFor(level).Should().Be(CraftMinigameXp.MultiplierFor(CraftMinigameType.Kitchen, level));
                Scenario.XpMultiplierFor(level).Should().Be(KitchenTuning.XpMultiplierForLevel(level));
            }

            Scenario.XpMultiplier.Should().Be(KitchenTuning.XpMultiplierLevels1To10, "the lowest band's, the largest");
        }

        [Test]
        public void TheEngineIsTheRealKitchenBuiltFromTheRecipesStoredParameters()
        {
            var recipe = Recipe(RecipeType.MynockMeatBall);
            var gear = GearLevel.Fixed("test", SkillType.Agriculture, control: 13, craftsmanship: 12, cpBonus: 20);

            var engine = (KitchenEngine)Scenario.CreateEngine(Request(recipe, 20, gear));

            var parameters = (KitchenParameters)recipe.Detail.MinigameParameters;
            engine.Recipe.Target.InnerRadius.Should().Be(parameters.InnerRadius);
            engine.Recipe.Target.OuterRadius.Should().Be(parameters.OuterRadius);
            engine.Recipe.Freshness.Should().Be(parameters.Freshness);
            engine.Stats.Should().Be(new KitchenStats(20, 12, 13, 20));
            engine.Meters.Cp.Max.Should().Be(new KitchenStats(20, 12, 13, 20).MaxCp);
            engine.Status.Should().Be(MinigameStatus.InProgress);
            engine.Turn.Should().Be(0);
        }

        [Test]
        public void EveryAgricultureRecipeBuildsAnEngine()
        {
            var gear = GearLevel.None(SkillType.Agriculture);
            foreach (var recipe in Scenario.Cohort.Recipes(SharedData.Recipes))
            {
                var engine = Scenario.CreateEngine(Request(recipe, ScenarioCohort.AtLevelRank(recipe), gear));

                engine.Actions.Should().NotBeEmpty(recipe.Type.ToString());
            }
        }

        [Test]
        public void EnhancementsRaiseTheDonenessTheDishNeedsByHalfTheirProgressPenalty()
        {
            var recipe = Recipe(RecipeType.MynockMeatBall);
            var gear = GearLevel.None(SkillType.Agriculture);

            var plain = Scenario.CreateEngine(Request(recipe, 20, gear));
            var enhanced = Scenario.CreateEngine(Request(recipe, 20, gear, 1, new CraftEnhancementConfiguration(2, 30)));

            plain.Meters.Progress.Max.Should().Be(KitchenTuning.DonenessMax);
            enhanced.Meters.Progress.Max.Should().Be(KitchenTuning.DonenessMax + KitchenTuning.EnhancementDoneness(60));
            enhanced.Meters.Progress.Max.Should().BeGreaterThan(plain.Meters.Progress.Max);
        }

        [Test]
        public void ARecipeWithoutKitchenParametersIsRefused()
        {
            var bare = new RecipeEntry(
                (RecipeType)90001, SkillType.Agriculture, 5, "bare", RecipeCategoryType.Food,
                new Dictionary<string, int> { ["distilled_water"] = 1 });

            var act = () => Scenario.CreateEngine(Request(bare, 5, GearLevel.None(SkillType.Agriculture)));

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void TheLeafHeuristicCreditsNearnessToTheTargetWhileQualityIsStillZero()
        {
            var heuristic = new KitchenLeafHeuristic();
            var estimate = new FinishEstimate(new[] { new FinishRoute(12, 4, 0) });
            var meters = new MinigameMeters(new MinigameMeter(40, 100), new MinigameMeter(0, 100), new MinigameMeter(60, 100), new MinigameMeter(30, 100));

            double Value(int distance) => heuristic.Evaluate(new LeafPosition(
                meters, MinigameStatus.InProgress, estimate, new Dictionary<string, int> { [KitchenEngine.DistanceMeterId] = distance }));

            Value(20).Should().BeGreaterThan(Value(40));
            Value(40).Should().BeGreaterThan(Value(80));
            Value(70).Should().Be(Value(120), "nearness earns nothing beyond the far distance");
        }

        [Test]
        public void TheLeafHeuristicNeverCreditsMoreThanItsCapAndRealQualityTakesOver()
        {
            var heuristic = new KitchenLeafHeuristic(gradientCap: 0.35);
            var estimate = new FinishEstimate(new[] { new FinishRoute(12, 4, 0) });
            MinigameMeters Meters(int quality) =>
                new(new MinigameMeter(40, 100), new MinigameMeter(quality, 100), new MinigameMeter(60, 100), new MinigameMeter(30, 100));

            double Value(int quality, int distance) => heuristic.Evaluate(new LeafPosition(
                Meters(quality), MinigameStatus.InProgress, estimate, new Dictionary<string, int> { [KitchenEngine.DistanceMeterId] = distance }));

            Value(0, 0).Should().BeApproximately(Value(35, 0), 1e-9, "a perfect position is credited at the cap");
            Value(90, 5).Should().BeGreaterThan(Value(0, 5), "real quality beats the nearness credit");
        }

        [Test]
        public void TheLeafHeuristicScoresAnEndedAttemptLikeTheDefault()
        {
            var heuristic = new KitchenLeafHeuristic();
            var meters = new MinigameMeters(new MinigameMeter(100, 100), new MinigameMeter(70, 100), new MinigameMeter(10, 100), new MinigameMeter(0, 100));

            heuristic.Evaluate(new LeafPosition(meters, MinigameStatus.Succeeded, FinishEstimate.None))
                .Should().Be(DefaultLeafHeuristic.Instance.Evaluate(new LeafPosition(meters, MinigameStatus.Succeeded, FinishEstimate.None)));
            heuristic.Evaluate(new LeafPosition(meters, MinigameStatus.Failed, FinishEstimate.None)).Should().Be(0);
        }

        [Test]
        public void ThePositionsOfAnEngineAndOfAPreviewedResultCarryEveryDisplayedMeter()
        {
            var recipe = Recipe(RecipeType.MynockMeatBall);
            var engine = Scenario.CreateEngine(Request(recipe, 20, GearLevel.None(SkillType.Agriculture)));

            var position = LeafPosition.Of(engine);

            position.Meter(KitchenEngine.DistanceMeterId).Should().NotBeNull();
            position.Meter(MinigameMeterIds.Progress).Should().Be(0);
            position.Meter("no-such-meter").Should().BeNull();
            engine.DescribeMeters().Select(view => view.Id).Should().BeSubsetOf(position.MeterValues.Keys);
        }

        [Test]
        public void TheAdaptivePlayerReadsTheKitchensDistanceMeterAtItsLeaves()
        {
            var recipe = Recipe(RecipeType.MynockMeatBall);
            var engine = Scenario.CreateEngine(Request(recipe, 20, GearLevel.None(SkillType.Agriculture)));
            var spy = new SpyHeuristic();
            var policy = new AdaptivePolicy(new AdaptivePolicyOptions { Depth = 1, Heuristic = spy });

            var action = policy.Choose(engine);

            action.Should().NotBeNullOrEmpty();
            spy.Distances.Should().NotBeEmpty().And.OnlyContain(distance => distance != null);
        }

        private sealed class SpyHeuristic : ILeafHeuristic
        {
            public List<int?> Distances { get; } = new();

            public double Evaluate(LeafPosition position)
            {
                lock (Distances)
                    Distances.Add(position.Meter(KitchenEngine.DistanceMeterId));
                return 0.5;
            }
        }
    }
}
