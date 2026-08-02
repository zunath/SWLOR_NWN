using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Editors.Behaviors
{
    /// <summary>
    /// Turns a palette (.itp) into the named categories a blueprint's PaletteID can point at, so the
    /// editor can offer "Traps" instead of the number 14.
    /// </summary>
    /// <remarks>
    /// A palette mixes two kinds of node. Some carry an ID and are the real categories; others are
    /// pure grouping and carry only a name, with the ID-bearing categories nested inside. Both are
    /// walked, and a nested category is labelled with its parent so "Special ▸ Traps" reads
    /// unambiguously when two branches reuse a word.
    /// </remarks>
    public static class PaletteCategoryReader
    {
        public static IReadOnlyList<BehaviorChoice> Read(
            ItpDocument palette, Func<uint, string?>? resolveStrRef = null)
        {
            ArgumentNullException.ThrowIfNull(palette);

            var categories = new List<BehaviorChoice>();
            Walk(palette.Nodes, parentName: null, resolveStrRef, categories);

            return categories
                .GroupBy(category => category.Value)
                .Select(group => group.First())
                .OrderBy(category => category.Display, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void Walk(
            IReadOnlyList<PaletteNode> nodes,
            string? parentName,
            Func<uint, string?>? resolveStrRef,
            List<BehaviorChoice> categories)
        {
            foreach (var node in nodes)
            {
                if (node.ResRef != null)
                    continue;

                var name = NameOf(node, resolveStrRef);
                var qualified = parentName == null ? name : $"{parentName} ▸ {name}";

                if (node.Id is { } id)
                    categories.Add(new BehaviorChoice(id, qualified));

                if (node.Children.Count > 0)
                    Walk(node.Children, qualified, resolveStrRef, categories);
            }
        }

        /// <summary>
        /// A node's own NAME wins; otherwise its STRREF is resolved. An unresolvable strref falls back
        /// to naming the number rather than showing a blank row, since a blank one is unpickable.
        /// </summary>
        private static string NameOf(PaletteNode node, Func<uint, string?>? resolveStrRef)
        {
            if (!string.IsNullOrWhiteSpace(node.Name))
                return node.Name!;

            if (node.StrRef is { } strRef)
            {
                var resolved = resolveStrRef?.Invoke(strRef);
                if (!string.IsNullOrWhiteSpace(resolved))
                    return resolved!;

                return $"Category {node.Id?.ToString() ?? strRef.ToString()}";
            }

            return node.Id is { } id ? $"Category {id}" : "Category";
        }
    }
}
