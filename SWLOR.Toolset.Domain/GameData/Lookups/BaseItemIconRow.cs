namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// The baseitems.2da columns that decide what an item's inventory icon is called.
    /// </summary>
    /// <param name="Id">The baseitems.2da row index, which is what a uti's BaseItem field stores.</param>
    /// <param name="ModelType">
    /// How the item's model - and therefore its icon - is assembled: 0 simple, 1 layered part
    /// (helmets, cloaks), 2 composite bottom/middle/top (most weapons), 3 armor.
    /// </param>
    /// <param name="ItemClass">
    /// The resource-name stem shared by the item's models and icons ("it_belt", "WSwLs"). Icons are
    /// this with an "i" in front.
    /// </param>
    /// <param name="DefaultIcon">
    /// The generic icon for the base item, used by the game when the specific part has no icon of its
    /// own. Blank on a few reserved rows.
    /// </param>
    public sealed record BaseItemIconRow(int Id, int ModelType, string? ItemClass, string? DefaultIcon);
}
