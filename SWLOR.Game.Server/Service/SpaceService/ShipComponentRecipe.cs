using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// One component recipe. A crafted variant makes the same item as the plain recipe and stamps its trait on it.
    /// Rows come from RevampComponentCatalog.
    /// </summary>
    public class ShipComponentRecipe
    {
        public RecipeType RecipeType { get; set; }
        public ShipComponentFamily Family { get; set; }
        public int Tier { get; set; }
        public string Resref { get; set; }
        public SkillType Skill { get; set; }
        public int RecipeLevel { get; set; }

        /// <summary>The crafted variant's trait, or None for the plain component.</summary>
        public ShipComponentTraitType Trait { get; set; }

        public string Name { get; set; }
        public (string Resref, int Quantity)[] Components { get; set; } = new (string Resref, int Quantity)[0];
    }
}
