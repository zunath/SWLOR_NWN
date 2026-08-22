namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// One pickable appearance in the placeable model grid: the 2DA row id that gets stored, the
    /// model that gets rendered, and whatever there is to call it.
    /// </summary>
    /// <param name="Id">placeables.2da row index, which is what a placeable's Appearance holds.</param>
    /// <param name="ModelName">The model resref, always present - a row without one is not pickable.</param>
    /// <param name="DisplayName">The row's label, falling back to the model resref.</param>
    /// <param name="HasLabel">
    /// False for the majority of the table. Of the 24,304 rows carrying a model, 15,761 have no
    /// label at all, so the caption under a tile is a model name rather than a name - and searching
    /// has to match either.
    /// </param>
    public sealed record PlaceableModelRow(int Id, string ModelName, string DisplayName, bool HasLabel);
}
