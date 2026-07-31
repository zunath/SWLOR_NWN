namespace SWLOR.Toolset.Domain.Editors.Merchants
{
    /// <summary>
    /// The five store panes in the exact positional order the engine and Aurora toolset use.
    /// The pane struct IDs vary in legacy files, so position rather than struct ID is authoritative.
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
