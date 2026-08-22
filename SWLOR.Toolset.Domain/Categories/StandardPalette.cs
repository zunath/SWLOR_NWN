namespace SWLOR.Toolset.Domain.Categories
{
    /// <summary>
    /// The base game's own palette tree for one blueprint type, as read out of a <c>*palstd.itp</c>:
    /// Aurora's "Standard" half of the palette, alongside the module's "Custom" content.
    /// </summary>
    /// <remarks>
    /// This is a description of what the game ships, not a record of any decision a builder made, so it
    /// is deliberately not part of <see cref="CategoryCatalog"/> and therefore cannot be written to the
    /// category sidecar. Nothing here is loaded from or saved to disk on the toolset's behalf - the only
    /// source is the base-game resource layer.
    /// </remarks>
    public sealed class StandardPalette
    {
        /// <summary>What every failure and every unsupported type resolves to: no folders, no resrefs.</summary>
        public static StandardPalette Empty { get; } = new(
            new CategorySection(),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        internal StandardPalette(
            CategorySection section,
            IReadOnlySet<string> resRefs,
            IReadOnlyDictionary<string, string> names)
        {
            Section = section;
            ResRefs = resRefs;
            Names = names;
        }

        /// <summary>
        /// Display names the palette file declares for its own entries, by resref.
        /// </summary>
        /// <remarks>
        /// The base game's blueprints are not in the module, so the module catalog has no name for them.
        /// The palette file is the only place one exists.
        /// </remarks>
        public IReadOnlyDictionary<string, string> Names { get; }

        /// <summary>The imported category tree. Empty when the base game or the palette is unavailable.</summary>
        public CategorySection Section { get; }

        /// <summary>
        /// The resrefs this palette offers that actually resolve to a real resource. Verified against the
        /// resource index rather than taken from the palette file, which lists blueprints an install may
        /// not have (expansion content, cut resources) and would otherwise show as dead tiles.
        /// </summary>
        public IReadOnlySet<string> ResRefs { get; }

        /// <summary>True when there is nothing to show, so a caller can skip the group entirely.</summary>
        public bool IsEmpty => ResRefs.Count == 0 && Section.Folders.Count == 0;
    }
}
