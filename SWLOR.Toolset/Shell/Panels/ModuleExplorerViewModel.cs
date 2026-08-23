using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Validation;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
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
        private readonly Stack<ResourceMoveEdit> _resourceMoveUndo = new();
        private readonly Stack<ResourceMoveEdit> _resourceMoveRedo = new();

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

        [ObservableProperty]
        private bool _isDeletingResource;

        /// <summary>The new-area wizard while it is open, or null - the view shows it as an overlay.</summary>
        [ObservableProperty]
        private NewAreaViewModel? _activeNewArea;

        /// <summary>Which tab is showing. Everything else in the panel is scoped to it.</summary>
        [ObservableProperty]
        private ResourceType _selectedType = ResourceType.Area;

        /// <summary>Raised while a pack, validation, or Build All is walking the module folder.</summary>
        private readonly Services.ModuleMutationLock? _mutationLock;

        public ModuleExplorerViewModel(
            WorkspaceContext workspaceContext,
            PropertiesViewModel properties,
            CategoryService categories,
            OutputLogService log,
            Func<Editors.EditorService>? editorService = null,
            TilesetCatalog? tilesetCatalog = null,
            Services.IEditorPromptService? prompts = null,
            Settings.ToolsetSettings? settings = null,
            Services.ModuleMutationLock? mutationLock = null)
        {
            _mutationLock = mutationLock;
            if (_mutationLock != null)
                _mutationLock.Changed += OnMutationLockChanged;

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

            _workspaceContext.CatalogEntryRefreshed += (type, resRef) =>
            {
                InvalidateMoveHistoryForMissingResource(type, resRef);

                if (type == ResourceType.Dlg)
                {
                    _dialogueHitsQuery = null;
                    QueueDialogueScan();
                }

                // Dialogs and scripts are intentionally absent from BlueprintCatalog. Their rows
                // still follow file changes, but no full catalog regroup is needed to do that.
                if (!WorkspaceContext.IsCatalogIndexedType(type))
                    Refresh();
            };
            _workspaceContext.CatalogEntriesChanged += (_, _) =>
            {
                if (_workspaceContext.Catalog is { } catalog)
                    RefreshFromCatalog(catalog);
            };
        }

        /// <summary>
        /// Rename and delete notifications name the resource that just disappeared. A move edit for
        /// that identity can no longer be replayed safely: adding its stale resref back to the
        /// category sidecar would corrupt membership while leaving the renamed file where it is.
        /// </summary>
        private void InvalidateMoveHistoryForMissingResource(ResourceType type, string resRef)
        {
            if (!_resourceMoveUndo.Concat(_resourceMoveRedo).Any(edit =>
                    edit.Type == type &&
                    edit.ResRef.Equals(resRef, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            var stillExists = type == ResourceType.Dlg
                ? ConversationResRefs(workspace).Contains(resRef, StringComparer.OrdinalIgnoreCase)
                : workspace.EnumerateResRefs(type).Contains(resRef, StringComparer.OrdinalIgnoreCase);
            if (!stillExists)
                ClearResourceMoveHistory();
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
        /// Every kind here can be created. Dialogs were the exception while they had no editor —
        /// a blank DLG the toolset could not open was an unusable resource — and the conversation
        /// editor is what lifted that.
        /// </summary>
        /// <remarks>
        /// The lock is the second condition because creating writes into the folders a pack is
        /// copying. Creating an area in particular writes an ARE/GIT/GIC triplet and then edits
        /// module.ifo, so a pack that runs between those two steps can capture an IFO entry with no
        /// area behind it — or an area the module never lists.
        /// </remarks>
        public bool CanCreateSelectedType =>
            !IsDeletingResource &&
            _mutationLock?.IsLocked != true;

        /// <summary>Re-reads <see cref="CanCreateSelectedType"/> after the module-wide lock flips.</summary>
        private void OnMutationLockChanged()
        {
            OnPropertyChanged(nameof(CanCreateSelectedType));
            OnPropertyChanged(nameof(CanCompileSelectedType));
            OnPropertyChanged(nameof(CanDeleteSelectedResource));
            NewItemCommand.NotifyCanExecuteChanged();
            CompileSelectedCommand.NotifyCanExecuteChanged();
            DeleteSelectedResourceCommand.NotifyCanExecuteChanged();
        }

        /// <summary>Builds the tree for the selected tab.</summary>
        public void Initialize()
        {
            ClearResourceMoveHistory();
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

            // Unsorted is also the drag target that takes a resource back out of a folder. Keep it
            // visible when empty; otherwise a completely filed section has no drag-and-drop route out
            // of its folders and the feature disappears precisely when the arrangement is tidiest.
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
            OnPropertyChanged(nameof(CanCompileSelectedType));
            OnPropertyChanged(nameof(CanCreateSelectedType));
            CompileSelectedCommand.NotifyCanExecuteChanged();
            SelectedRow = null;
            StatusMessage = null;
            // The dialogue scan only means anything on the Dialogs tab; leaving one running against
            // another tab spends a full corpus read on a result nothing will read.
            QueueDialogueScan();
            Refresh();
        }

        private int CountFor(ResourceType type)
        {
            if (type == ResourceType.Dlg && _workspaceContext.Workspace is { } workspace)
                return ConversationResRefs(workspace).Count;

            if (IsCatalogIndexed(type)
                && _catalogByType != null
                && _catalogByType.TryGetValue(type, out var entries))
            {
                return entries.Count;
            }

            var resRefs = _workspaceContext.Workspace?.EnumerateResRefs(type);
            if (resRefs == null)
                return 0;

            return resRefs.Count;
        }

        // ----- creating -----

        /// <summary>Creates a resource of the selected type: the area wizard, or a prompt plus a template.</summary>
        [RelayCommand(CanExecute = nameof(CanCreateSelectedType))]
        private async Task NewItemAsync()
        {
            if (!CanCreateSelectedType)
            {
                StatusMessage = "Creating resources is unavailable while the module is being packed or built.";
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
                $"Name for the new {SelectedType.SingularDisplayName().ToLowerInvariant()}. Its ResRef is derived from this.",
                string.Empty,
                "Create");

            if (string.IsNullOrWhiteSpace(name))
                return;

            var resRef = ModuleResourceTemplateFactory.ToResRef(name);
            if (resRef.Length == 0)
            {
                StatusMessage = "That name has no letters or digits to make a ResRef from.";
                return;
            }

            var path = SelectedType == ResourceType.Dlg
                ? workspace.GetConversationGraphPath(resRef)
                : workspace.GetResourcePath(SelectedType, resRef);
            var alreadyExists = File.Exists(path) ||
                                (SelectedType == ResourceType.Dlg &&
                                 File.Exists(workspace.GetResourcePath(ResourceType.Dlg, resRef)));
            if (alreadyExists)
            {
                StatusMessage = $"'{resRef}' already exists.";
                return;
            }

            string? scriptTemplateId = null;
            if (SelectedType == ResourceType.Nss)
            {
                scriptTemplateId = await _prompts.PromptForScriptTemplateAsync(
                    ModuleResourceTemplateFactory.ScriptTemplates);
                if (string.IsNullOrWhiteSpace(scriptTemplateId))
                    return;
            }

            // Rechecked after the prompts: the builder was looking at a dialog while a pack could
            // have started behind it, and the write below is what a pack must not race.
            if (!CanCreateSelectedType)
            {
                StatusMessage = "Creating resources is unavailable while another module operation is in progress.";
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                SaveService.WriteNewAtomic(
                    path,
                    SelectedType == ResourceType.Dlg
                        ? ConversationGraphTemplateFactory.CreateFileContent(resRef, name.Trim())
                        : ModuleResourceTemplateFactory.CreateFileContent(
                            SelectedType, resRef, name.Trim(), scriptTemplateId));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not create '{resRef}': {ex.Message}";
                return;
            }

            // Filed straight into the folder that was selected, so creating inside a folder puts it
            // there rather than dropping it in Unsorted for the builder to move.
            var filed = FileNewResource(resRef);

            _log.AppendLine($"Created {SelectedType.SingularDisplayName().ToLowerInvariant()} '{resRef}'.");
            _workspaceContext.RefreshCatalogEntry(SelectedType, resRef);
            Refresh();

            // Said plainly rather than left to be discovered: the toolset writes .nss source and does
            // not compile it, and NWN runs the compiled .ncs. A new script is a real file in the module
            // but it does nothing until the build pipeline compiles it.
            if (filed)
            {
                StatusMessage = SelectedType == ResourceType.Nss
                    ? $"Created '{resRef}'. It must be compiled to .ncs by the build before the game will run it."
                    : $"Created '{resRef}'.";
            }

            if (CanOpenSelectedType)
                _editorService?.Invoke().TryOpenEditor(SelectedType, resRef);
        }

        private void NewArea()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            // Captured now, not read from the live selection when the wizard's callback fires: the
            // wizard is a nonmodal overlay, so the builder can switch Module Contents tabs or select a
            // different folder while it sits open, and the area must still file into the folder that
            // was current when "New Area..." was clicked - never whatever tab or folder happens to be
            // selected minutes later when they finish the form.
            var targetFolder = SelectedRow?.Folder;

            ActiveNewArea = new NewAreaViewModel(
                workspace,
                _tilesetCatalog,
                resRef =>
                {
                    ActiveNewArea = null;

                    FileNewResource(resRef, targetFolder);

                    _workspaceContext.RefreshCatalogEntry(ResourceType.Area, resRef);
                    _workspaceContext.InvalidatePlacementIndex();
                    Refresh();
                    _editorService?.Invoke().TryOpenEditor(ResourceType.Area, resRef);
                },
                () => ActiveNewArea = null,
                // The wizard writes the ARE/GIT/GIC triplet and then edits module.ifo. A pack that
                // starts between those two writes captures one without the other, so the wizard has
                // to ask again at the moment it commits rather than trusting the check that opened it.
                () => !IsDeletingResource && _mutationLock?.IsLocked != true);
        }

        /// <summary>
        /// Files a freshly created resource into the currently selected folder, reporting a sidecar
        /// failure rather than letting the "Created ..." message overwrite it.
        /// </summary>
        /// <returns>
        /// False when the resource was created but could not be filed, in which case the caller
        /// leaves the sidecar failure on screen. True when there was nothing to file, or filing worked.
        /// </returns>
        private bool FileNewResource(string resRef) => FileNewResource(resRef, SelectedRow?.Folder);

        /// <summary>
        /// Files a freshly created resource into an explicit folder rather than reading the current
        /// selection. Callers whose creation flow can outlive a change of tab or selection - the
        /// nonmodal New Area wizard chief among them - must capture the folder up front and pass it
        /// through here instead of letting this read whatever happens to be selected when it runs.
        /// </summary>
        /// <returns>
        /// False when the resource was created but could not be filed, in which case the caller
        /// leaves the sidecar failure on screen. True when there was nothing to file, or filing worked.
        /// </returns>
        private bool FileNewResource(string resRef, CategoryFolder? folder)
        {
            if (folder == null)
                return true;

            folder.AddMember(resRef);
            if (SaveCategories())
                return true;

            // SaveCategories restored the persisted catalog, so the file exists but is in Unsorted.
            // Said explicitly: silently filing it somewhere else is how a builder loses track of it.
            StatusMessage =
                $"Created '{resRef}', but it could not be filed under '{folder.Name}' — it is in Unsorted. {StatusMessage}";
            return false;
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

            // Checked rather than sanitized: the builder typed this and is still here to retype it, so
            // say what is wrong instead of quietly hyphenating a name they did not ask for. The
            // constructor would throw, and an exception out of a command handler has nowhere to go.
            // Asked before the sibling check, so a name holding a separator is reported as that rather
            // than as a clash with whatever the split happened to land on.
            if (CategoryFolder.NameProblem(name) is { } problem)
            {
                StatusMessage = problem;
                return;
            }

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

            if (CategoryFolder.NameProblem(name) is { } problem)
            {
                StatusMessage = problem;
                return;
            }

            var section = _categories.Section(SelectedType);
            var trimmed = name.Trim();
            if (section == null || !section.TryRenameFolder(folder, trimmed))
            {
                StatusMessage = $"A folder named '{trimmed}' already exists here.";
                return;
            }

            SaveCategories();
            ClearResourceMoveHistory();
            Refresh();

            // Refresh() rebuilds every node, so the pre-rebuild SelectedRow is now orphaned. Rename
            // mutates the CategoryFolder in place, so the same reference finds its rebuilt row.
            SelectedRow = Rows.FirstOrDefault(row => ReferenceEquals(row.Folder, folder));
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
            ClearResourceMoveHistory();
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
            if (SelectedRow is not { } source)
                return;

            MoveResource(source, target);
        }

        /// <summary>Takes the selected resource out of every folder, back to Unsorted.</summary>
        [RelayCommand]
        private void RemoveFromFolder()
        {
            if (SelectedRow is not { } source)
                return;

            MoveResource(source, target: null);
        }

        /// <summary>
        /// Whether a resource row can be dropped on a folder row. The synthetic Unsorted row is a
        /// valid destination even though it has no <see cref="CategoryFolder"/> behind it.
        /// </summary>
        public bool CanDropResource(ExplorerNodeViewModel? source, ExplorerNodeViewModel? target)
        {
            if (source?.Item == null || target?.IsBranch != true ||
                source.Type != SelectedType || target.Type != SelectedType)
            {
                return false;
            }

            var section = _categories.Section(SelectedType);
            if (section == null ||
                (target.Folder == null &&
                 !string.Equals(target.Name, CategorySection.UnsortedFolderName, StringComparison.Ordinal)))
            {
                return false;
            }

            var current = section.FoldersContaining(source.Item.ResRef).ToList();
            return target.Folder == null
                ? current.Count > 0
                : current.Count != 1 || !ReferenceEquals(current[0], target.Folder);
        }

        /// <summary>
        /// Commits a drag from a resource row to a real folder or to Unsorted. Source and destination
        /// are passed explicitly rather than read from selection: pointer movement and auto-scroll may
        /// change selection during a drag, and the item under the pointer must not become the item moved.
        /// </summary>
        public bool DropResource(ExplorerNodeViewModel? source, ExplorerNodeViewModel? target)
        {
            if (!CanDropResource(source, target))
                return false;

            return MoveResource(source!, target!.Folder);
        }

        private bool MoveResource(ExplorerNodeViewModel source, CategoryFolder? target)
        {
            var section = _categories.Section(SelectedType);
            if (section == null || source.Item is not { } item || source.Type != SelectedType)
                return false;

            var beforeFolderPaths = section.FoldersContaining(item.ResRef)
                .Select(section.PathKey)
                .ToArray();
            var afterFolderPaths = target == null
                ? Array.Empty<string>()
                : new[] { section.PathKey(target) };

            // Out of wherever it was first: a resref may legally sit in several folders, but a move the
            // builder asked for means one destination, not an extra one. A null destination is Unsorted.
            var changed = false;
            foreach (var folder in section.AllFolders())
                changed |= folder.RemoveMember(item.ResRef);

            if (target != null)
                changed |= target.AddMember(item.ResRef);

            if (!changed)
                return false;

            var saved = SaveCategories();
            Refresh();
            if (!saved)
                return false;

            _resourceMoveUndo.Push(new ResourceMoveEdit(
                SelectedType,
                item.ResRef,
                item.PrimaryText,
                beforeFolderPaths,
                afterFolderPaths));
            _resourceMoveRedo.Clear();
            NotifyResourceMoveHistoryChanged();
            StatusMessage = target == null
                ? $"Moved '{item.PrimaryText}' to Unsorted."
                : $"Moved '{item.PrimaryText}' to '{string.Join(" / ", section.PathTo(target))}'.";
            return saved;
        }

        private bool CanUndoResourceMove() => _resourceMoveUndo.Count > 0;

        [RelayCommand(CanExecute = nameof(CanUndoResourceMove))]
        private void UndoResourceMove()
        {
            if (!_resourceMoveUndo.TryPeek(out var edit) ||
                !ApplyResourceMove(edit, edit.BeforeFolderPaths, "undo"))
            {
                return;
            }

            _resourceMoveUndo.Pop();
            _resourceMoveRedo.Push(edit);
            NotifyResourceMoveHistoryChanged();
            StatusMessage = $"Undid move of '{edit.DisplayName}'.";
        }

        private bool CanRedoResourceMove() => _resourceMoveRedo.Count > 0;

        [RelayCommand(CanExecute = nameof(CanRedoResourceMove))]
        private void RedoResourceMove()
        {
            if (!_resourceMoveRedo.TryPeek(out var edit) ||
                !ApplyResourceMove(edit, edit.AfterFolderPaths, "redo"))
            {
                return;
            }

            _resourceMoveRedo.Pop();
            _resourceMoveUndo.Push(edit);
            NotifyResourceMoveHistoryChanged();
            StatusMessage = $"Redid move of '{edit.DisplayName}'.";
        }

        private bool ApplyResourceMove(
            ResourceMoveEdit edit,
            IReadOnlyList<string> destinationFolderPaths,
            string operation)
        {
            var section = _categories.Section(edit.Type);
            if (section == null)
            {
                ClearResourceMoveHistory();
                StatusMessage = $"Cannot {operation} the move because its resource section no longer exists.";
                return false;
            }

            var destinations = new List<CategoryFolder>();
            foreach (var path in destinationFolderPaths)
            {
                var folder = section.FindByPathKey(path);
                if (folder == null)
                {
                    ClearResourceMoveHistory();
                    StatusMessage = $"Cannot {operation} the move because folder '{path}' no longer exists.";
                    Refresh();
                    return false;
                }

                destinations.Add(folder);
            }

            foreach (var folder in section.AllFolders())
                folder.RemoveMember(edit.ResRef);
            foreach (var folder in destinations)
                folder.AddMember(edit.ResRef);

            // Persist even when the in-memory tree already matches the requested destination. This
            // can be a retry after a refused write: the history entry is intentionally retained, and
            // success must mean the sidecar on disk now matches the undo/redo state too.
            if (!SaveCategories())
            {
                Refresh();
                return false;
            }

            Refresh();
            return true;
        }

        private void ClearResourceMoveHistory()
        {
            if (_resourceMoveUndo.Count == 0 && _resourceMoveRedo.Count == 0)
                return;

            _resourceMoveUndo.Clear();
            _resourceMoveRedo.Clear();
            NotifyResourceMoveHistoryChanged();
        }

        private void NotifyResourceMoveHistoryChanged()
        {
            UndoResourceMoveCommand.NotifyCanExecuteChanged();
            RedoResourceMoveCommand.NotifyCanExecuteChanged();
        }

        private sealed record ResourceMoveEdit(
            ResourceType Type,
            string ResRef,
            string DisplayName,
            IReadOnlyList<string> BeforeFolderPaths,
            IReadOnlyList<string> AfterFolderPaths);


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
        /// Only real area, dialog, and script rows can be deleted, and never while a module-wide
        /// reader or writer owns the workspace.
        /// </summary>
        public bool CanDeleteSelectedResource =>
            !IsDeletingResource &&
            SelectedRow?.Item != null &&
            SelectedRow.Type is ResourceType.Area or ResourceType.Dlg or ResourceType.Nss &&
            _mutationLock?.IsLocked != true;

        /// <summary>
        /// Deletes the logical resource represented by the selected row. Areas include their whole
        /// triplet and module registration; dialogs include graph and legacy forms; scripts include
        /// source and same-resref compiled output.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDeleteSelectedResource))]
        private async Task DeleteSelectedResourceAsync()
        {
            var row = SelectedRow;
            var item = row?.Item;
            var workspace = _workspaceContext.Workspace;
            if (row == null || item == null || workspace == null || _prompts == null)
                return;

            var type = row.Type;
            var resRef = item.ResRef;
            var displayName = string.IsNullOrWhiteSpace(item.Name) ? row.Name : item.Name;
            var kind = type.SingularDisplayName().ToLowerInvariant();
            var editorService = _editorService?.Invoke();

            if (type == ResourceType.Area && editorService?.IsModulePropertiesOpen == true)
            {
                StatusMessage = "Module Properties is open - close that tab before deleting an area.";
                return;
            }

            var section = _categories.Section(type);
            if (section?.FoldersContaining(resRef).Any() == true)
            {
                var preflight = _categories.CanSaveChanges();
                if (!preflight.Saved)
                {
                    StatusMessage = $"'{displayName}' was not deleted: {preflight.Problem}";
                    _log.AppendLine($"Deleting {kind} '{resRef}' was refused: {preflight.Problem}");
                    return;
                }
            }

            ModuleResourceDeletionPlan plan;
            try
            {
                plan = ModuleResourceDeletionService.Prepare(workspace, type, resRef);
            }
            catch (Exception ex)
            {
                StatusMessage = $"'{displayName}' was not deleted: {ex.Message}";
                _log.AppendLine($"Deleting {kind} '{resRef}' was refused: {ex.Message}");
                return;
            }

            var files = string.Join(", ", plan.ExistingFileNames);
            var details = type switch
            {
                ResourceType.Area =>
                    $"This deletes {files}" +
                    (plan.RemovesAreaRegistration ? " and removes the area from module.ifo.json" : string.Empty) +
                    ". Transitions and other references to this area are not removed.",
                ResourceType.Dlg =>
                    $"This deletes every source form of the dialog ({files}). Objects and scripts that reference it are not changed.",
                ResourceType.Nss =>
                    $"This deletes the script source and any compiled output that exists ({files}). References to the script are not changed.",
                _ => throw new ArgumentOutOfRangeException()
            };

            var closesOpenEditor = editorService?.IsOpen(type, resRef) == true;
            var confirmed = await _prompts.ConfirmDestructiveAsync(
                $"Delete the {kind} '{displayName}'?",
                details +
                (closesOpenEditor
                    ? " Its open editor will be closed and any unsaved changes discarded."
                    : string.Empty) +
                " This cannot be undone from the toolset.",
                "Delete").ConfigureAwait(true);
            if (!confirmed)
                return;

            // The warning is scoped to the editor state the builder confirmed. If the resource was
            // closed when the dialog opened but became open while it was displayed, do not silently
            // extend that consent to discarding a new buffer. Leave it intact and require a second
            // Delete whose confirmation describes the close explicitly.
            if (!closesOpenEditor && editorService?.IsOpen(type, resRef) == true)
            {
                StatusMessage =
                    $"'{displayName}' was opened while the delete confirmation was active. " +
                    "It was not deleted; choose Delete again to confirm closing its editor.";
                return;
            }

            // The destructive prompt above covers the open buffer too. Close it now so its document
            // sessions cannot save the resource back after the filesystem transaction completes.
            if (editorService?.IsOpen(type, resRef) == true &&
                !editorService.TryCloseResourceForDeletion(type, resRef))
            {
                StatusMessage = $"'{displayName}' was not deleted because its editor could not be closed.";
                return;
            }

            // Reserve the shared deletion state before rechecking editor ownership. The reservation
            // blocks every editor-opening route until the deletion and its catalog cleanup finish,
            // closing the race between these checks and entering the filesystem transaction.
            using var deletionOperation = _mutationLock?.TryBeginResourceDeletion();
            if (_mutationLock != null && deletionOperation == null)
            {
                StatusMessage = $"'{displayName}' was not deleted: the module is being packed, validated, or built.";
                _log.AppendLine($"Deleting {kind} '{resRef}' was refused: the module is locked.");
                return;
            }

            if (editorService?.IsOpen(type, resRef) == true)
            {
                StatusMessage = $"'{displayName}' is now open in an editor - close that tab first.";
                return;
            }

            if (type == ResourceType.Area && editorService?.IsModulePropertiesOpen == true)
            {
                StatusMessage = "Module Properties is now open - close that tab before deleting an area.";
                return;
            }

            if (section?.FoldersContaining(resRef).Any() == true)
            {
                var recheck = _categories.CanSaveChanges();
                if (!recheck.Saved)
                {
                    StatusMessage = $"'{displayName}' was not deleted: {recheck.Problem}";
                    _log.AppendLine($"Deleting {kind} '{resRef}' was refused: {recheck.Problem}");
                    return;
                }
            }

            ModuleResourceDeletionResult result;
            IsDeletingResource = true;
            StatusMessage = $"Deleting {kind} '{displayName}'...";
            try
            {
                result = await Task.Run(() =>
                    {
                        // The deletion owns the shared module-operation lock. Its worker alone may
                        // perform the guarded filesystem writes while every editor/save/open route
                        // remains blocked until this command finishes its catalog cleanup.
                        using var allowance = ModuleMutationLock.AllowModuleWrites();
                        return ModuleResourceDeletionService.Commit(plan);
                    })
                    .ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                StatusMessage = $"'{displayName}' was not deleted: {ex.Message}";
                _log.AppendLine($"Deleting {kind} '{resRef}' failed: {ex.Message}");
                return;
            }
            finally
            {
                IsDeletingResource = false;
            }

            _workspaceContext.RemoveCatalogEntry(type, resRef);

            var unfiled = true;
            if (section != null)
            {
                var folders = section.FoldersContaining(resRef).ToList();
                foreach (var folder in folders)
                    folder.RemoveMember(resRef);

                if (folders.Count > 0)
                    unfiled = SaveCategories();
            }

            SelectedRow = null;
            ClearResourceMoveHistory();
            Refresh();

            var cleanup = result.CleanupWarnings.Count == 0
                ? string.Empty
                : $" Temporary delete backup cleanup needs attention: {string.Join("; ", result.CleanupWarnings)}";
            if (unfiled)
            {
                StatusMessage = $"Deleted {kind} '{displayName}'.{cleanup}";
            }
            else
            {
                StatusMessage =
                    $"Deleted {kind} '{displayName}', but its Module Contents folder still lists it. {StatusMessage}{cleanup}";
            }

            _log.AppendLine(
                $"Deleted {kind} '{resRef}' ({string.Join(", ", result.DeletedPaths)}).{cleanup}");
        }

        /// <summary>
        /// Whether the selected tab's resources can actually be opened. All three kinds can now:
        /// areas in the area editor, scripts in the script editor, and conversations in Play-it.
        /// </summary>
        public bool CanOpenSelectedType =>
            !IsDeletingResource &&
            SelectedType is ResourceType.Area or ResourceType.Nss or ResourceType.Dlg;

        /// <summary>
        /// Scripts are the one resource kind with a build step, so they are the only one that offers
        /// Compile. Lets a builder rebuild a script without opening it — useful after editing an
        /// include, when the dependents needing a rebuild are not the file you were working in.
        /// </summary>
        public bool CanCompileSelectedType =>
            !IsDeletingResource &&
            SelectedType == ResourceType.Nss &&
            _mutationLock?.IsLocked != true;

        [RelayCommand(CanExecute = nameof(CanCompileSelectedType))]
        private async Task CompileSelected()
        {
            if (SelectedRow?.Item is not { } item || SelectedType != ResourceType.Nss)
                return;

            if (!CanCompileSelectedType)
            {
                StatusMessage = "Compiling scripts is unavailable while another module operation is in progress.";
                return;
            }

            if (_editorService == null)
                return;

            StatusMessage = $"Compiling {item.ResRef}...";
            await _editorService.Invoke().CompileScriptAsync(item.ResRef).ConfigureAwait(true);
            StatusMessage = null;
        }

        /// <summary>The context menu's Open, which is the double-click by another route.</summary>
        [RelayCommand(CanExecute = nameof(CanOpenSelectedType))]
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
                StatusMessage = IsDeletingResource
                    ? "Opening resources is unavailable while a delete is in progress."
                    : $"{SelectedType.DisplayName()} cannot be edited in the toolset yet.";
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

        partial void OnFilterChanged(string value)
        {
            QueueDialogueScan();
            Refresh();
        }

        /// <summary>True while a dialogue-text scan is running, so the tree can say so.</summary>
        [ObservableProperty]
        private bool _isSearchingDialogue;

        // ----- dialogue-text search -----

        /// <summary>
        /// How long typing has to pause before the corpus is read. The scan opens all 609
        /// conversations, so this is the difference between one scan for a word and one per letter.
        /// </summary>
        private static readonly TimeSpan DialogueSearchDebounce = TimeSpan.FromMilliseconds(300);

        /// <summary>The resrefs the last completed scan matched, and the query it was run for.</summary>
        private HashSet<string>? _dialogueHits;

        private string? _dialogueHitsQuery;

        /// <summary>Cancels the scan in flight when the query changes underneath it.</summary>
        private CancellationTokenSource? _dialogueScan;

        /// <summary>
        /// Starts a background dialogue scan for the current query, after a pause in typing.
        /// </summary>
        /// <remarks>
        /// This used to run inline from <c>Refresh</c>, which put a full read of the conversation
        /// corpus - about a second - on the keystroke path: typing a five-letter word froze the
        /// window five times over, and four of those scans were for prefixes nobody wanted results
        /// for. Now the keystroke only schedules, the scan runs off the UI thread, and a result is
        /// published only if its query is still the one in the box.
        /// </remarks>
        private void QueueDialogueScan()
        {
            _dialogueScan?.Cancel();
            _dialogueScan?.Dispose();
            _dialogueScan = null;

            var needle = Filter?.Trim() ?? string.Empty;
            if (SelectedType != ResourceType.Dlg || needle.Length == 0)
            {
                _dialogueHits = null;
                _dialogueHitsQuery = null;
                IsSearchingDialogue = false;
                return;
            }

            // Already have it: retyping the same query should not re-read the corpus.
            if (string.Equals(_dialogueHitsQuery, needle, StringComparison.OrdinalIgnoreCase))
                return;

            var moduleRoot = _workspaceContext.Workspace?.ModuleRoot;
            if (moduleRoot == null)
                return;

            IsSearchingDialogue = true;

            var pending = new CancellationTokenSource();
            _dialogueScan = pending;
            var token = pending.Token;

            // Deep-snapshot open-editor documents and graphs on the UI thread so the worker-side
            // search sees unsaved edits instead of stale on-disk copies, without ever touching live
            // editor state that the UI may mutate concurrently.
            var openDialogs = _editorService?.Invoke().SnapshotOpenConversationDocuments();
            var openGraphs = _editorService?.Invoke().SnapshotOpenNuiConversationGraphs();

            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        // The debounce itself has to be awaited here, not just declared - this is what
                        // turns "read the corpus on every keystroke" into "read it once typing pauses".
                        await Task.Delay(DialogueSearchDebounce, token).ConfigureAwait(false);

                        var graphDirectory = _workspaceContext.Workspace?.ConversationDataRoot;
                        var matching = DialogueSearch
                            .Search(
                                Path.Combine(moduleRoot, "dlg"),
                                needle,
                                cancellationToken: token,
                                openDocument: resRef =>
                                    openDialogs != null && openDialogs.TryGetValue(resRef, out var open)
                                        ? open
                                        : null,
                                conversationGraphDirectory: graphDirectory,
                                openGraph: resRef =>
                                    openGraphs != null && openGraphs.TryGetValue(resRef, out var graph)
                                        ? graph
                                        : null)
                            .Select(hit => hit.ResRef)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        PublishDialogueHits(needle, matching, token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Superseded by a later query; the scan that replaced it owns the result.
                    }
                    catch (Exception ex)
                    {
                        PublishDialogueFailure(ex, token);
                    }
                },
                token);
        }

        private void PublishDialogueHits(string needle, HashSet<string> matching, CancellationToken token)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                _dialogueHitsQuery = needle;
                _dialogueHits = matching;
                IsSearchingDialogue = false;
                Refresh();
            });
        }

        private void PublishDialogueFailure(Exception ex, CancellationToken token)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                IsSearchingDialogue = false;
                StatusMessage = $"Dialogue search failed: {ex.Message}";
            });
        }

        partial void OnSelectedRowChanged(ExplorerNodeViewModel? value)
        {
            OnPropertyChanged(nameof(HasFolderSelected));
            OnPropertyChanged(nameof(CanDeleteSelectedResource));
            DeleteSelectedResourceCommand.NotifyCanExecuteChanged();

            if (value?.Item == null)
                return;

            var item = value.Item;
            _properties.ShowEntry(new CatalogEntry(value.Type, item.ResRef, item.Name, item.Tag, string.Empty));

            // Nothing in this panel has a model any more - areas, dialogs and scripts all have
            // none - so the preview is left showing whatever the Palette last put there rather than
            // being cleared to "no preview available" on every click.
        }

        partial void OnIsDeletingResourceChanged(bool value)
        {
            OnPropertyChanged(nameof(CanCreateSelectedType));
            OnPropertyChanged(nameof(CanOpenSelectedType));
            OnPropertyChanged(nameof(CanCompileSelectedType));
            OnPropertyChanged(nameof(CanDeleteSelectedResource));
            NewItemCommand.NotifyCanExecuteChanged();
            OpenSelectedCommand.NotifyCanExecuteChanged();
            CompileSelectedCommand.NotifyCanExecuteChanged();
            DeleteSelectedResourceCommand.NotifyCanExecuteChanged();
        }

        // ----- tree assembly -----

        private IReadOnlyList<ExplorerItem> Filtered(IReadOnlyList<ExplorerItem> items)
        {
            if (string.IsNullOrWhiteSpace(Filter))
                return items;

            var needle = Filter.Trim();
            if (SelectedType == ResourceType.Dlg)
                return DialogueMatches(items, needle);

            return items
                .Where(item =>
                    item.ResRef.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    (item.Name?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        /// <summary>
        /// Conversations matching a ResRef, name, or something somebody says.
        /// </summary>
        /// <remarks>
        /// Reads whatever the last completed background scan found rather than scanning here: this
        /// runs on the UI thread from <c>Refresh</c>, and reading all 609 conversations from it is
        /// what made the search box appear hung. <see cref="QueueDialogueScan"/> owns the reading.
        /// </remarks>
        private IReadOnlyList<ExplorerItem> DialogueMatches(IReadOnlyList<ExplorerItem> items, string needle)
        {
            if (_workspaceContext.Workspace == null)
                return items;

            var ordinaryMatch = new Func<ExplorerItem, bool>(item =>
                item.ResRef.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                (item.Name?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false));

            // Names and ResRefs are immediate. Spoken-text matches join them when the background
            // corpus scan lands, so the single search box never blanks a result it already knows.
            if (_dialogueHits == null ||
                !string.Equals(_dialogueHitsQuery, needle, StringComparison.OrdinalIgnoreCase))
                return items.Where(ordinaryMatch).ToList();

            return items
                .Where(item => ordinaryMatch(item) || _dialogueHits.Contains(item.ResRef))
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

            if (type == ResourceType.Dlg)
            {
                return ConversationResRefs(workspace)
                    .Select(resRef => new ExplorerItem(resRef, null, null))
                    .ToList();
            }

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
        /// Graph-native conversations and explicit legacy exceptions share the Dialogs explorer tab.
        /// A graph wins a duplicate resref.
        /// </summary>
        private static IReadOnlyList<string> ConversationResRefs(ModuleWorkspace workspace) =>
            workspace.EnumerateConversationGraphResRefs()
                .Concat(workspace.EnumerateResRefs(ResourceType.Dlg))
                .Where(resRef => !UnreferencedConversationRule.IsGeneratedShell(resRef))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(resRef => resRef, StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>
        /// Whether the background catalog indexes this type at all.
        /// </summary>
        /// <remarks>
        /// It indexes areas and the blueprint types, and nothing else. Dialogs and scripts have to stay
        /// enumeration-backed, because creating one calls RefreshCatalogEntry and that single insert
        /// would otherwise make the type look catalog-backed - at which point the list is that one new
        /// resource and every pre-existing dialog or script disappears until restart. Delegated to
        /// <see cref="WorkspaceContext.IsCatalogIndexedType"/> so this can never drift from what
        /// <see cref="WorkspaceContext"/> actually inserts into the catalog on a refresh.
        /// </remarks>
        private static bool IsCatalogIndexed(ResourceType type) =>
            WorkspaceContext.IsCatalogIndexedType(type);

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
            // Filtered folder counts already include every matching descendant. A zero therefore means
            // neither this category nor anything beneath it can lead to a search result. Keep the full
            // tree in _roots so clearing the search restores its expansion state, but do not publish
            // dead-end categories (including an empty Unsorted bucket) while the builder is searching.
            if (!string.IsNullOrWhiteSpace(Filter) && node.IsBranch && node.Count == 0)
                return;

            Rows.Add(node);
            if (!node.IsExpanded)
                return;

            foreach (var child in node.Children)
                Publish(child);
        }
    }
}
