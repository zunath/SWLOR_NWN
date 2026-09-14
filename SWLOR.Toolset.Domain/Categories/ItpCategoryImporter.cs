using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Categories
{
    /// <summary>
    /// Seeds a <see cref="CategorySection"/> from an NWN palette (<c>.itp</c>) tree, so a builder who
    /// already has categories does not start from an empty one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Import only.</b> The toolset reads <c>.itp</c> files and never writes them, because the game and
    /// the Aurora toolset rewrite them - anything saved there is eventually lost. Run this once to
    /// populate the sidecar, then the sidecar owns the arrangement.
    /// </para>
    /// <para>
    /// Three shapes appear in the real files and each needs different handling. A node carrying
    /// <c>RESREF</c> is a blueprint, and becomes a member of the folder enclosing it. A node carrying
    /// <c>NAME</c> or <c>STRREF</c> plus a child list is a category. A node with a child list but neither
    /// name is a transparent wrapper - the corpus puts one at the root of every palette - so its children
    /// are hoisted into its parent rather than filed under a nameless folder.
    /// </para>
    /// </remarks>
    public static class ItpCategoryImporter
    {
        /// <summary>
        /// Builds a section from a palette tree. <paramref name="resolveStrRef"/> supplies category names
        /// for the base-game palettes, which label categories by TLK reference rather than by string;
        /// without it those folders fall back to a legible placeholder that a builder can rename.
        /// </summary>
        public static CategorySection Import(ItpDocument document, Func<uint, string?>? resolveStrRef = null) =>
            Import(document, out _, resolveStrRef);

        /// <summary>
        /// Builds a section and also hands back the display name each leaf declares.
        /// </summary>
        /// <remarks>
        /// A palette leaf carries a NAME or a STRREF of its own, and those are the only names the base
        /// game has for its blueprints - there is no module file to read one from. Discarding them is why
        /// the Standard palette showed a cryptic resref for nearly every entry.
        /// </remarks>
        public static CategorySection Import(
            ItpDocument document,
            out IReadOnlyDictionary<string, string> leafNames,
            Func<uint, string?>? resolveStrRef = null)
        {
            ArgumentNullException.ThrowIfNull(document);

            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var section = new CategorySection();
            foreach (var folder in ImportChildren(document.Nodes, resolveStrRef, names: names))
                section.AddFolder(folder);

            leafNames = names;
            return section;
        }

        /// <summary>
        /// Converts a level of palette nodes into folders. Members found at this level belong to the
        /// caller's folder, so they are surfaced through <paramref name="membersForParent"/> rather than
        /// invented a folder for.
        /// </summary>
        private static List<CategoryFolder> ImportChildren(
            IEnumerable<PaletteNode> nodes,
            Func<uint, string?>? resolveStrRef,
            List<string>? membersForParent = null,
            Dictionary<string, string>? names = null)
        {
            var folders = new List<CategoryFolder>();

            foreach (var node in nodes)
            {
                if (node.DeleteMe == true)
                    continue;

                if (!string.IsNullOrWhiteSpace(node.ResRef))
                {
                    var resRef = node.ResRef.Trim();
                    membersForParent?.Add(resRef);

                    if (names != null && ResolveName(node, resolveStrRef) is { } leafName)
                        names[resRef] = leafName;

                    continue;
                }

                var children = node.Children;
                if (children.Count == 0)
                    continue;

                var name = ResolveName(node, resolveStrRef, out var isPlaceholder);
                if (name == null)
                {
                    // Transparent wrapper: hoist rather than create a folder nobody can name or find.
                    folders.AddRange(ImportChildren(children, resolveStrRef, membersForParent, names));
                    continue;
                }

                var folder = new CategoryFolder(name) { IsUnresolvedPlaceholder = isPlaceholder };
                var members = new List<string>();
                foreach (var child in ImportChildren(children, resolveStrRef, members, names))
                    folder.AddChild(child);

                foreach (var member in members)
                    folder.AddMember(member);

                // An empty branch carries no arrangement worth importing - the base-game palettes are
                // full of categories this module never filed anything into.
                if (folder.MembersIncludingDescendants.Any())
                    folders.Add(folder);
            }

            return folders;
        }

        /// <summary>
        /// A palette node's display name, or null when it has none to offer.
        /// </summary>
        /// <remarks>
        /// Sanitised, because these names are the base game's rather than ours and a folder name may not
        /// hold a path separator: the standard item palette really does ship "Skin/Hide" and
        /// "Crafting/Tradeskill Material", and importing those verbatim threw. Repairing here is what keeps
        /// the seeder from writing a name the reader would then have to repair again.
        /// </remarks>
        private static string? ResolveName(PaletteNode node, Func<uint, string?>? resolveStrRef) =>
            ResolveName(node, resolveStrRef, out _);

        /// <summary>
        /// As above, and also reports whether the returned name is the "Category N" placeholder text
        /// invented because <paramref name="resolveStrRef"/> had nothing to offer. This is the one place
        /// that fact is known for certain - the caller only sees the resulting string - so the marker is
        /// set here rather than reconstructed later from the text.
        /// </summary>
        private static string? ResolveName(
            PaletteNode node, Func<uint, string?>? resolveStrRef, out bool isPlaceholder)
        {
            isPlaceholder = false;

            if (!string.IsNullOrWhiteSpace(node.Name))
                return CategoryFolder.Sanitize(node.Name);

            if (node.StrRef is not { } strRef)
                return null;

            var resolved = resolveStrRef?.Invoke(strRef);
            var sanitized = string.IsNullOrWhiteSpace(resolved) ? null : CategoryFolder.Sanitize(resolved);
            if (sanitized != null)
                return sanitized;

            isPlaceholder = true;
            return $"Category {strRef}";
        }
    }
}
