namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// baseitems.2da's label and ModelType columns, keyed by row - what a uti's BaseItem field
    /// stores and what <see cref="Editors.Items.ItemFamilyClassifier"/> needs to place an item in
    /// a family. Separate from <see cref="BaseItemIconRow"/>, which reads the icon-naming columns
    /// (ItemClass/DefaultIcon) instead.
    /// </summary>
    /// <param name="Id">The baseitems.2da row index, matching a uti's BaseItem field.</param>
    /// <param name="Label">The base item's identifier label ("shortsword", "armor", "ess2").</param>
    /// <param name="ModelType">0 simple icon, 1 layered part, 2 composite, 3 armor.</param>
    public sealed record BaseItemRow(int Id, string Label, int ModelType);
}
