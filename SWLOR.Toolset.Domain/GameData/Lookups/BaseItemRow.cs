namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// baseitems.2da's label, model, store, and equipment-slot columns, keyed by row - what a UTI's
    /// BaseItem field stores and what the item and merchant editors use to classify it. Separate
    /// from <see cref="BaseItemIconRow"/>, which reads the icon-naming columns
    /// (ItemClass/DefaultIcon) instead.
    /// </summary>
    /// <param name="Id">The baseitems.2da row index, matching a uti's BaseItem field.</param>
    /// <param name="Label">The base item's identifier label ("shortsword", "armor", "ess2").</param>
    /// <param name="ModelType">0 simple icon, 1 layered part, 2 composite, 3 armor.</param>
    /// <param name="StorePanel">The native merchant category: 0 armor, 1 weapons,
    /// 2 potions/scrolls, 3 rings/amulets, or 4 miscellaneous.</param>
    /// <param name="EquipableSlots">Aurora equipment-slot bitmask. A bit is set when an item of
    /// this base type can occupy the corresponding UTC Equip_ItemList slot.</param>
    public sealed record BaseItemRow(
        int Id,
        string Label,
        int ModelType,
        int StorePanel = 4,
        int EquipableSlots = 0);
}
