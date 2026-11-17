namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Reads the tint-map material tables and the neck, part-skin, shoulder and robe render tables from the 2DAs when
    /// the module caches its data; the boot handlers are on <see cref="TintMapCatalogService"/>. Static callers reach
    /// the container's instance through <c>GameServices.TintMapCatalog</c>.
    /// </summary>
    public interface ITintMapCatalogService
    {
        /// <summary>Loads the tint-map material table (tintmap and cloaktint) into the tint-map state.</summary>
        void LoadMaterials();

        /// <summary>Loads the neck render table into the appearance render state.</summary>
        void LoadNeckCatalog();

        /// <summary>Loads the part-skin layout and render tables into the appearance render state.</summary>
        void LoadPartSkinCatalog();

        /// <summary>Loads the robe render table and the reserved robe phenotypes into the appearance render state.</summary>
        void LoadRobeCatalog();

        /// <summary>Loads the shoulder render table into the appearance render state.</summary>
        void LoadShoulderCatalog();
    }
}
