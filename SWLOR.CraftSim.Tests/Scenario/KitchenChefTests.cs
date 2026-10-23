using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Scenario.Kitchen;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Scenario
{
    /// <summary>Covers the Chef builds the Kitchen scenarios play: their numbers come from the game's perk definitions at the rank they are played at.</summary>
    [TestFixture]
    public class KitchenChefTests
    {
        private static RecipeEntry Recipe(int level) =>
            SharedData.Recipes.ForSkill(SkillType.Agriculture).First(recipe => recipe.Level >= level);

        private static CraftEngineRequest Request(RecipeEntry recipe, int rank, CraftStyle style = null, int seed = 7) =>
            new(recipe, rank, SharedData.Gear.Levels(SkillType.Agriculture)[1], style, seed, SimMinigameRandom.ForRun(seed, 1, (int)recipe.Type));

        [Test]
        public void ABuildTakesItsNumbersFromThePerkDefinitionsAtTheRankItIsPlayedAt()
        {
            KitchenBuild.SlowCookingAlone.StatsAt(19).Should().Be(KitchenPerkStats.None, "the style opens at Agriculture 20");
            KitchenBuild.SlowCookingAlone.StatsAt(20).Should().Be(new KitchenPerkStats(LowHeatDonenessPercent: 80, MediumDriftReductionPercent: 25));
            KitchenBuild.FlashCookingAlone.StatsAt(50).Should().Be(new KitchenPerkStats(HighDriftReductionPercent: 15));

            KitchenBuild.SlowCookingLine.StatsAt(24).Should().Be(new KitchenPerkStats(
                PantryPushBonus: 1, PushSpreadReduction: 5, SeasonedPalateOwned: true, LowHeatDonenessPercent: 80, MediumDriftReductionPercent: 25));
            KitchenBuild.FlashCookingLine.StatsAt(50).Should().Be(new KitchenPerkStats(
                PantryPushBonus: 1, PushSpreadReduction: 5, SeasonedPalateOwned: true, SeasonedPalateSpreadPercent: 0, HighDriftReductionPercent: 15,
                ReducePercent: 35, ReduceCpReduction: 3, PeakAromaChancePoints: 10, StirCpReduction: 4,
                StirDonenessPercent: 50, SignatureDish: true));
        }

        [Test]
        public void SeasonedPalateCountsOnlyInTheBuildsThatSwitchItOn()
        {
            var off = KitchenBuild.SlowCookingLine.StatsAt(50);
            var on = KitchenBuild.SlowCookingLineSeasoned.StatsAt(50);

            off.SeasonedPalateOwned.Should().BeTrue();
            off.SeasonedPalateOn.Should().BeFalse();
            off.SeasonedPalateSpreadPercent.Should().Be(0);
            on.SeasonedPalateOn.Should().BeTrue();
            on.SeasonedPalateSpreadPercent.Should().Be(25);
            KitchenBuild.SlowCookingLineSeasoned.StatsAt(14).SeasonedPalateOn.Should().BeFalse("it opens at Agriculture 15");
        }

        [Test]
        public void AFullLineOwnsOneStyleOnly_AndAnAloneBuildOwnsOnePerk()
        {
            KitchenBuild.SlowCookingLine.Perks.Should().Contain(PerkType.SlowCooking).And.NotContain(PerkType.FlashCooking);
            KitchenBuild.FlashCookingLine.Perks.Should().Contain(PerkType.FlashCooking).And.NotContain(PerkType.SlowCooking);
            KitchenBuild.SlowCookingLine.Perks.Should().HaveCount(KitchenBuild.AllPerks.Count - 1);
            KitchenBuild.Alone(PerkType.KitchenReduce).Perks.Should().Equal(PerkType.KitchenReduce);
            KitchenBuild.Alone(PerkType.KitchenReduce).StatsAt(24).Should().Be(KitchenPerkStats.None);
            KitchenBuild.Alone(PerkType.KitchenReduce).StatsAt(25).HasReduce.Should().BeTrue();
            KitchenBuild.AllPerks.Should().HaveCount(9);
        }

        [Test]
        public void ARankedPerkGivesTheHighestRankTheCooksRankAllows()
        {
            var reduce = KitchenBuild.Alone(PerkType.KitchenReduce);

            reduce.StatsAt(39).ReducePercent.Should().Be(20);
            reduce.StatsAt(40).ReducePercent.Should().Be(35);
            reduce.StatsAt(40).ReduceCpReduction.Should().Be(3);
        }

        [Test]
        public void ABuildInTheRequestReachesTheEngine_ThroughTheStatsTheWindowAlsoUses()
        {
            var scenario = new KitchenScenario();
            var recipe = Recipe(30);

            var engine = (KitchenEngine)scenario.CreateEngine(Request(recipe, 50, KitchenBuild.FlashCookingLine.AsStyle()));

            engine.Stats.Perks.Should().Be(KitchenBuild.FlashCookingLine.StatsAt(50));
            engine.Actions.Select(action => action.Id).Should().Contain(KitchenActionIds.Reduce).And.Contain(KitchenActionIds.SignatureSavory);
            ((KitchenEngine)scenario.CreateEngine(Request(recipe, 50))).Stats.Perks.Should().Be(KitchenPerkStats.None);
        }

        [Test]
        public void APerkStatsRecordOrAFunctionOfTheRankCanBeThePlayersStyleToo_ForTryingOtherMagnitudes()
        {
            var scenario = new KitchenScenario();
            var recipe = Recipe(30);

            var fixedStats = (KitchenEngine)scenario.CreateEngine(Request(
                recipe, 30, new CraftStyle("Try", new KitchenPerkStats(LowHeatDonenessPercent: 100))));
            var atRank = (KitchenEngine)scenario.CreateEngine(Request(
                recipe, 30, new CraftStyle("Rank", (Func<int, KitchenPerkStats>)(rank => new KitchenPerkStats(ReducePercent: rank)))));

            fixedStats.Stats.Perks.LowHeatDonenessPercent.Should().Be(80);
            atRank.Stats.Perks.ReducePercent.Should().Be(30);
        }

        [Test]
        public void TheUnperkedScenarioComparesTheStylesAloneWithTheLineAndWithSeasonedPalateOn()
        {
            var pairs = new KitchenScenario().StylePairs;

            pairs.Select(pair => pair.Name).Should().Equal(
                "Slow Cooking vs Flash Cooking",
                "Slow Cooking (full line) vs Flash Cooking (full line)",
                "Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on)");
            pairs.Should().OnlyContain(pair => pair.MinimumRank == 20);
        }

        [Test]
        public void TheChefScenariosAreFoundByName_AndPlayTheLineAtEveryRank()
        {
            var registry = ScenarioRegistry.Discover();
            var slow = registry.Find("kitchen-slow-cooking");
            var flash = registry.Find("kitchen-flash-cooking");
            var slowSeasoned = registry.Find("kitchen-slow-cooking-seasoned");
            var flashSeasoned = registry.Find("kitchen-flash-cooking-seasoned");

            slow.Should().BeOfType<KitchenSlowCookingScenario>();
            flash.Should().BeOfType<KitchenFlashCookingScenario>();
            slow.Skill.Should().Be(SkillType.Agriculture);
            new[] { slow, flash, slowSeasoned, flashSeasoned }.Should().OnlyContain(scenario => scenario.StylePairs.Count == 0, "the styles are compared in the unperked scenario");
            slow.Limitations.Should().Contain(text => text.Contains("Slow Cooking"));

            var recipe = Recipe(25);
            ((KitchenEngine)slow.CreateEngine(Request(recipe, 50))).Stats.Perks.LowHeatDonenessPercent.Should().Be(100);
            ((KitchenEngine)flash.CreateEngine(Request(recipe, 50))).Stats.Perks.HighDriftReductionPercent.Should().Be(15);
            ((KitchenEngine)slow.CreateEngine(Request(recipe, 10))).Stats.Perks.LowHeatDonenessPercent.Should().Be(0, "no style below rank 20");
            ((KitchenEngine)slow.CreateEngine(Request(recipe, 25))).Stats.Perks.HasReduce.Should().BeTrue();
            ((KitchenEngine)slow.CreateEngine(Request(recipe, 24))).Stats.Perks.HasReduce.Should().BeFalse();
            ((KitchenEngine)slow.CreateEngine(Request(recipe, 50))).Stats.Perks.SeasonedPalateOn.Should().BeFalse();
            ((KitchenEngine)slowSeasoned.CreateEngine(Request(recipe, 50))).Stats.Perks.SeasonedPalateOn.Should().BeTrue();
        }

        [Test]
        public void ARequestsOwnStyleOverridesTheChefScenariosBuild()
        {
            var slow = new KitchenSlowCookingScenario();

            var engine = (KitchenEngine)slow.CreateEngine(Request(Recipe(25), 50, KitchenBuild.FlashCookingAlone.AsStyle()));

            engine.Stats.Perks.Should().Be(KitchenBuild.FlashCookingAlone.StatsAt(50));
        }

        [Test]
        public void TheAdaptivePlayerMakesADishWithEveryChefBuild()
        {
            var scenario = new KitchenScenario();
            var recipe = Recipe(40);
            foreach (var build in new[]
                     {
                         KitchenBuild.SlowCookingLine, KitchenBuild.FlashCookingLine, KitchenBuild.SlowCookingLineSeasoned,
                         KitchenBuild.Alone(PerkType.SignatureDish), KitchenBuild.Alone(PerkType.KitchenReduce)
                     })
            {
                var engine = scenario.CreateEngine(Request(recipe, 50, build.AsStyle()));

                var result = CraftPlayer.Play(engine, new AdaptivePolicy(scenario.AdaptiveOptions, 5));

                result.Succeeded.Should().BeTrue(build.Name);
                result.QualityFraction.Should().BeGreaterThan(0.3, build.Name);
            }
        }
    }
}
