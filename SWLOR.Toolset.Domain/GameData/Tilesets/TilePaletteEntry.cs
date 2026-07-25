namespace SWLOR.Toolset.Domain.GameData.Tilesets
{
    /// <summary>
    /// One pickable thing in the tile palette: either a single tile, or a named multi-tile group
    /// that is placed as a unit.
    /// </summary>
    /// <param name="Label">What the palette shows. Never blank.</param>
    /// <param name="TileIds">
    /// The tile ids to place, ROW-MAJOR (index = row * <paramref name="Columns"/> + column) - the
    /// same order <see cref="AreaTiles"/> addresses the area's Tile_List in, and the same order the
    /// .set file's own Tile0..Tile{n-1} keys are written in. One entry for a single tile;
    /// <paramref name="Rows"/> * <paramref name="Columns"/> entries for a group.
    /// A slot may be -1, meaning "this cell of the rectangle is not part of the group" - leave
    /// whatever is already in the area alone there. See <see cref="TilePaletteBuilder"/>.
    /// </param>
    /// <param name="PreviewModelResRef">
    /// The model to render a thumbnail from, or "" when there is none. For a group this is the
    /// first non-hole tile's model rather than a composite: a thumbnail only has to be
    /// recognizable, and every corpus group's first real tile is a distinctive piece of it.
    /// </param>
    /// <param name="Terrain">
    /// The terrain this entry paints, or null when it is a fixed stamp. A terrain entry does not
    /// write <paramref name="TileIds"/> literally: it hands the clicked cell to
    /// <see cref="TilePainter.PaintTerrain"/>, which fills it and re-blends the eight neighbours so
    /// the edges match. <paramref name="TileIds"/> then holds only a representative tile, used for
    /// the thumbnail.
    /// </param>
    public sealed record TilePaletteEntry(
        string Label,
        IReadOnlyList<int> TileIds,
        int Columns,
        int Rows,
        string PreviewModelResRef,
        string? Terrain = null);
}
