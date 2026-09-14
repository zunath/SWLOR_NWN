using System.Collections.ObjectModel;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// One node of a browsed palette tree: either a category or a blueprint leaf.
    /// </summary>
    public sealed class PaletteNodeViewModel
    {
        public string? ResRef { get; }
        public bool IsLeaf => ResRef != null;
        public string DisplayName { get; }
        public ObservableCollection<PaletteNodeViewModel> Children { get; } = new();

        /// <param name="node">The palette entry this node wraps.</param>
        /// <param name="resolveStrRef">
        /// Resolves an entry that names itself by STRREF rather than NAME. The checked-in palettes lean
        /// on this heavily - the placeable palette alone labels 1,525 leaves by custom-TLK reference -
        /// so without a resolver the picker lists numbers where the rest of the app lists names.
        /// </param>
        public PaletteNodeViewModel(PaletteNode node, Func<uint, string?>? resolveStrRef)
        {
            ResRef = string.IsNullOrWhiteSpace(node.ResRef) ? null : node.ResRef.Trim();

            var resolved = node.StrRef.HasValue ? resolveStrRef?.Invoke(node.StrRef.Value) : null;
            DisplayName = !string.IsNullOrWhiteSpace(node.Name)
                ? node.Name
                : !string.IsNullOrWhiteSpace(resolved)
                    ? resolved
                    : ResRef ?? (node.StrRef.HasValue ? $"(strref {node.StrRef.Value})" : "(unnamed)");

            // DELETE_ME entries are tombstones Aurora leaves behind; the game ignores them and so
            // does the category importer, so offering one here would place a blueprint the module
            // has already retired.
            foreach (var child in node.Children.Where(candidate => candidate.DeleteMe != true))
                Children.Add(new PaletteNodeViewModel(child, resolveStrRef));
        }
    }
}
