using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Scenario.Reaction;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Scenario
{
    [TestFixture]
    public class ReactionScenarioTests
    {
        private static IReadOnlyList<RecipeEntry> EspionageRecipes() =>
            SharedData.Recipes.ForSkill(SkillType.Espionage).ToList();

        private static CraftEngineRequest Request(RecipeEntry recipe, int rank, GearLevel gear, int seed = 1) =>
            new(recipe, rank, gear, null, 1, SimMinigameRandom.ForRun(1, SimStreams.Adaptive, (int)recipe.Type, rank, 0, seed));

        [Test]
        public void TheGatesCommandFindsTheReactionAndItsDampenFreeVariantByName()
        {
            var registry = ScenarioRegistry.Discover();

            registry.Find("reaction").Minigame.Should().Be(CraftMinigameType.Reaction);
            registry.Find("reaction").Skill.Should().Be(SkillType.Espionage);
            registry.Find("reaction-nodampen").Title.Should().Contain("without Dampen");
        }

        [Test]
        public void ItPlaysTheFifteenEspionageRecipesAtTheirOwnLevels()
        {
            ICraftScenario scenario = new ReactionScenario();
            var recipes = scenario.Cohort.Recipes(SharedData.Recipes);

            recipes.Should().HaveCount(15);
            recipes.Should().OnlyContain(recipe => recipe.Skill == SkillType.Espionage && recipe.Detail.MinigameParameters is ReactionParameters);
            recipes.Select(scenario.Cohort.RankFor).Should().Equal(recipes.Select(recipe => Math.Min(recipe.Level, 50)));
            scenario.LengthTarget.ToString().Should().Contain("10-20");
        }

        [Test]
        public void TheEngineIsBuiltFromTheGamesOwnSettingsAndIgnoresGear()
        {
            var scenario = new ReactionScenario();
            var recipe = EspionageRecipes().First(entry => entry.Level >= 40);
            var gear = SharedData.Gear.Levels(SkillType.Smithery).Last();

            var bare = (ReactionEngine)scenario.CreateEngine(Request(recipe, 45, GearLevel.None(SkillType.Espionage)));
            var geared = (ReactionEngine)scenario.CreateEngine(Request(recipe, 45, gear));

            gear.Craftsmanship.Should().BeGreaterThan(0);
            geared.Settings.ChargePotency.Should().Be(bare.Settings.ChargePotency, "Espionage has no crafting gear");
            geared.Settings.MaxCp.Should().Be(bare.Settings.MaxCp);
            bare.Settings.Rank.Should().Be(45);
            bare.Settings.RecipeLevel.Should().Be(recipe.Level);
            bare.Settings.ChargePotency.Should().Be(ReactionSettings.ForRecipe(recipe.Detail, 45).ChargePotency);
        }

        [Test]
        public void TheSameSeedStartsTheSameAttempt()
        {
            var scenario = new ReactionScenario();
            var recipe = EspionageRecipes().First();

            var first = (ReactionEngine)scenario.CreateEngine(Request(recipe, 3, GearLevel.None(SkillType.Espionage), seed: 4));
            var second = (ReactionEngine)scenario.CreateEngine(Request(recipe, 3, GearLevel.None(SkillType.Espionage), seed: 4));

            first.CycleIndex.Should().Be(second.CycleIndex);
        }

        [Test]
        public void EspionageRecipesHaveNoEnhancementSlotsSoGateSevenIsNotApplicable()
        {
            new ReactionScenario().Enhancements.Should().Be(EnhancementSupport.NoSlots);
            new ReactionWithoutDampenScenario().Enhancements.Should().Be(EnhancementSupport.NoSlots);
        }

        [Test]
        public void TheXpMultiplierIsTheOneTheGameApplies()
        {
            ICraftScenario scenario = new ReactionScenario();

            scenario.XpMultiplier.Should().Be(ReactionRules.XpMultiplier);
            scenario.SecondsPerAction.Should().Be(8.0);
        }

        [Test]
        public void TheDampenFreeVariantHidesDampenEverywhereAndForksIntoAnotherDampenFreeEngine()
        {
            var recipe = EspionageRecipes().First(entry => entry.Level >= 40);
            var engine = new ReactionWithoutDampenScenario().CreateEngine(Request(recipe, 50, GearLevel.None(SkillType.Espionage)));

            engine.Actions.Select(action => action.Id).Should().NotContain("dampen");
            engine.PreviewAll().Select(preview => preview.Action.Id).Should().NotContain("dampen");
            engine.Preview("charge").Enabled.Should().BeTrue();
            ((Action)(() => engine.Preview("dampen"))).Should().Throw<ArgumentException>();
            engine.Perform("dampen").Performed.Should().BeFalse();

            var copy = engine.Fork(SimMinigameRandom.ForRun(1, SimStreams.Adaptive, 1, 1, 0, 1));
            copy.Perform("refine").Performed.Should().BeTrue();
            copy.Actions.Select(action => action.Id).Should().NotContain("dampen");
            engine.Turn.Should().Be(0);
        }
    }
}
