using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
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

        private Dictionary<ResourceType, List<CatalogEntry>>? _catalogByType;

        public ObservableCollection<CategoryNode> Categories { get; } = new();
        public ObservableCollection<ExplorerItem> Items { get; } = new();

        [ObservableProperty]
        private CategoryNode? _selectedCategory;

        [ObservableProperty]
        private ExplorerItem? _selectedItem;

        /// <summary>The new-area wizard while it is open, or null - the view shows it as an overlay.</summary>
        [ObservableProperty]
        private NewAreaViewModel? _activeNewArea;

        public ModuleExplorerViewModel(
            WorkspaceContext workspaceContext,
            PropertiesViewModel properties,
            Func<Editors.EditorService>? editorService = null,
            ModelPreviewViewModel? modelPreview = null,
            TilesetCatalog? tilesetCatalog = null)
        {
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

        /// <summary>Opens the selected item in its blueprint editor (double-click).</summary>
        public void OpenSelectedItem()
        {
            if (SelectedCategory == null || SelectedItem == null)
                return;

            _editorService?.Invoke().TryOpenEditor(SelectedCategory.Type, SelectedItem.ResRef);
        }

        /// <summary>Populates the category list from the workspace's (unparsed) resref enumeration. Cheap - safe to call as soon as a workspace is open.</summary>
        public void Initialize()
        {
            _catalogByType = null;
            Categories.Clear();
            Items.Clear();

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            Categories.Add(new CategoryNode(ResourceType.Area, "Areas", workspace.EnumerateAreaResRefs().Count));
            foreach (var type in ModuleWorkspace.BlueprintTypes)
                Categories.Add(new CategoryNode(type, type.ToString(), workspace.EnumerateResRefs(type).Count));
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
            if (value != null)
                PopulateItems(value);
            else
                Items.Clear();
        }

        partial void OnSelectedItemChanged(ExplorerItem? value)
        {
            if (value == null || SelectedCategory == null)
                return;

            _properties.ShowEntry(new CatalogEntry(SelectedCategory.Type, value.ResRef, value.Name, value.Tag, string.Empty));
            _modelPreview?.ShowFor(SelectedCategory.Type, value.ResRef);
        }

        private void PopulateItems(CategoryNode category)
        {
            Items.Clear();

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            if (_catalogByType != null && _catalogByType.TryGetValue(category.Type, out var entries))
            {
                foreach (var entry in entries)
                    Items.Add(new ExplorerItem(entry.ResRef, entry.Name, entry.Tag));

                return;
            }

            var resRefs = category.Type == ResourceType.Area
                ? workspace.EnumerateAreaResRefs()
                : workspace.EnumerateResRefs(category.Type);

            foreach (var resRef in resRefs)
                Items.Add(new ExplorerItem(resRef, null, null));
        }
    }
}
