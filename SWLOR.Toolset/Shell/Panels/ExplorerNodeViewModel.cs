using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>What a row in the Module Contents tree stands for.</summary>
    public enum ExplorerNodeKind
    {
        /// <summary>A resource type: Areas, Creatures, Placeables.</summary>
        Type,

        /// <summary>A group inside a type - a planet, or a user folder.</summary>
        Group,

        /// <summary>One area or blueprint.</summary>
        Resource
    }

    /// <summary>
    /// One node of the Module Contents tree: type, group, or resource.
    /// </summary>
    /// <remarks>
    /// Children are built on first expand rather than up front. The module has 8,355 placeables and
    /// 7,651 items; realising every row for every type at startup would cost seconds and almost all of
    /// it would be thrown away, since a builder works in one type at a time.
    /// </remarks>
    public partial class ExplorerNodeViewModel : ObservableObject
    {
        public ExplorerNodeViewModel(ExplorerNodeKind kind, ResourceType type, string name, int depth)
        {
            Kind = kind;
            Type = type;
            Name = name;
            Depth = depth;
        }

        public ExplorerNodeKind Kind { get; }

        public ResourceType Type { get; }

        public string Name { get; }

        public int Depth { get; }

        /// <summary>Set for resource nodes only.</summary>
        public ExplorerItem? Item { get; init; }

        public string ResRef => Item?.ResRef ?? string.Empty;

        public bool IsResource => Kind == ExplorerNodeKind.Resource;

        public bool IsBranch => Kind != ExplorerNodeKind.Resource;

        public ObservableCollection<ExplorerNodeViewModel> Children { get; } = new();

        /// <summary>False until this node's children have been built, which happens on first expand.</summary>
        public bool IsLoaded { get; set; }

        [ObservableProperty]
        private int _count;

        [ObservableProperty]
        private bool _isExpanded;

        /// <summary>Blank for resources, so only branches show a twisty.</summary>
        public string Twisty => IsResource ? string.Empty : IsExpanded ? "▾" : "▸";

        /// <summary>16px per level, with resources sitting one notch past their group.</summary>
        public Avalonia.Thickness Indent => new(6 + Depth * 16, 0, 0, 0);

        /// <summary>Types read as headings; groups as sub-headings; resources as content.</summary>
        public Avalonia.Media.FontWeight Weight =>
            Kind == ExplorerNodeKind.Type ? Avalonia.Media.FontWeight.SemiBold : Avalonia.Media.FontWeight.Normal;
    }
}
