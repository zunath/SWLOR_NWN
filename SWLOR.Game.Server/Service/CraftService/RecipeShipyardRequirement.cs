namespace SWLOR.Game.Server.Service.CraftService
{
    /// <summary>
    /// The recipe is crafted only at a player station's Shipyard whose access setting admits the crafter (plan section 25):
    /// the Capital hull deeds.
    /// </summary>
    public class RecipeShipyardRequirement : IRecipeRequirement
    {
        public string CheckRequirements(uint player) => StationBuildings.GetShipyardError(player);

        public string RequirementText => "Built at a station Shipyard.";
    }
}
