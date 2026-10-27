namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>One component item: a family at a material tier. Rows come from RevampComponentCatalog.</summary>
    public class ShipComponentItem
    {
        public ShipComponentFamily Family { get; set; }
        public int Tier { get; set; }
        public string Resref { get; set; }
        public string Name { get; set; }
    }
}
