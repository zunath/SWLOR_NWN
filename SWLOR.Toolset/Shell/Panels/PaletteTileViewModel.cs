namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// One blueprint in the palette grid: what it is called, its resref, and - when the palette is
    /// showing search results rather than one folder - which category it came from.
    /// </summary>
    /// <remarks>
    /// The name is the primary text and the resref sits under it in monospace, the same pairing the
    /// explorer rows use. <see cref="CategoryPath"/> is only populated for search results, where knowing
    /// where a hit lives is half the answer and doubles as learning the tree.
    /// </remarks>
    public sealed record PaletteTileViewModel(string ResRef, string Name, string? CategoryPath)
    {
        public bool HasCategoryPath => !string.IsNullOrEmpty(CategoryPath);

        /// <summary>Shown until blueprint thumbnails exist; the first letter of the name reads better than a generic box.</summary>
        public string Glyph => string.IsNullOrWhiteSpace(Name) ? "?" : Name.Trim()[..1].ToUpperInvariant();
    }
}
