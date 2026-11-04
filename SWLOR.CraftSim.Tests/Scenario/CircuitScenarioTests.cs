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
            scenario.SecondsPerAction.Should().Be(CraftMinigameXp.ActionSeconds, "the gates model the pace the server holds a craft's XP to");
            ((ICraftScenario)scenario).XpMultiplierFor(5).Should().Be(CircuitRules.XpMultiplierFor(5), "the scenario plays the multiplier the game pays");
            scenario.LengthTarget.Should().Be(LengthTarget.ItemCraft);
            scenario.StylePairs.Should().HaveCount(2).And.OnlyContain(pair => pair.MinimumRank == CircuitRules.StyleRank);
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

        [Test]
        public void TheStylePairsComparePlainStylesAndFullLines_AndEachStyleCarriesItsLoadout()
        {
            var pairs = new CircuitScenario().StylePairs;

            pairs.Select(pair => pair.Name).Should().Equal(
                "Overclock (no lines) vs Safe Mode (no lines)", "Overclock (both lines) vs Safe Mode (both lines)");
            ((CircuitLoadout)pairs[0].First.Configuration).Should().Be(new CircuitLoadout(CircuitStyle.Overclock, false, false));
            ((CircuitLoadout)pairs[1].Second.Configuration).Should().Be(new CircuitLoadout(CircuitStyle.SafeMode, true, true));
        }

        [Test]
        public void AStyleBuildsTheEngineWithThePerksItsRankAllows()
        {
            var recipe = Recipe(30);
            var gear = GearLevel.None(SkillType.Engineering);
            var scenario = new CircuitScenario();
            var overclock = new CircuitLoadout(CircuitStyle.Overclock, true, true).ToStyle("Overclock (both lines)");

            var plain = (CircuitEngine)scenario.CreateEngine(new CraftEngineRequest(recipe, 30, gear, null, 1, new SeededMinigameRandom(1)));
            var early = (CircuitEngine)scenario.CreateEngine(new CraftEngineRequest(recipe, 4, gear, overclock, 1, new SeededMinigameRandom(1)));
            var perked = (CircuitEngine)scenario.CreateEngine(new CraftEngineRequest(recipe, 30, gear, overclock, 1, new SeededMinigameRandom(1)));
            var full = (CircuitEngine)scenario.CreateEngine(new CraftEngineRequest(Recipe(50), 50, gear, overclock, 1, new SeededMinigameRandom(1)));

            plain.Setup.Perks.Should().Be(CircuitPerkStats.None, "no style means no perks");
            early.Setup.Perks.Should().Be(CircuitPerkStats.None, "no purchase opens before Engineering 5");
            perked.Setup.Perks.ExtraLoadPerBuildAction.Should().Be(1, "Overclock is open from rank 20");
            perked.Setup.Perks.CascadeDischarge.Should().BeTrue("Cascade Discharge opens at 25");
            perked.Setup.Perks.Surge.Should().BeFalse("Surge is the rank 50 capstone");
            full.Setup.Perks.Surge.Should().BeTrue();
            full.Setup.Perks.Failsafe.Should().BeTrue();
            perked.Actions.Should().Contain(action => action.Id == CircuitActionIds.DischargeBoth);
            plain.Actions.Should().NotContain(action => action.Id == CircuitActionIds.DischargeBoth);
        }

        [TestCase("circuit-overclocker", CircuitStyle.Overclock, true, false)]
        [TestCase("circuit-technician", CircuitStyle.SafeMode, false, true)]
        [TestCase("circuit-both", CircuitStyle.Overclock, true, true)]
        public void ThePerkedScenariosPlayTheirLoadoutWithoutAStyleAndOfferNoStylePairs(string name, CircuitStyle style, bool overclocker, bool technician)
        {
            var scenario = ScenarioRegistry.Discover().Find(name);
            var recipe = Recipe(50);
            var engine = (CircuitEngine)scenario.CreateEngine(
                new CraftEngineRequest(recipe, 50, GearLevel.None(SkillType.Engineering), null, 1, new SeededMinigameRandom(1)));

            scenario.StylePairs.Should().BeEmpty();
            scenario.Skill.Should().Be(SkillType.Engineering);
            engine.Setup.Perks.Should().Be(new CircuitLoadout(style, overclocker, technician).StatsAt(50));
            engine.Setup.Perks.Surge.Should().Be(overclocker);
            engine.Setup.Perks.Failsafe.Should().Be(technician);
        }

        [Test]
        public void ThePerkedScenariosSayWhichLoadoutTheyPlay_AndThePlainOneDoesNot()
        {
            var both = ScenarioRegistry.Discover().Find("circuit-both");
            var technician = ScenarioRegistry.Discover().Find("circuit-technician");
            var plain = ScenarioRegistry.Discover().Find("circuit");

            both.Limitations.Should().Contain(
                "Every craft in this scenario is played by a crafter who bought both perk lines in full (every purchase their Engineering rank allows) and plays Overclock, so no gate here is an unperked result.");
            technician.Limitations.Should().Contain(line => line.Contains("the Technician line in full") && line.Contains("plays Safe Mode"));
            plain.Limitations.Should().NotContain(line => line.Contains("Every craft in this scenario"));
        }
    }
}
