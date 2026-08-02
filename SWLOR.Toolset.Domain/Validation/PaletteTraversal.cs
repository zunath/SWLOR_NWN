using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Validation
{
    /// <summary>Shared recursive-tree traversal over an <see cref="ItpDocument"/>'s palette nodes,
    /// used by every rule that needs to enumerate a palette's leaf blueprint references.</summary>
    internal static class PaletteTraversal
    {
        /// <summary>Every node in the palette tree that carries a non-empty RESREF, at any depth
        /// (categories are skipped implicitly - they have no RESREF).</summary>
        public static IEnumerable<PaletteNode> EnumerateLeaves(ItpDocument document)
        {
            foreach (var node in document.Nodes)
                foreach (var leaf in EnumerateLeaves(node))
                    yield return leaf;
        }

        private static IEnumerable<PaletteNode> EnumerateLeaves(PaletteNode node)
        {
            if (node.DeleteMe == true)
                yield break;

            if (!string.IsNullOrEmpty(node.ResRef))
                yield return node;

            foreach (var child in node.Children)
                foreach (var leaf in EnumerateLeaves(child))
                    yield return leaf;
        }
    }
}
