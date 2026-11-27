namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A manufacturer component (plan section 14, Appendix D): the tier 5 component of a family carrying its maker's
    /// trait. Rows come from the generated RevampComponentCatalog.
    /// </summary>
    public class ShipManufacturerComponent
    {
        public ShipManufacturer Manufacturer { get; set; }

        /// <summary>The component's name, which is also its trait's name, for example "Republic Shield Matrix".</summary>
        public string Name { get; set; }

        public ShipComponentFamily Family { get; set; }

        /// <summary>The family's tier 5 component item, which carries the trait.</summary>
        public string Resref { get; set; }

        public ShipComponentTraitType Trait { get; set; }
    }
}
