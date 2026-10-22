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

        private static ReactionEngine Inner(ICraftMinigameEngine engine) => ((ReactionPlayerEngine)engine).Inner;

        private static CraftEngineRequest Request(RecipeEntry recipe, int rank, GearLevel gear, int seed = 1) =>
            new(recipe, rank, gear, null, 1, SimMinigameRandom.ForRun(1, SimStreams.Adaptive, (int)recipe.Type, rank, 0, seed));

        [Test]
        public void TheGatesCommandFindsTheReactionAndItsDampenFreeVariantByName()
        {
            var registry = ScenarioRegistry.Discover();

            registry.Find("reaction").Minigame.Should().Be(CraftMinigameType.Reaction);
            registry.Find("reaction").Skill.Should().Be(SkillType.Espionage);
            registry.Find("reaction-nodampen").Title.Should().Contain("without Dampen");
            registry.Find("reaction-minrank").Title.Should().Contain("minimum rank");
            registry.Find("reaction-minrank-nodampen").Title.Should().Contain("without Dampen");
        }

        [Test]
        public void TheMinimumRankVariantsPlayTheSameRecipesThreeRanksBelowTheirLevels()
        {
            ICraftScenario atLevel = new ReactionScenario();

            foreach (ICraftScenario variant in new ICraftScenario[] { new ReactionAtMinimumRankScenario(), new ReactionAtMinimumRankWithoutDampenScenario() })
            {
                var recipes = variant.Cohort.Recipes(SharedData.Recipes);

                recipes.Select(recipe => recipe.Resref).Should().Equal(atLevel.Cohort.Recipes(SharedData.Recipes).Select(recipe => recipe.Resref));
                recipes.Select(variant.Cohort.RankFor).Should().Equal(recipes.Select(recipe => Math.Max(Math.Min(recipe.Level, 50) - 3, 0)), variant.Name);
            }
        }

        [Test]
        public void TheVariantsKeepTheXpMultiplierAndTheEnhancementSupportOfTheReaction()
        {
            ICraftScenario variant = new ReactionAtMinimumRankScenario();

            foreach (var recipe in EspionageRecipes())
                variant.XpMultiplierFor(recipe).Should().Be(ReactionRules.XpMultiplier(recipe.Level), recipe.Resref);

            variant.Enhancements.Should().Be(EnhancementSupport.NoSlots);
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

            var bare = Inner(scenario.CreateEngine(Request(recipe, 45, GearLevel.None(SkillType.Espionage))));
            var geared = Inner(scenario.CreateEngine(Request(recipe, 45, gear)));

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

            var first = Inner(scenario.CreateEngine(Request(recipe, 3, GearLevel.None(SkillType.Espionage), seed: 4)));
            var second = Inner(scenario.CreateEngine(Request(recipe, 3, GearLevel.None(SkillType.Espionage), seed: 4)));

            first.CycleIndex.Should().Be(second.CycleIndex);
        }

        [Test]
        public void TheXpMultiplierIsTheOneTheGameApplies()
        {
            ICraftScenario scenario = new ReactionScenario();

            foreach (var recipe in EspionageRecipes())
                scenario.XpMultiplierFor(recipe).Should().Be(ReactionRules.XpMultiplier(recipe.Level), recipe.Resref);

            scenario.XpMultiplierFor(EspionageRecipes().First(recipe => recipe.Level <= 10)).Should().BeGreaterThan(1.0);
            scenario.XpMultiplierFor(EspionageRecipes().First(recipe => recipe.Level > 10)).Should().Be(1.0, "only the lowest band needs a multiplier");
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

        private static ReactionPlayerEngine PlayerEngine(int rank, int level, bool offersDampen = true, bool prudent = true) =>
            new(
                new ReactionEngine(
                    new SeededMinigameRandom(1),
                    new ReactionSettings(new ReactionParameters(ReactionCyclePattern.Brew, 6, 2), rank, level),
                    0),
                offersDampen,
                prudent);

        /// <summary>Plays Measure until the Stability Dampen restores is missing.</summary>
        private static ReactionPlayerEngine Worn(ReactionPlayerEngine engine)
        {
            while (engine.Meters.Integrity.Max - engine.Meters.Integrity.Value < ReactionRules.DampenRestore)
                engine.Perform("measure").Performed.Should().BeTrue();

            engine.Status.Should().Be(MinigameStatus.InProgress);
            return engine;
        }

        [Test]
        public void ThePlayerKeepsTheCpItsNextStabilizeNeedsInsteadOfSpendingItOnDampen()
        {
            // Rank 24 has exactly the 18 CP a level 30 Stabilize costs.
            var engine = Worn(PlayerEngine(rank: 24, level: 30));
            engine.Meters.Cp.Value.Should().Be(ReactionRules.StabilizeCp(30));

            var preview = engine.Preview("dampen");
            var result = engine.Perform("dampen");
            var turn = engine.Turn;

            preview.Enabled.Should().BeFalse();
            preview.DisabledReason.Should().Be("Keeps the CP Stabilize needs.");
            result.Performed.Should().BeFalse();
            engine.Turn.Should().Be(turn);
            engine.Meters.Cp.Value.Should().Be(ReactionRules.StabilizeCp(30));
        }

        [Test]
        public void ThePlayerSpendsOnlyTheCpItHasToSpare()
        {
            var spare = Worn(PlayerEngine(rank: 50, level: 30));
            spare.Preview("dampen").Enabled.Should().BeTrue("a rank 50 crafter has CP beyond one Stabilize");
            spare.Perform("dampen").Performed.Should().BeTrue();
            spare.Meters.Cp.Value.Should().Be(ReactionRules.MaxCp(50) - ReactionRules.DampenCp(30));

            var locked = Worn(PlayerEngine(rank: 9, level: 12));
            locked.Preview("dampen").Enabled.Should().BeTrue("Stabilize is not learned before rank 10, so all the CP is spare");
        }

        [Test]
        public void ThePlayerDoesNotDampenWhileTheStabilityItRestoresIsNotMissing()
        {
            var engine = PlayerEngine(rank: 50, level: 30);

            engine.Preview("dampen").Enabled.Should().BeFalse("Stability is full");
            engine.Preview("dampen").DisabledReason.Should().Be("Would waste the Stability it restores.");
            engine.Perform("dampen").Performed.Should().BeFalse();
            engine.Turn.Should().Be(0);

            Worn(engine).Preview("dampen").Enabled.Should().BeTrue();
        }

        [Test]
        public void ThePlayerWithoutTheRulesDampensAnyTimeItCanPay()
        {
            var engine = PlayerEngine(rank: 24, level: 30, prudent: false);

            engine.Preview("dampen").Enabled.Should().BeTrue("Stability is full and the CP is Stabilize's");
            engine.Perform("dampen").Performed.Should().BeTrue();
            engine.Meters.Cp.Value.Should().BeLessThan(ReactionRules.StabilizeCp(30));
        }

        [Test]
        public void TheRulesSurviveAForkAndLeaveTheOriginalUntouched()
        {
            var engine = Worn(PlayerEngine(rank: 24, level: 30));
            var turn = engine.Turn;
            var copy = engine.Fork(new SeededMinigameRandom(2));

            copy.Perform("measure").Performed.Should().BeTrue();

            engine.Turn.Should().Be(turn);
            copy.Turn.Should().Be(turn + 1);
            copy.Preview("dampen").Enabled.Should().BeFalse();
            copy.Preview("dampen").DisabledReason.Should().Be("Keeps the CP Stabilize needs.");
            copy.Actions.Select(action => action.Id).Should().Contain("dampen");
        }
    }
}
