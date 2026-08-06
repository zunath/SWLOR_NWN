namespace SWLOR.Toolset.Domain.Editors.Merchants
{
    /// <summary>
    /// The five values baseitems.2da's StorePanel column uses. An item's BaseItem row is the
    /// authority for which pane it belongs in; legacy UTM files are not assumed to be correct.
    /// </summary>
    public enum MerchantInventoryCategory
    {
        Armor = 0,
        Weapons = 1,
        PotionsScrolls = 2,
        RingsAmulets = 3,
        Miscellaneous = 4
    }
}
