namespace SWLOR.Game.Server.Data.Entities
{
    public partial class ConstructionSiteComponent
    {
        public int ConstructionSiteComponentID { get; set; }

        public int ConstructionSiteID { get; set; }

        public int Quantity { get; set; }

        public int StructureComponentID { get; set; }

        public virtual ConstructionSite ConstructionSite { get; set; }

        public virtual StructureComponent StructureComponent { get; set; }
    }
}
