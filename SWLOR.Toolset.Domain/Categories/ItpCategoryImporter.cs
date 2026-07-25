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
        public static CategorySection Import(ItpDocument document, Func<uint, string?>? resolveStrRef = null)
        {
            ArgumentNullException.ThrowIfNull(document);

            var section = new CategorySection();
            foreach (var folder in ImportChildren(document.Nodes, resolveStrRef))
                section.AddFolder(folder);

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
            List<string>? membersForParent = null)
        {
            var folders = new List<CategoryFolder>();

            foreach (var node in nodes)
            {
                if (node.DeleteMe == true)
                    continue;

                if (!string.IsNullOrWhiteSpace(node.ResRef))
                {
                    membersForParent?.Add(node.ResRef.Trim());
                    continue;
                }

                var children = node.Children;
                if (children.Count == 0)
                    continue;

                var name = ResolveName(node, resolveStrRef);
                if (name == null)
                {
                    // Transparent wrapper: hoist rather than create a folder nobody can name or find.
                    folders.AddRange(ImportChildren(children, resolveStrRef, membersForParent));
                    continue;
                }

                var folder = new CategoryFolder(name);
                var members = new List<string>();
                foreach (var child in ImportChildren(children, resolveStrRef, members))
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

        private static string? ResolveName(PaletteNode node, Func<uint, string?>? resolveStrRef)
        {
            if (!string.IsNullOrWhiteSpace(node.Name))
                return node.Name.Trim();

            if (node.StrRef is not { } strRef)
                return null;

            var resolved = resolveStrRef?.Invoke(strRef);
            return string.IsNullOrWhiteSpace(resolved) ? $"Category {strRef}" : resolved.Trim();
        }
    }
}
