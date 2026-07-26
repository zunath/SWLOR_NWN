using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// Module Contents: the module's areas, dialogs and scripts, one tab each.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately NOT the blueprints. Creatures, placeables, items and the rest are what the Palette
    /// panel is for, and listing all 17,000 of them twice in the same window only makes the builder
    /// decide which of two trees to use. What is left is the three things the Aurora toolset kept
    /// separate from its palette for the same reason: areas, dialogs, scripts.
    /// </para>
    /// <para>
    /// Tabs rather than three expandable roots. Only one of the three is ever being worked in, and as
    /// roots they cost a row of screen height each plus a level of indentation on every row beneath -
    /// with the tab bar, folders start at the left edge and the New button can name what it makes.
    /// </para>
    /// <para>
    /// Folders come from the category sidecar, the same store the Palette's categories live in, so a
    /// builder's arrangement survives a restart without anything being written into the module. Areas
    /// are seeded from the "Planet - Place" naming rule the first time they are shown, which turns what
    /// used to be a fixed automatic grouping into a starting point that can then be edited.
    /// </para>
    /// <para>
    /// Rows are published as one flat, virtualized list rather than a real TreeView, and a folder builds
    /// its children the first time it is expanded - 609 dialogs would otherwise realise a
    /// container each for a folder nobody opened.
    /// </para>
    /// </remarks>
    public partial class ModuleExplorerViewModel : Tool
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly PropertiesViewModel _properties;
        private readonly CategoryService _categories;
        private readonly OutputLogService _log;
        private readonly Func<Editors.EditorService>? _editorService;
        private readonly TilesetCatalog? _tilesetCatalog;
        private readonly Services.IEditorPromptService? _prompts;
        private readonly Settings.ToolsetSettings? _settings;

        private readonly List<ExplorerNodeViewModel> _roots = new();
        private Dictionary<ResourceType, List<CatalogEntry>>? _catalogByType;

        /// <summary>Types whose sidecar section has already been seeded this session.</summary>
        private readonly HashSet<ResourceType> _seeded = new();

        /// <summary>The visible rows: every node whose ancestors are all expanded.</summary>
        public ObservableCollection<ExplorerNodeViewModel> Rows { get; } = new();

        /// <summary>The three tabs, in Aurora's order.</summary>
        public ObservableCollection<ExplorerTabViewModel> Tabs { get; } = new();

        [ObservableProperty]
        private ExplorerNodeViewModel? _selectedRow;

        [ObservableProperty]
        private string _filter = string.Empty;

        [ObservableProperty]
        private string? _statusMessage;

        /// <summary>The new-area wizard while it is open, or null - the view shows it as an overlay.</summary>
        [ObservableProperty]
        private NewAreaViewModel? _activeNewArea;

        /// <summary>Which tab is showing. Everything else in the panel is scoped to it.</summary>
        [ObservableProperty]
        private ResourceType _selectedType = ResourceType.Area;

        public ModuleExplorerViewModel(
            WorkspaceContext workspaceContext,
            PropertiesViewModel properties,
            CategoryService categories,
            OutputLogService log,
            Func<Editors.EditorService>? editorService = null,
            TilesetCatalog? tilesetCatalog = null,
            Services.IEditorPromptService? prompts = null,
            Settings.ToolsetSettings? settings = null)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));
            _categories = categories ?? throw new ArgumentNullException(nameof(categories));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _editorService = editorService;
            _tilesetCatalog = tilesetCatalog;
            _prompts = prompts;

            Id = "ModuleExplorer";
            Title = "Module Contents";

            _settings = settings;

            // Assigned rather than set through the property: the tab change handler rebuilds a tree that
            // does not exist yet, and the module is not open at construction time anyway.
            if (settings != null &&
                ResourceTypeExtensions.TryFromExtension(settings.ModuleContentsTab, out var tab) &&
                Sections.Contains(tab))
                _selectedType = tab;

            PublishTabs();

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
        public static readonly IReadOnlyList<ResourceType> Sections = new[]
        {
            ResourceType.Area,
            ResourceType.Dlg,
            ResourceType.Nss
        };

        /// <summary>What the New button says, which follows the tab - "New Area...", "New Script...".</summary>
        public string NewItemLabel => $"New {SelectedType.SingularDisplayName()}...";

        /// <summary>
        /// Dialog creation stays unavailable until dialogs have an editor. Producing a blank DLG that
        /// this toolset cannot populate or even open leaves the builder with an unusable resource.
        /// </summary>
        public bool CanCreateSelectedType => SelectedType != ResourceType.Dlg;

        /// <summary>Builds the tree for the selected tab.</summary>
        public void Initialize()
        {
            _catalogByType = null;
            Refresh();
        }

        /// <summary>Called once the background catalog publishes names, so rows can lead with them.</summary>
        public void RefreshFromCatalog(BlueprintCatalog catalog)
        {
            _catalogByType = catalog.Entries
                .GroupBy(entry => entry.ResourceType)
                .ToDictionary(group => group.Key, group => group.ToList());

            Refresh();
        }

        /// <summary>
        /// Rebuilds the tree, keeping which folders were open. Expansion is restored by folder name
        /// rather than by node, because every node is new after a rebuild.
        /// </summary>
        public void Refresh()
        {
            var expanded = _roots
                .SelectMany(Flatten)
                .Where(node => node.IsExpanded)
                .Select(node => node.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var selectedResRef = SelectedRow?.Item?.ResRef;

            _roots.Clear();
            Rows.Clear();

            foreach (var tab in Tabs)
                tab.Count = CountFor(tab.Type);

            var section = _categories.Section(SelectedType);
            if (section == null)
            {
                PublishVisibleRows();
                return;
            }

            var items = LoadItems(SelectedType);
            SeedIfNeeded(section, items);

            var byResRef = Filtered(items)
                .GroupBy(item => item.ResRef, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var folder in Ordered(section.Folders))
                _roots.Add(BuildFolderNode(folder, byResRef, depth: 0));

            var unsorted = section
                .UnsortedResRefs(byResRef.Keys)
                .Select(resRef => byResRef[resRef])
                .ToList();

            if (unsorted.Count > 0)
                _roots.Add(BuildUnsortedNode(unsorted));

            foreach (var node in _roots.SelectMany(Flatten))
                node.IsExpanded = expanded.Contains(node.Name);

            PublishMoveTargets(section);
            PublishVisibleRows();

            if (selectedResRef != null)
                SelectedRow = Rows.FirstOrDefault(row =>
                    string.Equals(row.Item?.ResRef, selectedResRef, StringComparison.OrdinalIgnoreCase));
        }

        // ----- tabs -----

        private void PublishTabs()
        {
            Tabs.Clear();
            foreach (var type in Sections)
                Tabs.Add(new ExplorerTabViewModel(type) { IsSelected = type == SelectedType });
        }

        [RelayCommand]
        private void SelectTab(ExplorerTabViewModel? tab)
        {
            if (tab == null || tab.Type == SelectedType)
                return;

            SelectedType = tab.Type;
        }

        partial void OnSelectedTypeChanged(ResourceType value)
        {
            if (_settings != null)
                _settings.ModuleContentsTab = value.Extension();

            foreach (var tab in Tabs)
                tab.IsSelected = tab.Type == value;

            OnPropertyChanged(nameof(NewItemLabel));
            OnPropertyChanged(nameof(CanOpenSelectedType));
            OnPropertyChanged(nameof(CanCreateSelectedType));
            SelectedRow = null;
            StatusMessage = null;
            Refresh();
        }

        private int CountFor(ResourceType type) =>
            IsCatalogIndexed(type) && _catalogByType != null && _catalogByType.TryGetValue(type, out var entries)
                ? entries.Count
                : _workspaceContext.Workspace?.EnumerateResRefs(type).Count ?? 0;

        // ----- creating -----

        /// <summary>Creates a resource of the selected type: the area wizard, or a prompt plus a template.</summary>
        [RelayCommand]
        private async Task NewItemAsync()
        {
            if (!CanCreateSelectedType)
            {
                StatusMessage = "Dialog creation will be available with the dialog editor.";
                return;
            }

            if (SelectedType == ResourceType.Area)
            {
                NewArea();
                return;
            }

            var workspace = _workspaceContext.Workspace;
            if (workspace == null || _prompts == null)
                return;

            var name = await _prompts.PromptForTextAsync(
                NewItemLabel.TrimEnd('.'),
                $"Name for the new {SelectedType.SingularDisplayName().ToLowerInvariant()}. Its resref is derived from this.",
                string.Empty,
                "Create");

            if (string.IsNullOrWhiteSpace(name))
                return;

            var resRef = ToResRef(name);
            if (resRef.Length == 0)
            {
                StatusMessage = "That name has no letters or digits to make a resref from.";
                return;
            }

            var path = workspace.GetResourcePath(SelectedType, resRef);
            if (File.Exists(path))
            {
                StatusMessage = $"'{resRef}' already exists.";
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(
                    path, ModuleResourceTemplateFactory.CreateFileContent(SelectedType, resRef, name.Trim()));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not create '{resRef}': {ex.Message}";
                return;
            }

            // Filed straight into the folder that was selected, so creating inside a folder puts it
            // there rather than dropping it in Unsorted for the builder to move.
            if (SelectedRow?.Folder is { } folder)
            {
                folder.AddMember(resRef);
                SaveCategories();
            }

            _log.AppendLine($"Created {SelectedType.SingularDisplayName().ToLowerInvariant()} '{resRef}'.");
            _workspaceContext.RefreshCatalogEntry(SelectedType, resRef);
            Refresh();

            // Said plainly rather than left to be discovered: the toolset writes .nss source and does
            // not compile it, and NWN runs the compiled .ncs. A new script is a real file in the module
            // but it does nothing until the build pipeline compiles it.
            StatusMessage = SelectedType == ResourceType.Nss
                ? $"Created '{resRef}'. It must be compiled to .ncs by the build before the game will run it."
                : $"Created '{resRef}'.";

            if (CanOpenSelectedType)
                _editorService?.Invoke().TryOpenEditor(SelectedType, resRef);
        }

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

                    if (SelectedRow?.Folder is { } folder)
                    {
                        folder.AddMember(resRef);
                        SaveCategories();
                    }

                    _workspaceContext.RefreshCatalogEntry(ResourceType.Area, resRef);
                    Refresh();
                    _editorService?.Invoke().TryOpenEditor(ResourceType.Area, resRef);
                },
                () => ActiveNewArea = null);
        }

        /// <summary>
        /// A resref from a display name: lowercase, alphanumeric and underscore, 16 characters. Same
        /// rule the Palette's New-blueprint action uses, so the two never disagree about a name.
        /// </summary>
        private static string ToResRef(string name)
        {
            // ASCII only: a resref is a NWN resource identifier, and char.IsLetterOrDigit would happily
            // keep the accented letter in "Café" and write a filename the game cannot address.
            var characters = name.Trim().ToLowerInvariant()
                .Select(character => char.IsAsciiLetterOrDigit(character) ? character : '_')
                .ToArray();

            return new string(characters).Trim('_').Replace("__", "_") is { Length: > 0 } cleaned
                ? cleaned[..Math.Min(16, cleaned.Length)]
                : string.Empty;
        }

        // ----- folders -----

        /// <summary>True while a real folder is selected, which is what rename and delete need.</summary>
        public bool HasFolderSelected => SelectedRow?.Folder != null;

        [RelayCommand]
        private async Task NewFolderAsync()
        {
            var section = _categories.Section(SelectedType);
            if (section == null || _prompts == null)
                return;

            var parent = SelectedRow?.Folder;
            var name = await _prompts.PromptForTextAsync(
                parent == null ? "New folder" : $"New folder in '{parent.Name}'",
                "Folder name",
                string.Empty,
                "Create");

            if (string.IsNullOrWhiteSpace(name))
                return;

            var trimmed = name.Trim();
            var nameAvailable = parent == null
                ? section.IsNameAvailable(trimmed)
                : parent.IsNameAvailable(trimmed);
            if (!nameAvailable)
            {
                StatusMessage = $"A folder named '{trimmed}' already exists here.";
                return;
            }

            if (parent == null)
                section.AddFolder(trimmed);
            else
                parent.AddChild(trimmed);

            SaveCategories();
            Refresh();
        }

        [RelayCommand]
        private async Task RenameFolderAsync()
        {
            if (SelectedRow?.Folder is not { } folder || _prompts == null)
                return;

            var name = await _prompts.PromptForTextAsync(
                $"Rename '{folder.Name}'", "Folder name", folder.Name, "Rename");
            if (string.IsNullOrWhiteSpace(name) || name.Trim() == folder.Name)
                return;

            var section = _categories.Section(SelectedType);
            var trimmed = name.Trim();
            if (section == null || !section.TryRenameFolder(folder, trimmed))
            {
                StatusMessage = $"A folder named '{trimmed}' already exists here.";
                return;
            }

            SaveCategories();
            Refresh();
        }

        /// <summary>
        /// Deletes a folder. Its contents are not: the members go back to Unsorted, because the sidecar
        /// only records an arrangement and deleting an arrangement must never delete a resource.
        /// </summary>
        [RelayCommand]
        private async Task DeleteFolderAsync()
        {
            var section = _categories.Section(SelectedType);
            if (section == null || SelectedRow?.Folder is not { } folder || _prompts == null)
                return;

            // Sub-folders are named separately, because an empty branch of folders has no members at
            // all: without saying so, deleting it looks like a no-op right up until the arrangement is
            // gone, and the sidecar has no undo.
            var count = folder.MembersIncludingDescendants.Count();
            var subFolders = folder.Children.Count;
            if (count > 0 || subFolders > 0)
            {
                var confirmed = await _prompts.ConfirmDestructiveAsync(
                    $"Delete '{folder.Name}'?",
                    DeleteFolderDetail(count, subFolders),
                    "Delete folder");

                if (!confirmed)
                    return;
            }

            section.RemoveFolder(folder);
            SaveCategories();
            SelectedRow = null;
            Refresh();
        }

        private static string DeleteFolderDetail(int members, int subFolders)
        {
            var parts = new List<string>();
            if (subFolders > 0)
                parts.Add($"{subFolders} sub-folder(s) are removed with it");
            if (members > 0)
                parts.Add($"{members} item(s) move back to Unsorted");

            return string.Join(", ", parts) + ". Nothing is deleted from the module.";
        }

        /// <summary>Every folder of the current tab, as "Move to" destinations.</summary>
        public ObservableCollection<FolderTargetViewModel> MoveTargets { get; } = new();

        public bool HasMoveTargets => MoveTargets.Count > 0;

        private void PublishMoveTargets(CategorySection? section)
        {
            MoveTargets.Clear();
            if (section != null)
            {
                foreach (var folder in section.AllFolders())
                    MoveTargets.Add(new FolderTargetViewModel(
                        folder, string.Join(" / ", section.PathTo(folder)), MoveSelectedInto));
            }

            OnPropertyChanged(nameof(HasMoveTargets));
        }

        /// <summary>Files the selected resource into a folder, which is how a builder organises by hand.</summary>
        private void MoveSelectedInto(CategoryFolder target)
        {
            var section = _categories.Section(SelectedType);
            if (section == null || SelectedRow?.Item is not { } item)
                return;

            // Out of wherever it was first: a resref may legally sit in several folders, but a move the
            // builder asked for means one destination, not an extra one.
            foreach (var folder in section.AllFolders())
                folder.RemoveMember(item.ResRef);

            target.AddMember(item.ResRef);
            SaveCategories();
            Refresh();
        }

        /// <summary>Takes the selected resource out of every folder, back to Unsorted.</summary>
        [RelayCommand]
        private void RemoveFromFolder()
        {
            var section = _categories.Section(SelectedType);
            if (section == null || SelectedRow?.Item is not { } item)
                return;

            var removed = false;
            foreach (var folder in section.AllFolders())
                removed |= folder.RemoveMember(item.ResRef);

            if (!removed)
                return;

            SaveCategories();
            Refresh();
        }


        /// <summary>
        /// Writes the category sidecar and reports a refusal in the status line.
        /// </summary>
        /// <remarks>
        /// The sidecar can legitimately decline a write - it is read-only when a newer Toolset wrote it,
        /// and it will not clobber an edit made outside the app. Every command here has already told the
        /// builder what it did, so a silent refusal would leave them believing it.
        /// </remarks>
        private bool SaveCategories()
        {
            var result = _categories.SaveChanges();
            if (!result.Saved)
                StatusMessage = result.Problem;

            return result.Saved;
        }

        // ----- browsing -----

        /// <summary>
        /// Whether the selected tab's resources can actually be opened. Only areas can: EditorService
        /// has schemas for the blueprint types and a special path for areas, so a dialog or script would
        /// only ever log "No editor available yet". An action that cannot succeed should not be offered.
        /// </summary>
        public bool CanOpenSelectedType => SelectedType == ResourceType.Area;

        /// <summary>The context menu's Open, which is the double-click by another route.</summary>
        [RelayCommand]
        private void OpenSelected() => OpenSelectedItem();

        /// <summary>Double-click: open a resource, or expand a folder.</summary>
        public void OpenSelectedItem()
        {
            if (SelectedRow is not { } row)
                return;

            if (row.IsBranch)
            {
                Toggle(row);
                return;
            }

            if (!CanOpenSelectedType)
            {
                StatusMessage = $"{SelectedType.DisplayName()} cannot be edited in the toolset yet.";
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
            PublishVisibleRows();
        }

        partial void OnFilterChanged(string value) => Refresh();

        partial void OnSelectedRowChanged(ExplorerNodeViewModel? value)
        {
            OnPropertyChanged(nameof(HasFolderSelected));

            if (value?.Item == null)
                return;

            var item = value.Item;
            _properties.ShowEntry(new CatalogEntry(value.Type, item.ResRef, item.Name, item.Tag, string.Empty));

            // Nothing in this panel has a model any more - areas, dialogs and scripts all have
            // none - so the preview is left showing whatever the Palette last put there rather than
            // being cleared to "no preview available" on every click.
        }

        // ----- tree assembly -----

        private IReadOnlyList<ExplorerItem> Filtered(IReadOnlyList<ExplorerItem> items)
        {
            if (string.IsNullOrWhiteSpace(Filter))
                return items;

            var needle = Filter.Trim();
            return items
                .Where(item =>
                    item.ResRef.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    (item.Name?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        private ExplorerNodeViewModel BuildFolderNode(
            CategoryFolder folder, IReadOnlyDictionary<string, ExplorerItem> items, int depth)
        {
            var node = new ExplorerNodeViewModel(ExplorerNodeKind.Group, SelectedType, folder.Name, depth)
            {
                Folder = folder,
                IsLoaded = true
            };

            foreach (var child in Ordered(folder.Children))
                node.Children.Add(BuildFolderNode(child, items, depth + 1));

            var members = folder.Members
                .Where(items.ContainsKey)
                .Select(resRef => items[resRef])
                .OrderBy(SortKey, StringComparer.CurrentCultureIgnoreCase);

            foreach (var item in members)
                node.Children.Add(ResourceNode(item, LabelFor(item, insideFolder: true), depth + 1));

            // The count is what this folder and everything under it will actually show, so it never
            // promises rows a filter has already removed.
            node.Count = node.Children.Sum(child => child.IsResource ? 1 : child.Count);
            return node;
        }

        private ExplorerNodeViewModel BuildUnsortedNode(IReadOnlyList<ExplorerItem> items)
        {
            var node = new ExplorerNodeViewModel(
                ExplorerNodeKind.Group, SelectedType, CategorySection.UnsortedFolderName, 0)
            {
                IsLoaded = true,
                Count = items.Count
            };

            foreach (var item in items.OrderBy(SortKey, StringComparer.CurrentCultureIgnoreCase))
                node.Children.Add(ResourceNode(item, LabelFor(item, insideFolder: false), 1));

            return node;
        }

        /// <summary>
        /// What a row reads as. Inside a folder an area drops the part its folders already say -
        /// "North Entrance" under Tatooine/Anchorhead, not the whole "Tatooine - Anchorhead - North
        /// Entrance". Only area names carry that structure, so dialogs and scripts keep their resref.
        /// </summary>
        private string LabelFor(ExplorerItem item, bool insideFolder)
        {
            if (!insideFolder)
                return item.PrimaryText;

            var label = ModuleFolderSeeder.LeafLabel(
                SelectedType, new SeedableResource(item.ResRef, item.Name));

            return label.Length > 0 ? label : item.PrimaryText;
        }

        private ExplorerNodeViewModel ResourceNode(ExplorerItem item, string label, int depth) =>
            new(ExplorerNodeKind.Resource, SelectedType, label, depth) { Item = item };

        /// <summary>Pinned folders first, then alphabetical - the order the Palette's tree uses.</summary>
        private IEnumerable<CategoryFolder> Ordered(IReadOnlyList<CategoryFolder> folders)
        {
            var pinned = _categories.Section(SelectedType)?.Pinned ?? Array.Empty<string>();
            return folders
                .OrderBy(folder => pinned.Contains(folder.Name, StringComparer.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Gives a section its starting folders - see <see cref="ModuleFolderSeeder"/> for the rules and
        /// for why the three sections do not get the same ones.
        /// </summary>
        /// <remarks>
        /// Written into the sidecar once rather than recomputed, which is what makes it editable: the tree
        /// opens already organised and a builder can then rename, nest and refile. Seeded from the
        /// unfiltered list, so what a search happens to be showing cannot decide the shape.
        /// </remarks>
        private void SeedIfNeeded(CategorySection section, IReadOnlyList<ExplorerItem> items)
        {
            // IsSeeded, not just "has folders". A builder who deliberately empties Areas, Dialogs or
            // Scripts keeps that flag in the sidecar precisely so the empty arrangement survives a
            // restart; reading only Folders.Count meant the next launch recreated the default hierarchy
            // and refiled everything, so a flat section could not be kept. The palette's own seeding
            // already reads the marker this way.
            if (!_seeded.Add(SelectedType) || section.IsSeeded || section.Folders.Count > 0 || items.Count == 0)
                return;

            // Areas are filed by display name, which arrives with the background catalog. Seeding off
            // bare resrefs would put every area in Unsorted and then never try again.
            if (SelectedType == ResourceType.Area && _catalogByType == null)
            {
                _seeded.Remove(SelectedType);
                return;
            }

            var seeded = ModuleFolderSeeder.Seed(
                section, SelectedType, items.Select(item => new SeedableResource(item.ResRef, item.Name)));

            if (seeded == 0)
                return;

            // Recorded in the sidecar, so emptying the section later is respected on the next launch
            // rather than re-seeded. Without this the marker the guard above now reads would never be set.
            section.IsSeeded = true;
            SaveCategories();
            _log.AppendLine(
                $"Organised {SelectedType.DisplayName().ToLowerInvariant()} into {seeded} folder(s). " +
                "Rename, nest or refile them from the right-click menu.");
        }

        private IReadOnlyList<ExplorerItem> LoadItems(ResourceType type)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<ExplorerItem>();

            if (IsCatalogIndexed(type) &&
                _catalogByType != null &&
                _catalogByType.TryGetValue(type, out var entries))
            {
                return entries.Select(entry => new ExplorerItem(entry.ResRef, entry.Name, entry.Tag)).ToList();
            }

            return workspace.EnumerateResRefs(type)
                .Select(resRef => new ExplorerItem(resRef, null, null))
                .ToList();
        }

        /// <summary>
        /// Whether the background catalog indexes this type at all.
        /// </summary>
        /// <remarks>
        /// It indexes areas and the blueprint types, and nothing else. Dialogs and scripts have to stay
        /// enumeration-backed, because creating one calls RefreshCatalogEntry and that single insert
        /// would otherwise make the type look catalog-backed - at which point the list is that one new
        /// resource and every pre-existing dialog or script disappears until restart.
        /// </remarks>
        private static bool IsCatalogIndexed(ResourceType type) =>
            type == ResourceType.Area || ModuleWorkspace.BlueprintTypes.Contains(type);

        private static string SortKey(ExplorerItem item) => item.PrimaryText;

        private static IEnumerable<ExplorerNodeViewModel> Flatten(ExplorerNodeViewModel node) =>
            new[] { node }.Concat(node.Children.SelectMany(Flatten));

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
