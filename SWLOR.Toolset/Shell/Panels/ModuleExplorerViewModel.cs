using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
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

        private Dictionary<ResourceType, List<CatalogEntry>>? _catalogByType;

        public ObservableCollection<CategoryNode> Categories { get; } = new();
        public ObservableCollection<ExplorerItem> Items { get; } = new();

        [ObservableProperty]
        private CategoryNode? _selectedCategory;

        [ObservableProperty]
        private ExplorerItem? _selectedItem;

        public ModuleExplorerViewModel(WorkspaceContext workspaceContext, PropertiesViewModel properties)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));
            Id = "ModuleExplorer";
            Title = "Module Explorer";
        }

        /// <summary>Populates the category list from the workspace's (unparsed) resref enumeration. Cheap - safe to call as soon as a workspace is open.</summary>
        public void Initialize()
        {
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
