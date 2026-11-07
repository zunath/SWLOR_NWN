using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>One tier's recipe for a crafted ammo variant. Rows come from the generated RevampRewardCatalog.</summary>
    public class ShipAmmoRecipe
    {
        public RecipeType RecipeType { get; set; }
        public string Resref { get; set; }
        public string Name { get; set; }
        public int Tier { get; set; }
        public int RecipeLevel { get; set; }
        public int Quantity { get; set; }
        public (string Resref, int Quantity)[] Components { get; set; } = new (string Resref, int Quantity)[0];
    }
}
