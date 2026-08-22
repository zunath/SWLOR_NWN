namespace SWLOR.Toolset.Editors.Appearance
{
    /// <summary>
    /// One choice in an appearance grid, described the same way whatever the editor is picking.
    /// </summary>
    /// <param name="Key">
    /// Stable identity for the choice. Compared to decide which tile is the current one, so it has
    /// to distinguish rows the caption does not — a door's generic row 12 from its specific row 12.
    /// </param>
    /// <param name="Caption">What the tile is called.</param>
    /// <param name="Detail">The model or row underneath the caption; null hides that line.</param>
    /// <param name="ModelResRef">
    /// A model to draw the tile from, when the choice names one directly. Null when the preview
    /// comes from <paramref name="CreatureAppearanceId"/> or when there is nothing to draw.
    /// </param>
    /// <param name="CreatureAppearanceId">
    /// An <c>appearance.2da</c> row to draw the tile from. Half of that table names a phenotype
    /// rather than a model, so those rows can only be drawn by composing a creature — which is what
    /// the renderer does with this.
    /// </param>
    /// <param name="IsSegmentedCreatureAppearance">
    /// True for MODELTYPE P rows, whose representative preview composes a generic full creature
    /// for that row's race rather than rendering one fixed model resource.
    /// </param>
    /// <param name="IsDoorTransition">
    /// True when a door choice needs hidden editor geometry or the fixed doorway fallback instead
    /// of an ordinary visible-model thumbnail.
    /// </param>
    public sealed record AppearanceOption(
        string Key,
        string Caption,
        string? Detail,
        string? ModelResRef = null,
        int? CreatureAppearanceId = null,
        bool IsSegmentedCreatureAppearance = false,
        bool IsDoorTransition = false)
    {
        /// <summary>Everything the search box matches against, lowercased once at construction.</summary>
        public string SearchText { get; } =
            $"{Caption} {Detail}".ToLowerInvariant();
    }
}
