using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StationService;

namespace SWLOR.CraftSim.Scenario.Circuit
{
    /// <summary>
    /// The space revamp's Capital hull deeds, built at a station Shipyard whose droid speeds up every Assembly gain
    /// (<see cref="StationDroidRules.ShipyardBuildSpeedPercent"/>, played at its ceiling: a droid of Industry
    /// <see cref="MaximumIndustry"/>). Only the Shipyard recipes are in the cohort (all in the top band), so the gates check that the
    /// fastest Shipyard build still meets the Circuit's targets. The crafting revamp's own <c>circuit</c> scenario is unchanged.
    /// </summary>
    public sealed class CircuitShipyardScenario : CircuitScenario, ICraftScenario
    {
        /// <summary>The highest droid Industry the Shipyard droid percent is played at.</summary>
        public const int MaximumIndustry = 100;

        public CircuitShipyardScenario()
            : base("circuit-shipyard", "Circuit (Capital hulls at a Shipyard, best droid)", null)
        {
        }

        public static int DroidPercent => StationDroidRules.ShipyardBuildSpeedPercent(MaximumIndustry);

        public ScenarioCohort Cohort => new(
            recipe => recipe.Skill == SkillType.Engineering && recipe.Detail != null && Craft.IsShipyardRecipe(recipe.Detail),
            bands: new[] { new RecipeBand("41-53", 41, int.MaxValue) });

        protected override int StationAssemblyPercent(RecipeEntry recipe) =>
            recipe.Detail != null && Craft.IsShipyardRecipe(recipe.Detail) ? DroidPercent : 0;
    }
}
