using CommunityToolkit.Mvvm.ComponentModel;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// One row of the Module Explorer's item list - either a group header or a resource under it.
    /// </summary>
    /// <remarks>
    /// Group headers and items share one flat, virtualized list for the same reason the palette's
    /// category tree does: the module has 443 areas and 8,355 placeables, and a TreeView would realise a
    /// container for every one of them. Collapsing a group re-publishes the list rather than tearing down
    /// nested containers.
    /// </remarks>
    public partial class ExplorerRowViewModel : ObservableObject
    {
        private ExplorerRowViewModel(string groupName, int count)
        {
            IsGroup = true;
            GroupName = groupName;
            Count = count;
            IsExpanded = true;
        }

        private ExplorerRowViewModel(ExplorerItem item, string label)
        {
            Item = item;
            Label = label;
        }

        public static ExplorerRowViewModel Group(string name, int count) => new(name, count);

        /// <summary>
        /// An item row. <paramref name="label"/> is the name with its group prefix removed - under
        /// "Viscara" the row reads "Veles" rather than repeating "Viscara - Veles" on every line.
        /// </summary>
        public static ExplorerRowViewModel Resource(ExplorerItem item, string label) => new(item, label);

        public bool IsGroup { get; }

        public bool IsResource => !IsGroup;

        public string? GroupName { get; }

        public ExplorerItem? Item { get; }

        public string Label { get; } = string.Empty;

        public string ResRef => Item?.ResRef ?? string.Empty;

        public int Count { get; }

        [ObservableProperty]
        private bool _isExpanded;

        public string Twisty => IsExpanded ? "▾" : "▸";
    }
}
