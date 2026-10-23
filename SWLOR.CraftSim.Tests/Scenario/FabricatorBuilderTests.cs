using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Scenario.Fabricator;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Scenario
{
    [TestFixture]
    public class FabricatorBuilderTests
    {
        private static RecipeEntry Recipe(int level) =>
            SharedData.Recipes.ForSkill(SkillType.Fabrication).First(recipe => recipe.Level >= level);

        private static CraftEngineRequest Request(RecipeEntry recipe, int rank, CraftStyle style = null, int seed = 7) =>
            new(recipe, rank, SharedData.Gear.Levels(SkillType.Fabrication)[1], style, seed, SimMinigameRandom.ForRun(seed, 1, (int)recipe.Type));

        [Test]
        public void ABuildTakesItsNumbersFromThePerkDefinitionsAtTheRankItIsPlayedAt()
        {
            FabricatorBuild.ProductionRunAlone.StatsAt(19).Should().Be(FabricatorPerkStats.None, "the style opens at Fabrication 20");
            FabricatorBuild.ProductionRunAlone.StatsAt(20).Should().Be(new FabricatorPerkStats(RunPowerPercent: 25, PrecisionPowerPercent: -25));
            FabricatorBuild.FineToleranceAlone.StatsAt(50).Should().Be(new FabricatorPerkStats(RunPowerPercent: -25, PrecisionPowerPercent: 25));

            FabricatorBuild.ProductionRunLine.StatsAt(24).Should().Be(new FabricatorPerkStats(
                RunPowerPercent: 25, PrecisionPowerPercent: -25, WearReduction: 1, RetoolCpReductionPercent: 25,
                ServiceRestoreBonus: 10, ServiceCpReduction: 2));
            FabricatorBuild.ProductionRunLine.StatsAt(50).Should().Be(new FabricatorPerkStats(
                RunPowerPercent: 25, PrecisionPowerPercent: -25, WearReduction: 1, RetoolCpReductionPercent: 50,
                ServiceRestoreBonus: 20, ServiceCpReduction: 4, DualFeedUnlocked: true, PrimeWindowBonusActions: 1,
                PrimeBonusPercentBonus: 25, AssembleCapBonusPoints: 15));
            FabricatorBuild.FineToleranceLine.StatsAt(50).RunPowerPercent.Should().Be(-25);
        }

        [Test]
        public void AFullLineOwnsOneStyleOnly_AndAnAloneBuildOwnsOnePerk()
        {
            FabricatorBuild.ProductionRunLine.Perks.Should().Contain(PerkType.ProductionRun).And.NotContain(PerkType.FineTolerance);
            FabricatorBuild.FineToleranceLine.Perks.Should().Contain(PerkType.FineTolerance).And.NotContain(PerkType.ProductionRun);
            FabricatorBuild.ProductionRunLine.Perks.Should().HaveCount(FabricatorBuild.AllPerks.Count - 1);
            FabricatorBuild.Alone(PerkType.DualFeed).Perks.Should().Equal(PerkType.DualFeed);
            FabricatorBuild.Alone(PerkType.DualFeed).StatsAt(24).Should().Be(FabricatorPerkStats.None);
            FabricatorBuild.Alone(PerkType.DualFeed).StatsAt(25).DualFeedUnlocked.Should().BeTrue();
            FabricatorBuild.AllPerks.Should().HaveCount(9);
        }

        [Test]
        public void ARankedPerkGivesTheHighestRankTheCraftersRankAllows()
        {
            var quickChange = FabricatorBuild.Alone(PerkType.QuickChange);

            quickChange.StatsAt(4).RetoolCpReductionPercent.Should().Be(0);
            quickChange.StatsAt(5).RetoolCpReductionPercent.Should().Be(25);
            quickChange.StatsAt(34).RetoolCpReductionPercent.Should().Be(25);
            quickChange.StatsAt(35).RetoolCpReductionPercent.Should().Be(50);
        }

        [Test]
        public void ABuildInTheRequestReachesTheEngine_ThroughTheSetupTheWindowAlsoUses()
        {
            var scenario = new FabricatorScenario();
            var recipe = Recipe(30);
            var request = Request(recipe, 50, FabricatorBuild.ProductionRunLine.AsStyle());

            var engine = (FabricatorEngine)scenario.CreateEngine(request);

            engine.Setup.PerkStats.Should().Be(FabricatorBuild.ProductionRunLine.StatsAt(50));
            engine.Actions.Select(action => action.Id).Should().Contain(FabricatorActionIds.DualFeedFirst);
            ((FabricatorEngine)scenario.CreateEngine(Request(recipe, 50))).Setup.PerkStats.Should().Be(FabricatorPerkStats.None);
        }

        [Test]
        public void APerkStatsRecordCanBeThePlayersStyleToo_ForTryingOtherMagnitudes()
        {
            var scenario = new FabricatorScenario();
            var style = new CraftStyle("Try", new FabricatorPerkStats(RunPowerPercent: 60, PrecisionPowerPercent: -10));

            var engine = (FabricatorEngine)scenario.CreateEngine(Request(Recipe(30), 30, style));

            engine.Setup.PerkStats.RunPowerPercent.Should().Be(60);
            engine.Setup.PerkStats.PrecisionPowerPercent.Should().Be(-10);
        }

        [Test]
        public void TheBuilderScenariosAreFoundByName_AndPlayTheLineAtEveryRank()
        {
            var registry = ScenarioRegistry.Discover();
            var production = registry.Find("fabricator-production-run");
            var fine = registry.Find("fabricator-fine-tolerance");

            production.Should().BeOfType<FabricatorProductionRunScenario>();
            fine.Should().BeOfType<FabricatorFineToleranceScenario>();
            production.Skill.Should().Be(SkillType.Fabrication);
            production.StylePairs.Should().BeEmpty("the styles are compared in the unperked scenario");
            production.Limitations.Should().Contain(text => text.Contains("Production Run"));

            var recipe = Recipe(25);
            ((FabricatorEngine)production.CreateEngine(Request(recipe, 50))).Setup.PerkStats.RunPowerPercent.Should().Be(25);
            ((FabricatorEngine)fine.CreateEngine(Request(recipe, 50))).Setup.PerkStats.RunPowerPercent.Should().Be(-25);
            ((FabricatorEngine)production.CreateEngine(Request(recipe, 10))).Setup.PerkStats.RunPowerPercent.Should().Be(0, "no style below rank 20");
            ((FabricatorEngine)production.CreateEngine(Request(recipe, 25))).Setup.PerkStats.DualFeedUnlocked.Should().BeTrue();
            ((FabricatorEngine)production.CreateEngine(Request(recipe, 24))).Setup.PerkStats.DualFeedUnlocked.Should().BeFalse();
        }

        [Test]
        public void ARequestsOwnStyleOverridesTheBuilderScenariosBuild()
        {
            var production = new FabricatorProductionRunScenario();

            var engine = (FabricatorEngine)production.CreateEngine(Request(Recipe(25), 50, FabricatorBuild.FineToleranceAlone.AsStyle()));

            engine.Setup.PerkStats.Should().Be(FabricatorBuild.FineToleranceAlone.StatsAt(50));
        }

        [Test]
        public void TheAdaptivePlayerMakesAnItemWithEveryBuilderBuild()
        {
            var scenario = new FabricatorScenario();
            var recipe = Recipe(40);
            foreach (var build in new[] { FabricatorBuild.ProductionRunLine, FabricatorBuild.FineToleranceLine, FabricatorBuild.Alone(PerkType.DualFeed) })
            {
                var engine = scenario.CreateEngine(Request(recipe, 50, build.AsStyle()));

                var result = CraftPlayer.Play(engine, new AdaptivePolicy(scenario.AdaptiveOptions, 5));

                result.Succeeded.Should().BeTrue(build.Name);
                result.QualityFraction.Should().BeGreaterThan(0.5, build.Name);
            }
        }

        [Test]
        public void ThePerkLinesHelperSelectsTheHighestRankByTheSkillRequirement()
        {
            var perks = PerkLines.Build(new SWLOR.Game.Server.Feature.PerkDefinition.FabricationBuilderPerkDefinition());

            PerkLines.OwnedLevel(perks[PerkType.FieldService], SkillType.Fabrication, 14).Should().BeNull();
            PerkLines.OwnedLevel(perks[PerkType.FieldService], SkillType.Fabrication, 15).Price.Should().Be(2);
            PerkLines.OwnedLevel(perks[PerkType.FieldService], SkillType.Fabrication, 40).Price.Should().Be(3);
            PerkLines.TotalsAt(perks, SkillType.Fabrication, 0).Should().BeEmpty();
        }
    }
}
