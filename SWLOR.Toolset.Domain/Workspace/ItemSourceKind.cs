namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// How a player can obtain an item, as classified by <see cref="ItemObtainabilityIndex"/>.
    /// Ordering here is also the Source tab's fixed display order (Store, Recipe, Loot, Quest,
    /// Npc, Container, Other).
    /// </summary>
    public enum ItemSourceKind
    {
        /// <summary>Sold by a vendor (.utm) store.</summary>
        Store,

        /// <summary>Produced as a crafting recipe output, or consumed as a recipe component.</summary>
        Recipe,

        /// <summary>Granted by a loot table, or by a slicing lockbox/terminal reward roll.</summary>
        Loot,

        /// <summary>Granted as a quest reward.</summary>
        Quest,

        /// <summary>Carried as a droppable inventory item by a spawnable NPC (.utc).</summary>
        Npc,

        /// <summary>Found in a placed container's default inventory.</summary>
        Container,

        /// <summary>
        /// A direct literal item grant (CreateItemOnObject/CopyItemAndModify) or a fixed
        /// system-granted resref, where no more specific kind applies.
        /// </summary>
        Other
    }
}
