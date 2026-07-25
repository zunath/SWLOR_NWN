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
    /// The Module Explorer panel: a category list (Areas, plus one node per blueprint type with
    /// its count) and, for whichever category is selected, a virtualized list of its items.
    /// Deliberately two-level rather than a single deep tree - expanding a node with (for example)
    /// 8341 utp entries into individual TreeViewItems would not be virtualized in Avalonia's
    /// default TreeView, so a category ListBox + item ListBox (both virtualized via the default
    /// ListBox ItemsPanel) keeps the UI responsive over the full corpus.
    /// </summary>
    public partial class ModuleExplorerViewModel : Tool
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly PropertiesViewModel _properties;
        private readonly Func<Editors.EditorService>? _editorService;
        private readonly ModelPreviewViewModel? _modelPreview;
        private readonly TilesetCatalog? _tilesetCatalog;
        private readonly CategoryService? _categories;

        /// <summary>Every row for the selected category, expanded or not, so collapsing needn't regroup.</summary>
        private readonly List<ExplorerRowViewModel> _allRows = new();

        private Dictionary<ResourceType, List<CatalogEntry>>? _catalogByType;

        public ObservableCollection<CategoryNode> Categories { get; } = new();

        /// <summary>The visible rows - group headers plus the items under expanded groups.</summary>
        public ObservableCollection<ExplorerRowViewModel> Rows { get; } = new();

        /// <summary>How the selected category's items are grouped. Persisted per type in the sidecar.</summary>
        public IReadOnlyList<CategoryGrouping> GroupingChoices { get; } =
            new[] { CategoryGrouping.Automatic, CategoryGrouping.Folders, CategoryGrouping.Flat };

        [ObservableProperty]
        private CategoryNode? _selectedCategory;

        [ObservableProperty]
        private ExplorerRowViewModel? _selectedRow;

        [ObservableProperty]
        private CategoryGrouping _grouping = CategoryGrouping.Automatic;

        [ObservableProperty]
        private string _filter = string.Empty;

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
            _categories = categories;
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));
            _editorService = editorService;
            _modelPreview = modelPreview;
            _tilesetCatalog = tilesetCatalog;
            Id = "ModuleExplorer";
            Title = "Module Explorer";
            _workspaceContext.CatalogEntryRefreshed += (_, _) =>
            {
                if (_workspaceContext.Catalog is { } catalog)
                    RefreshFromCatalog(catalog);
            };
        }

        /// <summary>
        /// Opens the new-area wizard. On success the explorer re-enumerates (so the new area
        /// shows up in the Areas category) and the area opens in its editor, ready to paint.
        /// </summary>
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

                    SelectedCategory = Categories.FirstOrDefault(c => c.Type == ResourceType.Area);
                    _editorService?.Invoke().TryOpenEditor(ResourceType.Area, resRef);
                },
                () => ActiveNewArea = null);
        }

        /// <summary>Opens the selected item in its editor (double-click). Group rows toggle instead.</summary>
        public void OpenSelectedItem()
        {
            if (SelectedCategory == null || SelectedRow == null)
                return;

            if (SelectedRow.IsGroup)
            {
                ToggleGroup(SelectedRow);
                return;
            }

            _editorService?.Invoke().TryOpenEditor(SelectedCategory.Type, SelectedRow.ResRef);
        }

        /// <summary>Expands or collapses a group header, re-publishing the flat list.</summary>
        [RelayCommand]
        private void ToggleGroup(ExplorerRowViewModel? row)
        {
            if (row is not { IsGroup: true })
                return;

            row.IsExpanded = !row.IsExpanded;
            PublishVisibleRows();
        }

        /// <summary>Populates the category list from the workspace's (unparsed) resref enumeration. Cheap - safe to call as soon as a workspace is open.</summary>
        public void Initialize()
        {
            _catalogByType = null;
            Categories.Clear();
            _allRows.Clear();
            Rows.Clear();

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            Categories.Add(new CategoryNode(
                ResourceType.Area, ResourceType.Area.DisplayName(), workspace.EnumerateAreaResRefs().Count));
            foreach (var type in ModuleWorkspace.BlueprintTypes)
                Categories.Add(new CategoryNode(type, type.DisplayName(), workspace.EnumerateResRefs(type).Count));
        }

        /// <summary>Called once the background <see cref="BlueprintCatalog"/> build completes, to enrich item display with parsed Name/Tag. Must be called on the UI thread.</summary>
        public void RefreshFromCatalog(BlueprintCatalog catalog)
        {
            _catalogByType = catalog.Entries
                .GroupBy(entry => entry.ResourceType)
                .ToDictionary(group => group.Key, group => group.ToList());

            for (var i = 0; i < Categories.Count; i++)
            {
                var category = Categories[i];
                if (_catalogByType.TryGetValue(category.Type, out var entries))
                    Categories[i] = category with { Count = entries.Count };
            }

            if (SelectedCategory != null)
                PopulateItems(SelectedCategory);
        }

        partial void OnSelectedCategoryChanged(CategoryNode? value)
        {
            if (value == null)
            {
                _allRows.Clear();
                Rows.Clear();
                return;
            }

            // Each type remembers its own grouping, so switching between Areas and Placeables does not
            // drag one's arrangement onto the other.
            var stored = _categories?.Section(value.Type)?.Grouping;
            if (stored != null && stored != Grouping)
            {
                _grouping = stored.Value;
                OnPropertyChanged(nameof(Grouping));
            }

            PopulateItems(value);
        }

        partial void OnGroupingChanged(CategoryGrouping value)
        {
            if (SelectedCategory is { } category && _categories?.Section(category.Type) is { } section)
            {
                section.Grouping = value;
                _categories.SaveChanges();
            }

            if (SelectedCategory != null)
                PopulateItems(SelectedCategory);
        }

        partial void OnFilterChanged(string value)
        {
            if (SelectedCategory != null)
                PopulateItems(SelectedCategory);
        }

        partial void OnSelectedRowChanged(ExplorerRowViewModel? value)
        {
            if (value?.Item == null || SelectedCategory == null)
                return;

            var item = value.Item;
            _properties.ShowEntry(new CatalogEntry(SelectedCategory.Type, item.ResRef, item.Name, item.Tag, string.Empty));
            _modelPreview?.ShowFor(SelectedCategory.Type, item.ResRef);
        }

        private void PopulateItems(CategoryNode category)
        {
            _allRows.Clear();

            var items = LoadItems(category);
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
                        _allRows.Add(ExplorerRowViewModel.Resource(item, item.PrimaryText));
                    break;

                case CategoryGrouping.Folders:
                    BuildFolderRows(category, items);
                    break;

                default:
                    BuildAutomaticRows(items);
                    break;
            }

            PublishVisibleRows();
        }

        private IReadOnlyList<ExplorerItem> LoadItems(CategoryNode category)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<ExplorerItem>();

            if (_catalogByType != null && _catalogByType.TryGetValue(category.Type, out var entries))
                return entries.Select(entry => new ExplorerItem(entry.ResRef, entry.Name, entry.Tag)).ToList();

            var resRefs = category.Type == ResourceType.Area
                ? workspace.EnumerateAreaResRefs()
                : workspace.EnumerateResRefs(category.Type);

            return resRefs.Select(resRef => new ExplorerItem(resRef, null, null)).ToList();
        }

        /// <summary>
        /// Groups on the part of the name before its first dash - see <see cref="AutomaticGrouping"/>.
        /// Anything without a separator lands in Unsorted, which sorts last and is always shown.
        /// </summary>
        private void BuildAutomaticRows(IReadOnlyList<ExplorerItem> items)
        {
            var groups = items
                .GroupBy(item => AutomaticGrouping.GroupNameFor(item.Name) ?? CategorySection.UnsortedFolderName)
                .OrderBy(group => group.Key == CategorySection.UnsortedFolderName ? 1 : 0)
                .ThenBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase);

            foreach (var group in groups)
            {
                _allRows.Add(ExplorerRowViewModel.Group(group.Key, group.Count()));
                foreach (var item in group.OrderBy(SortKey, StringComparer.CurrentCultureIgnoreCase))
                {
                    var label = AutomaticGrouping.LeafLabelFor(item.Name);
                    _allRows.Add(ExplorerRowViewModel.Resource(
                        item, label.Length > 0 ? label : item.PrimaryText));
                }
            }
        }

        /// <summary>Groups by the user's own folders from the sidecar, top level only in this panel.</summary>
        private void BuildFolderRows(CategoryNode category, IReadOnlyList<ExplorerItem> items)
        {
            var section = _categories?.Section(category.Type);
            if (section == null)
            {
                BuildAutomaticRows(items);
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

                if (members.Count == 0)
                    continue;

                _allRows.Add(ExplorerRowViewModel.Group(folder.Name, members.Count));
                foreach (var item in members)
                {
                    filed.Add(item.ResRef);
                    _allRows.Add(ExplorerRowViewModel.Resource(item, item.PrimaryText));
                }
            }

            var unsorted = items
                .Where(item => !filed.Contains(item.ResRef))
                .OrderBy(SortKey, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            if (unsorted.Count == 0)
                return;

            _allRows.Add(ExplorerRowViewModel.Group(CategorySection.UnsortedFolderName, unsorted.Count));
            foreach (var item in unsorted)
                _allRows.Add(ExplorerRowViewModel.Resource(item, item.PrimaryText));
        }

        private static string SortKey(ExplorerItem item) => item.PrimaryText;

        /// <summary>Publishes group headers plus the items under expanded groups only.</summary>
        private void PublishVisibleRows()
        {
            Rows.Clear();
            var visible = true;

            foreach (var row in _allRows)
            {
                if (row.IsGroup)
                {
                    visible = row.IsExpanded;
                    Rows.Add(row);
                    continue;
                }

                if (visible)
                    Rows.Add(row);
            }
        }
    }
}
