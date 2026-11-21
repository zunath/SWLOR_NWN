using SWLOR.Game.Server.Service.PlanetIdentityService;

namespace SWLOR.Game.Server.Service.CraftService
{
    /// <summary>
    /// The recipe is crafted only at a crafting station inside a city on one planet, or on a player station (Player City Revamp, "Planet
    /// identity"): the planets' signature dishes and drinks and their decor.
    /// </summary>
    public class RecipeIdentityRequirement : IRecipeRequirement
    {
        public IdentityPlace Place { get; }

        public RecipeIdentityRequirement(IdentityPlace place)
        {
            Place = place;
        }

        public string CheckRequirements(uint player) =>
            PlanetIdentityRules.DescribeRefusal(Place, PlanetIdentity.GetCraftingPlace(player));

        public string RequirementText => PlanetIdentityRules.DescribeRequirement(Place);
    }
}
