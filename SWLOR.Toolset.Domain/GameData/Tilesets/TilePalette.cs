namespace SWLOR.Toolset.Domain.GameData.Tilesets
{
    /// <summary>
    /// A browsable, categorised view of one tileset's placeable content, built by
    /// <see cref="TilePaletteBuilder"/> from a parsed <see cref="TilesetDefinition"/>.
    /// </summary>
    /// <remarks>
    /// Pure presentation shaping - it holds no reference to the tileset, the resource index, or any
    /// renderer, so it can be handed to a palette panel and thrown away when the open area's
    /// tileset changes.
    /// </remarks>
    public sealed class TilePalette
    {
        /// <summary>What every failure and every absent tileset resolves to: no categories at all.</summary>
        public static TilePalette Empty { get; } = new(Array.Empty<TilePaletteCategory>());

        internal TilePalette(IReadOnlyList<TilePaletteCategory> categories)
        {
            Categories = categories;
        }

        public IReadOnlyList<TilePaletteCategory> Categories { get; }

        /// <summary>True when there is nothing to show, so a caller can skip the panel entirely.</summary>
        public bool IsEmpty => Categories.Count == 0;
    }
}
