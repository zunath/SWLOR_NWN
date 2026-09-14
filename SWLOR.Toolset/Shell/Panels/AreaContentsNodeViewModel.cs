using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// One row of the Area Contents tree: a kind, a group of alike placements, a single placement,
    /// or the "... n more" tail of an overlong group.
    /// </summary>
    /// <remarks>
    /// Rows carry their display strings rather than computing them from a live model, because a row
    /// is rebuilt whenever anything it describes changes and there is no state to keep in step.
    /// </remarks>
    public partial class AreaContentsNodeViewModel : ObservableObject
    {
        public AreaContentsNodeViewModel(
            AreaContentsNodeKind kind,
            ResourceType blueprintType,
            string name,
            int depth)
        {
            Kind = kind;
            BlueprintType = blueprintType;
            Name = name;
            Depth = depth;
        }

        public AreaContentsNodeKind Kind { get; }

        /// <summary>Which instance list this row lives in - the section a delete or select acts on.</summary>
        public ResourceType BlueprintType { get; }

        public string Name { get; }

        /// <summary>The right-hand column: a resref, a position, or a count.</summary>
        public string Detail { get; init; } = string.Empty;

        public int Depth { get; }

        /// <summary>
        /// The list indices this row stands for: one for a placement, every member for a group,
        /// none for a kind heading or an overflow tail.
        /// </summary>
        /// <remarks>
        /// Indices rather than rows, because that is what both things a row can do take - selecting
        /// one (AreaEditorViewModel.RevealInstance) and deleting the set (DeleteInstances) - and
        /// holding InstanceRow references across a refresh would keep stale objects alive.
        /// </remarks>
        public IReadOnlyList<int> Indices { get; init; } = Array.Empty<int>();

        /// <summary>Where the camera goes when this row is opened. Null for anything but a placement.</summary>
        public System.Numerics.Vector3? Position { get; init; }

        public ObservableCollection<AreaContentsNodeViewModel> Children { get; } = new();

        [ObservableProperty]
        private bool _isExpanded;

        public bool IsBranch => Kind is AreaContentsNodeKind.Kind or AreaContentsNodeKind.Group;

        public bool IsInstance => Kind == AreaContentsNodeKind.Instance;

        /// <summary>
        /// True when this row identifies placements whose properties can be opened. A group is an
        /// object-bearing row too; its action opens the first member and says so explicitly.
        /// </summary>
        public bool CanOpenProperties =>
            (Kind is AreaContentsNodeKind.Instance or AreaContentsNodeKind.Group) && Indices.Count > 0;

        public string OpenPropertiesLabel => Kind == AreaContentsNodeKind.Group
            ? "Open first instance properties..."
            : "Open properties...";

        /// <summary>True for anything a Delete keypress may act on.</summary>
        public bool IsDeletable => Indices.Count > 0;

        /// <summary>A kind with nothing in it still shows, dimmed - "no doors here" is an answer.</summary>
        public bool IsEmptyKind => Kind == AreaContentsNodeKind.Kind && Indices.Count == 0 &&
                                   Children.Count == 0;

        public bool IsDimmed => IsEmptyKind || Kind == AreaContentsNodeKind.Overflow;

        /// <summary>Blank unless the row has children, so only branches show a twisty.</summary>
        public string Twisty => Children.Count == 0 ? string.Empty : IsExpanded ? "▾" : "▸";

        /// <summary>Matches Module Contents: 16px a level, so the two trees indent alike.</summary>
        public Avalonia.Thickness Indent => new(6 + Depth * 16, 0, 0, 0);

        public Avalonia.Media.FontWeight Weight =>
            Kind == AreaContentsNodeKind.Kind
                ? Avalonia.Media.FontWeight.SemiBold
                : Avalonia.Media.FontWeight.Normal;

        partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(Twisty));
    }
}
