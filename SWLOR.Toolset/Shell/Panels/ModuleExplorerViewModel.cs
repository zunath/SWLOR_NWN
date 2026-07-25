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
    /// Module Contents: one tree over everything in the module - a row per resource type, its groups
    /// beneath it, and resources beneath those.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One tree rather than the category rail plus flat item list this panel used to be. A builder
    /// thinks "the Veles area", not "the Areas category, then find veles_exterior among 443 in resref
    /// order", and a tree is what the Aurora toolset trained them on.
    /// </para>
    /// <para>
    /// Rows are published as one flat, virtualized list rather than a real TreeView, and a branch builds
    /// its children the first time it is expanded. Both for the same reason: 8,355 placeables and 7,651
    /// items would otherwise realise a container each, at startup, for types nobody opened.
    /// </para>
    /// </remarks>
    public partial class ModuleExplorerViewModel : Tool
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly PropertiesViewModel _properties;
        private readonly Func<Editors.EditorService>? _editorService;
        private readonly ModelPreviewViewModel? _modelPreview;
        private readonly TilesetCatalog? _tilesetCatalog;
        private readonly CategoryService? _categories;

        private readonly List<ExplorerNodeViewModel> _roots = new();
        private Dictionary<ResourceType, List<CatalogEntry>>? _catalogByType;

        /// <summary>The visible rows: every node whose ancestors are all expanded.</summary>
        public ObservableCollection<ExplorerNodeViewModel> Rows { get; } = new();

        public IReadOnlyList<GroupingChoiceViewModel> GroupingChoices { get; } = new[]
        {
            new GroupingChoiceViewModel(CategoryGrouping.Automatic, "Planet"),
            new GroupingChoiceViewModel(CategoryGrouping.Folders, "My folders"),
            new GroupingChoiceViewModel(CategoryGrouping.Flat, "Flat A-Z")
        };

        [ObservableProperty]
        private ExplorerNodeViewModel? _selectedRow;

        [ObservableProperty]
        private CategoryGrouping _grouping = CategoryGrouping.Automatic;

        /// <summary>What the Group by control is bound to; mirrors <see cref="Grouping"/>.</summary>
        [ObservableProperty]
        private GroupingChoiceViewModel? _selectedGroupingChoice;

        [ObservableProperty]
        private string _filter = string.Empty;

        [ObservableProperty]
        private bool _isOrganizing;

        [ObservableProperty]
        private string _newFolderName = string.Empty;

        [ObservableProperty]
        private string? _statusMessage;

        /// <summary>The new-area wizard while it is open, or null - the view shows it as an overlay.</summary>
        [ObservableProperty]
        private NewAreaViewModel? _activeNewArea;

        public ModuleExplorerViewModel(
            WorkspaceContext workspaceContext,
            PropertiesViewModel properties,
            Func<Editors.EditorService>? editorService = null,
            ModelPreviewViewModel? modelPreview = null,
            TilesetCatalog? tilesetCatalog = null,
            CategoryService? categories = null)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));
            _editorService = editorService;
            _modelPreview = modelPreview;
            _tilesetCatalog = tilesetCatalog;
            _categories = categories;

            Id = "ModuleExplorer";
            Title = "Module Contents";
            SelectedGroupingChoice = GroupingChoices[0];

            _workspaceContext.CatalogEntryRefreshed += (_, _) =>
            {
                if (_workspaceContext.Catalog is { } catalog)
                    RefreshFromCatalog(catalog);
            };
        }

        /// <summary>Builds the type rows. Cheap - nothing beneath them is loaded until expanded.</summary>
        public void Initialize()
        {
            _catalogByType = null;
            _roots.Clear();
            Rows.Clear();

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            AddRoot(ResourceType.Area, workspace.EnumerateAreaResRefs().Count);
            foreach (var type in ModuleWorkspace.BlueprintTypes)
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

        partial void OnSelectedGroupingChoiceChanged(GroupingChoiceViewModel? value)
        {
            if (value != null)
                Grouping = value.Value;
        }

        partial void OnGroupingChanged(CategoryGrouping value)
        {
            var choice = GroupingChoices.FirstOrDefault(candidate => candidate.Value == value);
            if (choice != null && !ReferenceEquals(choice, SelectedGroupingChoice))
                SelectedGroupingChoice = choice;

            if (SelectedRow?.Type is { } type && _categories?.Section(type) is { } section)
            {
                section.Grouping = value;
                _categories.SaveChanges();
            }

            RebuildLoadedBranches();
        }

        partial void OnFilterChanged(string value) => RebuildLoadedBranches();

        partial void OnSelectedRowChanged(ExplorerNodeViewModel? value)
        {
            if (value?.Item == null)
                return;

            var item = value.Item;
            _properties.ShowEntry(new CatalogEntry(value.Type, item.ResRef, item.Name, item.Tag, string.Empty));
            _modelPreview?.ShowFor(value.Type, item.ResRef);
        }

        // ----- folder editing, shown only in the Organize state -----

        [RelayCommand]
        private void NewFolder()
        {
            if (SelectedRow?.Type is not { } type || _categories?.Section(type) is not { } section)
            {
                StatusMessage = "Select something first, so the folder knows where it belongs.";
                return;
            }

            var name = string.IsNullOrWhiteSpace(NewFolderName) ? "New folder" : NewFolderName.Trim();
            section.AddFolder(name);
            section.Grouping = CategoryGrouping.Folders;
            NewFolderName = string.Empty;
            _categories.SaveChanges();

            Grouping = CategoryGrouping.Folders;
            RebuildLoadedBranches();
            StatusMessage = $"Added folder '{name}'.";
        }

        /// <summary>Moves the selected resource into the named folder - filing is a move, not a copy.</summary>
        [RelayCommand]
        private void FileSelected()
        {
            if (SelectedRow is not { IsResource: true } row ||
                _categories?.Section(row.Type) is not { } section)
            {
                StatusMessage = "Select a resource to file.";
                return;
            }

            var folder = section.AllFolders().FirstOrDefault(candidate =>
                string.Equals(candidate.Name, NewFolderName.Trim(), StringComparison.OrdinalIgnoreCase));

            if (folder == null)
            {
                StatusMessage = "Type the name of an existing folder to file it into.";
                return;
            }

            foreach (var previous in section.FoldersContaining(row.ResRef).ToList())
                previous.RemoveMember(row.ResRef);

            folder.AddMember(row.ResRef);
            _categories.SaveChanges();
            RebuildLoadedBranches();
            StatusMessage = $"Filed {row.Name} into '{folder.Name}'.";
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

            switch (Grouping)
            {
                case CategoryGrouping.Flat:
                    foreach (var item in items.OrderBy(SortKey, StringComparer.CurrentCultureIgnoreCase))
                        node.Children.Add(ResourceNode(node.Type, item, item.PrimaryText, 1));
                    break;

                case CategoryGrouping.Folders:
                    BuildFolderGroups(node, items);
                    break;

                default:
                    BuildAutomaticGroups(node, items);
                    break;
            }
        }

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

        /// <summary>Groups by the user's own folders from the sidecar.</summary>
        private void BuildFolderGroups(ExplorerNodeViewModel parent, IReadOnlyList<ExplorerItem> items)
        {
            var section = _categories?.Section(parent.Type);
            if (section == null)
            {
                BuildAutomaticGroups(parent, items);
                return;
            }

            var byResRef = new Dictionary<string, ExplorerItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
                byResRef[item.ResRef] = item;

            var filed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in section.Folders)
            {
                var members = folder.MembersIncludingDescendants
                    .Where(byResRef.ContainsKey)
                    .Select(resRef => byResRef[resRef])
                    .OrderBy(SortKey, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                var node = new ExplorerNodeViewModel(ExplorerNodeKind.Group, parent.Type, folder.Name, 1)
                {
                    Count = members.Count,
                    IsLoaded = true
                };

                foreach (var item in members)
                {
                    filed.Add(item.ResRef);
                    node.Children.Add(ResourceNode(parent.Type, item, item.PrimaryText, 2));
                }

                parent.Children.Add(node);
            }

            var unsorted = items
                .Where(item => !filed.Contains(item.ResRef))
                .OrderBy(SortKey, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            if (unsorted.Count == 0)
                return;

            var unsortedNode = new ExplorerNodeViewModel(
                ExplorerNodeKind.Group, parent.Type, CategorySection.UnsortedFolderName, 1)
            {
                Count = unsorted.Count,
                IsLoaded = true
            };

            foreach (var item in unsorted)
                unsortedNode.Children.Add(ResourceNode(parent.Type, item, item.PrimaryText, 2));

            parent.Children.Add(unsortedNode);
        }

        private IReadOnlyList<ExplorerItem> LoadItems(ResourceType type)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<ExplorerItem>();

            if (_catalogByType != null && _catalogByType.TryGetValue(type, out var entries))
                return entries.Select(entry => new ExplorerItem(entry.ResRef, entry.Name, entry.Tag)).ToList();

            var resRefs = type == ResourceType.Area
                ? workspace.EnumerateAreaResRefs()
                : workspace.EnumerateResRefs(type);

            return resRefs.Select(resRef => new ExplorerItem(resRef, null, null)).ToList();
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
