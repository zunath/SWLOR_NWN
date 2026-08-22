namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// The broad shape a .uti blueprint falls into, decided from its baseitems.2da row. Drives
    /// which stat groups and roles an item editor offers - a helmet has no business seeing the
    /// crafting matrix, and a creature weapon has no business seeing player-facing stats at all.
    /// </summary>
    public enum ItemFamily
    {
        MeleeWeapon,
        RangedWeapon,
        Lightsaber,
        Armor,
        Helmet,
        Cape,
        Shield,
        Accessory,
        Tool,
        CreatureItem,
        Essence,
        Miscellaneous
    }
}
