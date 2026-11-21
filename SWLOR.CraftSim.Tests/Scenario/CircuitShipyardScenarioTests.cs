using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Scenario.Circuit;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StationService;

namespace SWLOR.CraftSim.Tests.Scenario
{
    /// <summary>The space revamp's Shipyard build speed in the Circuit gates: Capital hulls only, at the best droid.</summary>
    [TestFixture]
    public class CircuitShipyardScenarioTests
    {
        [Test]
        public void ItsCohortIsTheShipyardRecipes_AllInTheTopBand()
        {
            var cohort = ((ICraftScenario)ScenarioRegistry.Discover().Find("circuit-shipyard")).Cohort;

            var recipes = cohort.Recipes(SharedData.Recipes);

            recipes.Should().NotBeEmpty().And.OnlyContain(recipe => recipe.Skill == SkillType.Engineering && Craft.IsShipyardRecipe(recipe.Detail));
            cohort.Bands.Should().ContainSingle().Which.Contains(45).Should().BeTrue();
        }

        [Test]
        public void ItPlaysTheBestShipyardDroid_OnAssemblyOnly()
        {
            var recipe = ((ICraftScenario)new CircuitShipyardScenario()).Cohort.Recipes(SharedData.Recipes).First();
            var gear = SharedData.Gear.Levels(SkillType.Engineering).Last();
            CraftEngineRequest Request() => new(recipe, recipe.Level, gear, null, 1, new SeededMinigameRandom(1));

            var plain = ((CircuitEngine)new CircuitScenario().CreateEngine(Request())).Setup;
            var droid = ((CircuitEngine)new CircuitShipyardScenario().CreateEngine(Request())).Setup;

            CircuitShipyardScenario.DroidPercent.Should().Be(StationDroidRules.ShipyardBuildSpeedPercent(CircuitShipyardScenario.MaximumIndustry)).And.BePositive();
            plain.StationAssemblyPercent.Should().Be(0, "the crafting revamp's own scenario crafts at an ordinary station");
            droid.StationAssemblyPercent.Should().Be(CircuitShipyardScenario.DroidPercent);
            droid.WireGain(0).Should().BeGreaterThan(plain.WireGain(0));
            droid.TuneGain(0).Should().Be(plain.TuneGain(0));
        }
    }
}
