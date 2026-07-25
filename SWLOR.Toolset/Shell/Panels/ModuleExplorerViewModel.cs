using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// Module Contents: the module's areas, conversations and scripts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately NOT the blueprints. Creatures, placeables, items and the rest are what the Palette
    /// panel is for, and listing all 17,000 of them twice in the same window only makes the builder
    /// decide which of two trees to use. What is left is the three things the Aurora toolset kept
    /// separate from its palette for the same reason: areas, conversations, scripts.
    /// </para>
    /// <para>
    /// Rows are published as one flat, virtualized list rather than a real TreeView, and a branch builds
    /// its children the first time it is expanded - 609 conversations would otherwise realise a
    /// container each at startup for a section nobody opened.
    /// </para>
    /// </remarks>
    public partial class ModuleExplorerViewModel : Tool
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly PropertiesViewModel _properties;
        private readonly Func<Editors.EditorService>? _editorService;
        private readonly TilesetCatalog? _tilesetCatalog;

        private readonly List<ExplorerNodeViewModel> _roots = new();
        private Dictionary<ResourceType, List<CatalogEntry>>? _catalogByType;

        /// <summary>The visible rows: every node whose ancestors are all expanded.</summary>
        public ObservableCollection<ExplorerNodeViewModel> Rows { get; } = new();

        [ObservableProperty]
        private ExplorerNodeViewModel? _selectedRow;

        [ObservableProperty]
        private string _filter = string.Empty;

        [ObservableProperty]
        private string? _statusMessage;

        /// <summary>The new-area wizard while it is open, or null - the view shows it as an overlay.</summary>
        [ObservableProperty]
        private NewAreaViewModel? _activeNewArea;

        public ModuleExplorerViewModel(
            WorkspaceContext workspaceContext,
            PropertiesViewModel properties,
            Func<Editors.EditorService>? editorService = null,
            TilesetCatalog? tilesetCatalog = null)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));
            _editorService = editorService;
            _tilesetCatalog = tilesetCatalog;

            Id = "ModuleExplorer";
            Title = "Module Contents";

            _workspaceContext.CatalogEntryRefreshed += (_, _) =>
            {
                if (_workspaceContext.Catalog is { } catalog)
                    RefreshFromCatalog(catalog);
            };
        }

        /// <summary>
        /// The sections, in the order Aurora listed them. Each one is a resource kind that lives in the
        /// module and is not a blueprint.
        /// </summary>
        private static readonly ResourceType[] Sections =
        {
            ResourceType.Area,
            ResourceType.Dlg,
            ResourceType.Nss
        };

        /// <summary>Builds the section rows. Cheap - nothing beneath them is loaded until expanded.</summary>
        public void Initialize()
        {
            _catalogByType = null;
            _roots.Clear();
            Rows.Clear();

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            foreach (var type in Sections)
                AddRoot(type, workspace.EnumerateResRefs(type).Count);

            PublishVisibleRows();
        }

        private void AddRoot(ResourceType type, int count) =>
            _roots.Add(new ExplorerNodeViewModel(ExplorerNodeKind.Type, type, type.DisplayName(), 0)
            {
                Count = count
            });

        /// <summary>Called once the background catalog publishes names, so rows can lead with them.</summary>
        public void RefreshFromCatalog(BlueprintCatalog catalog)
        {
            _catalogByType = catalog.Entries
                .GroupBy(entry => entry.ResourceType)
                .ToDictionary(group => group.Key, group => group.ToList());

            // Anything already open is rebuilt in place, so names replace resrefs without the tree
            // collapsing under the user mid-browse.
            foreach (var root in _roots)
            {
                if (_catalogByType.TryGetValue(root.Type, out var entries))
                    root.Count = entries.Count;

                Reload(root);
            }

            PublishVisibleRows();
        }

        [RelayCommand]
        private void NewArea()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            ActiveNewArea = new NewAreaViewModel(
                workspace,
                _tilesetCatalog,
                resRef =>
                {
                    ActiveNewArea = null;
                    Initialize();
                    _workspaceContext.RefreshCatalogEntry(ResourceType.Area, resRef);
                    _editorService?.Invoke().TryOpenEditor(ResourceType.Area, resRef);
                },
                () => ActiveNewArea = null);
        }

        /// <summary>Double-click: open a resource, or expand a branch.</summary>
        public void OpenSelectedItem()
        {
            if (SelectedRow is not { } row)
                return;

            if (row.IsBranch)
            {
                Toggle(row);
                return;
            }

            _editorService?.Invoke().TryOpenEditor(row.Type, row.ResRef);
        }

        [RelayCommand]
        private void Toggle(ExplorerNodeViewModel? row)
        {
            if (row is not { IsBranch: true })
                return;

            row.IsExpanded = !row.IsExpanded;
            if (row.IsExpanded)
                EnsureLoaded(row);

            PublishVisibleRows();
        }

        partial void OnFilterChanged(string value) => RebuildLoadedBranches();

        partial void OnSelectedRowChanged(ExplorerNodeViewModel? value)
        {
            if (value?.Item == null)
                return;

            var item = value.Item;
            _properties.ShowEntry(new CatalogEntry(value.Type, item.ResRef, item.Name, item.Tag, string.Empty));

            // Nothing in this panel has a model any more - areas, conversations and scripts all have
            // none - so the preview is left showing whatever the Palette last put there rather than
            // being cleared to "no preview available" on every click.
        }

        // ----- tree assembly -----

        private void RebuildLoadedBranches()
        {
            foreach (var root in _roots)
                Reload(root);

            PublishVisibleRows();
        }

        private void Reload(ExplorerNodeViewModel root)
        {
            if (!root.IsLoaded)
                return;

            root.IsLoaded = false;
            root.Children.Clear();
            if (root.IsExpanded)
                EnsureLoaded(root);
        }

        private void EnsureLoaded(ExplorerNodeViewModel node)
        {
            if (node.IsLoaded || node.Kind != ExplorerNodeKind.Type)
                return;

            node.IsLoaded = true;

            var items = LoadItems(node.Type);
            if (!string.IsNullOrWhiteSpace(Filter))
            {
                var needle = Filter.Trim();
                items = items
                    .Where(item =>
                        item.ResRef.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        (item.Name?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            // Grouping is a property of the content, not a setting. Area names carry their own folder
            // structure in the "Planet - Place" convention, so they group by it; conversation and script
            // names carry nothing to group on, and grouping them anyway produced one "Unsorted" folder
            // wrapping the entire list.
            if (GroupsByName(node.Type))
            {
                BuildAutomaticGroups(node, items);
                return;
            }

            foreach (var item in items.OrderBy(SortKey, StringComparer.CurrentCultureIgnoreCase))
                node.Children.Add(ResourceNode(node.Type, item, item.PrimaryText, 1));
        }

        private static bool GroupsByName(ResourceType type) => type == ResourceType.Area;

        private static ExplorerNodeViewModel ResourceNode(
            ResourceType type, ExplorerItem item, string label, int depth) =>
            new(ExplorerNodeKind.Resource, type, label, depth) { Item = item };

        /// <summary>Groups on the part of a name before its first dash - see <see cref="AutomaticGrouping"/>.</summary>
        private static void BuildAutomaticGroups(ExplorerNodeViewModel parent, IReadOnlyList<ExplorerItem> items)
        {
            var groups = items
                .GroupBy(item => AutomaticGrouping.GroupNameFor(item.Name) ?? CategorySection.UnsortedFolderName)
                .OrderBy(group => group.Key == CategorySection.UnsortedFolderName ? 1 : 0)
                .ThenBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase);

            foreach (var group in groups)
            {
                var node = new ExplorerNodeViewModel(ExplorerNodeKind.Group, parent.Type, group.Key, 1)
                {
                    Count = group.Count(),
                    IsLoaded = true
                };

                foreach (var item in group.OrderBy(SortKey, StringComparer.CurrentCultureIgnoreCase))
                {
                    var label = AutomaticGrouping.LeafLabelFor(item.Name);
                    node.Children.Add(ResourceNode(
                        parent.Type, item, label.Length > 0 ? label : item.PrimaryText, 2));
                }

                parent.Children.Add(node);
            }
        }

        private IReadOnlyList<ExplorerItem> LoadItems(ResourceType type)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<ExplorerItem>();

            if (_catalogByType != null && _catalogByType.TryGetValue(type, out var entries))
                return entries.Select(entry => new ExplorerItem(entry.ResRef, entry.Name, entry.Tag)).ToList();

            return workspace.EnumerateResRefs(type)
                .Select(resRef => new ExplorerItem(resRef, null, null))
                .ToList();
        }

        private static string SortKey(ExplorerItem item) => item.PrimaryText;

        private void PublishVisibleRows()
        {
            Rows.Clear();
            foreach (var root in _roots)
                Publish(root);
        }

        private void Publish(ExplorerNodeViewModel node)
        {
            Rows.Add(node);
            if (!node.IsExpanded)
                return;

            foreach (var child in node.Children)
                Publish(child);
        }
    }
}
