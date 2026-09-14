namespace SWLOR.Toolset.Domain.GameData.Tilesets
{
    /// <summary>
    /// Which of a tile palette's categories each <see cref="TilePaintMode"/> offers.
    /// </summary>
    /// <remarks>
    /// The two modes are not two palettes: they are two answers to "what am I choosing when I click".
    /// In Auto the choice is a terrain and the tileset picks the tile; in Manual the choice is the
    /// tile itself. Filtering here rather than building two palettes keeps
    /// <see cref="TilePaletteBuilder"/> as the single description of what a tileset contains, and
    /// means a mode switch costs a filter rather than a re-parse.
    /// </remarks>
    public static class TilePaintModes
    {
        /// <summary>
        /// The categories <paramref name="mode"/> shows, in palette order.
        /// </summary>
        /// <remarks>
        /// Features and Groups appear in both. Each is a fixed arrangement the tileset author named -
        /// an elevator, a subway station - so neither is solved from terrain nor chosen as a bare tile,
        /// and hiding them in either mode would only mean switching modes to reach them. Aurora lists
        /// both alongside Terrain for the same reason.
        /// </remarks>
        public static IReadOnlyList<TilePaletteCategory> CategoriesFor(TilePalette palette, TilePaintMode mode)
        {
            ArgumentNullException.ThrowIfNull(palette);

            return palette.Categories.Where(category => Offers(category.Name, mode)).ToList();
        }

        /// <summary>Whether <paramref name="mode"/> offers the category called <paramref name="categoryName"/>.</summary>
        public static bool Offers(string categoryName, TilePaintMode mode)
        {
            if (string.Equals(categoryName, TilePaletteBuilder.FeaturesCategoryName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(categoryName, TilePaletteBuilder.GroupsCategoryName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return mode == TilePaintMode.Auto
                ? string.Equals(categoryName, TilePaletteBuilder.TerrainCategoryName, StringComparison.OrdinalIgnoreCase)
                : string.Equals(categoryName, TilePaletteBuilder.AllTilesCategoryName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
