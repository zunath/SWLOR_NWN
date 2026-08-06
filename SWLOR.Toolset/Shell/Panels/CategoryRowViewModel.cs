using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Categories;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// One row of the palette's category tree, flattened.
    /// </summary>
    /// <remarks>
    /// A flat list with an explicit <see cref="Depth"/> rather than a real TreeView, because Avalonia's
    /// TreeView does not virtualize its items and this tree is expected to reach hundreds of categories -
    /// the same lesson the Module Explorer already learned when a type's items were put in one.
    /// Expanding and collapsing rebuilds the flat list instead of realising nested containers.
    /// </remarks>
    public partial class CategoryRowViewModel : ObservableObject
    {
        public CategoryRowViewModel(CategoryFolder? folder, int depth, int count, bool hasChildren)
        {
            Folder = folder;
            Depth = depth;
            Count = count;
            HasChildren = hasChildren;
        }

        /// <summary>The folder this row shows, or null for the synthetic Unsorted row.</summary>
        public CategoryFolder? Folder { get; }

        /// <summary>
        /// A name for a row that has no folder behind it - the tile palette's categories, which come from
        /// a tileset rather than the category sidecar. Leaving this null keeps the old meaning of a
        /// folderless row: the synthetic Unsorted bucket.
        /// </summary>
        public string? SyntheticName { get; init; }

        public int Depth { get; }

        public bool HasChildren { get; }

        /// <summary>True for the synthetic Unsorted row, which is generated rather than stored.</summary>
        public bool IsUnsorted => Folder == null && SyntheticName == null;

        public bool IsPinned { get; init; }

        public string Name => Folder?.Name ?? SyntheticName ?? CategorySection.UnsortedFolderName;

        [ObservableProperty]
        private int _count;

        [ObservableProperty]
        private bool _isExpanded;

        /// <summary>Indentation for the row, 15px per level - deep enough to read, shallow enough to nest.</summary>
        public Avalonia.Thickness Indent => new(11 + Depth * 15, 0, 0, 0);

        /// <summary>The twisty, blank when the row has nothing beneath it.</summary>
        public string Twisty => !HasChildren ? string.Empty : IsExpanded ? "▾" : "▸";
    }
}
