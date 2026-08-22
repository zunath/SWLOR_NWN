namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// A section of related itemproperty rows, as an item editor would present them together.
    /// Grouping follows itempropdef.2da's own clusters (Defense/Resistance/enhancement/
    /// droid stats/...) rather than any single family's needs, since <see cref="ItemStatVisibility"/>
    /// is what decides which groups a given family actually shows.
    /// </summary>
    public enum ItemStatGroup
    {
        Defense,
        Resistance,
        Vitals,
        Combat,
        Crafting,
        Bonuses,
        Droid,
        Incubation,
        Npc,
        Utility,

        /// <summary>
        /// The *Enhancement properties (101/102/107/108/109/110/116 - ArmorEnhancement,
        /// WeaponEnhancement, StructureEnhancement, FoodEnhancement, StarshipEnhancement,
        /// ModuleEnhancement, DroidEnhancement). These mark an item AS an enhancement module
        /// (Craft.IsItemEnhancement in SWLOR.Game.Server) rather than counting gear slots; gear
        /// slots come from recipes instead. Used only by <see cref="ItemMultiEntryCatalog"/> to
        /// tag which context those properties surface under - <see cref="ItemStatCatalog"/>
        /// carries no stats of its own in this group.
        /// </summary>
        Enhancements
    }
}
