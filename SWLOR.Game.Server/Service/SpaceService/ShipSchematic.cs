using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A Ship Schematic: the recipe unlock for an Advanced ship's deed (plan section 7). Rows come from the generated
    /// RevampRewardCatalog.
    /// </summary>
    public class ShipSchematic
    {
        public string ShipTag { get; set; }
        public string ShipName { get; set; }
        public RecipeType RecipeType { get; set; }

        /// <summary>The schematic item, a recipe book that teaches <see cref="RecipeType"/>.</summary>
        public string Resref { get; set; }

        /// <summary>The ship's tier, from its Piloting gate (1-5).</summary>
        public int Tier { get; set; }

        /// <summary>The schematic only drops in Wild Space (Rift Raider ships).</summary>
        public bool WildSpaceOnly { get; set; }
    }
}
