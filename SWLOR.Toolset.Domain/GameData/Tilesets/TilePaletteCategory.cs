namespace SWLOR.Toolset.Domain.GameData.Tilesets
{
    /// <summary>
    /// One heading in the tile palette and the entries filed under it. A category is only ever
    /// constructed with at least one entry - <see cref="TilePaletteBuilder"/> omits an empty
    /// category rather than showing a header with nothing under it.
    /// </summary>
    public sealed record TilePaletteCategory(string Name, IReadOnlyList<TilePaletteEntry> Entries);
}
