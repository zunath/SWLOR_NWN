using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A Module Schematic: the recipe unlock for a named rank-50 module (plan section 14). Rows come from the generated
    /// RevampRewardCatalog.
    /// </summary>
    public class ShipModuleSchematic
    {
        public ShipModuleFamily Family { get; set; }

        /// <summary>The named module's item tag.</summary>
        public string ModuleTag { get; set; }

        public RecipeType RecipeType { get; set; }

        /// <summary>The schematic item, a recipe book that teaches <see cref="RecipeType"/>.</summary>
        public string Resref { get; set; }
    }
}
