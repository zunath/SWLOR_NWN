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
    /// The terrain this entry paints, or null when it is a fixed stamp or a crosser. A terrain
    /// entry does not write <paramref name="TileIds"/> literally: it names a grid VERTEX that
    /// <see cref="TilePainter.PaintTerrainVertex"/> re-solves the surrounding cells against.
    /// <paramref name="TileIds"/> then holds only a representative tile, used for the thumbnail.
    /// </param>
    /// <param name="Crosser">
    /// The edge crosser this entry paints (road, bridge, wall, ...), or null otherwise. A crosser
    /// entry names a grid EDGE that <see cref="TilePainter.PaintCrosserEdge"/> re-solves the two
    /// adjacent cells against - the reference toolset's model, verified against it live. Never set
    /// together with <paramref name="Terrain"/>.
    /// </param>
    /// <param name="FootprintModelResRefs">
    /// One model resref per footprint slot, ROW-MAJOR over <paramref name="Columns"/> and empty for a
    /// hole - what a multi-tile group's thumbnail composes so it shows its real shape rather than
    /// its first tile. Empty for a single tile or a brush, where
    /// <paramref name="PreviewModelResRef"/> already says everything.
    /// </param>
    public sealed record TilePaletteEntry(
        string Label,
        IReadOnlyList<int> TileIds,
        int Columns,
        int Rows,
        string PreviewModelResRef,
        string? Terrain = null,
        string? Crosser = null,
        IReadOnlyList<string>? FootprintModelResRefs = null);
}
