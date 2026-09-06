using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>A titled group of field view models.</summary>
    public sealed record EditorGroup(string Title, IReadOnlyList<FieldViewModel> Fields);

    /// <summary>
    /// The generic schema-driven blueprint editor, docked as a document tab. Every mutation
    /// flows through a one-step DocumentTransaction on the session's undo stack; Save writes
    /// the document bytes atomically and marks the stack clean.
    /// </summary>
    public partial class BlueprintEditorViewModel : Document, IEditorDocument
    {
        private readonly DocumentSession _session;
        private readonly EditorFieldContext _context;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;
        private readonly LookupOptionProvider _lookups;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly IScriptSlotHost? _scriptSlotHost;
        private readonly BlueprintSaveCoordinator? _saveCoordinator;

        /// <summary>Supplies the module workspace to resource pickers; null leaves them free-text.</summary>
        private readonly Func<Domain.Workspace.ModuleWorkspace?>? _resourceLister;
        private string _resRef;
        private bool _closeApproved;
        private bool _closePromptOpen;
        private bool _disposed;

        public ObservableCollection<EditorGroup> Groups { get; } = new();

        /// <summary>
        /// The editor's tabs. A schema that names no tab produces one page holding every group,
        /// which is what every type but the placeable does; the view hides the strip in that case.
        /// </summary>
        public ObservableCollection<EditorTabViewModel> Tabs { get; } = new();

        /// <summary>The raw Conversation tab content, held so Advanced follows Custom.</summary>
        private EditorTabViewModel? _advancedTab;

        private EditorTabViewModel? _selectedTab;

        /// <summary>
        /// Which tab is showing. Held here rather than left to the TabControl because the docking
        /// host rebuilds the view when you switch documents - so a selection kept in the control is
        /// lost the moment you look at something else and come back.
        /// </summary>
        public EditorTabViewModel? SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (ReferenceEquals(_selectedTab, value))
                    return;

                _selectedTab = value;
                OnPropertyChanged();

                // The retained 3D preview now stays in the editor's left rail across every
                // placeable tab, matching the Door editor.
                PlaceableSections?.Appearance.SetTabVisible(true);

                // The model grid continues loading independently; selecting Appearance is still
                // the natural point to ensure that work has started.
                if (PlaceableSections != null && ReferenceEquals(value?.Content, PlaceableSections.Appearance))
                    PlaceableSections.Appearance.EnsureLoaded();
            }
        }

        /// <summary>
        /// False for a schema that declares no tabs, where a tab strip over a single page would be
        /// chrome around nothing.
        /// </summary>
        public bool HasMultipleTabs => Tabs.Count > 1;

        /// <summary>The only tab's content, for the single-page case.</summary>
        public object? SingleTabContent => Tabs.Count > 0 ? Tabs[0].Content : null;

        public VarTableSectionViewModel? VarTableSection { get; private set; }

        /// <summary>The placeable's Appearance and Behavior tabs, or null for every other type.</summary>
        public Placeables.PlaceableEditorSections? PlaceableSections { get; }

        /// <summary>
        /// The searchable appearance grid, for blueprint types that have one. Null leaves the
        /// schema's own appearance field as the only way to set it.
        /// </summary>
        public Appearance.AppearanceGallerySectionViewModel? AppearanceGallery { get; }

        public Sources.ObjectSourceSectionViewModel? Source { get; }
        public TintMaps.TintMapEditorViewModel? TintMapEditor { get; }

        /// <summary>
        /// The generic blueprint view hosts the placeable editor too; only that editor shows the
        /// Waypoint-style Save/Revert footer requested for its multi-tab authoring workflow.
        /// </summary>
        public bool IsPlaceableEditor => PlaceableSections != null;

        public bool IsDirty => _session.UndoStack.IsDirty;

        public string FilePath => _session.FilePath;

        public string ResRef => _resRef;

        /// <summary>This blueprint's resource type — lets the model preview resolve its appearance.</summary>
        public ResourceType BlueprintType { get; }

        /// <summary>The live (possibly unsaved) document root, for appearance-driven model preview.</summary>

        /// <summary>Raised after this resource is saved or reloaded so catalog views can re-index it.</summary>
        public event Action? CatalogEntryChanged;

        public event Action<BlueprintEditorViewModel, string, string>? Renamed;

        public BlueprintEditorViewModel(
            string filePath,
            string resRef,
            ResourceType type,
            EditorSchema schema,
            LookupOptionProvider lookups,
            IGameCodeIndex? gameCodeIndex,
            OutputLogService log,
            IEditorPromptService prompts,
            Func<uint, string?>? resolveStrRef = null,
            IScriptSlotHost? scriptSlotHost = null,
            Func<EditorFieldContext, Func<string, Action, bool>, IScriptSlotHost?,
                Func<string?, IReadOnlyList<string>>, Placeables.PlaceableEditorSections?>? placeableSections = null,
            Func<Domain.Workspace.ModuleWorkspace?>? resourceLister = null,
            Func<EditorFieldContext, Func<string, Action, bool>,
                Appearance.AppearanceGallerySectionViewModel?>? appearanceGallery = null,
            Func<EditorFieldContext, Func<string, Action, bool>,
                TintMaps.TintMapEditorViewModel?>? tintMapEditor = null,
            BlueprintSaveCoordinator? saveCoordinator = null,
            Sources.ObjectSourceSectionViewModel? source = null,
            Action<uint>? openTlkRow = null)
        {
            _scriptSlotHost = scriptSlotHost;
            _resourceLister = resourceLister;
            _log = log;
            _prompts = prompts;
            _lookups = lookups;
            _gameCodeIndex = gameCodeIndex;
            _saveCoordinator = saveCoordinator;
            Source = source;
            _resRef = resRef;
            BlueprintType = type;
            Id = $"editor:{filePath}";
            _session = DocumentSession.Open(filePath);
            _context = new EditorFieldContext(_session.Document, RunEdit, resolveStrRef, openTlkRow);

            var tabbedGroups = new List<(string Tab, EditorGroup Group)>();
            foreach (var group in schema.Groups)
            {
                var fields = group.Fields
                    .Select(descriptor => FieldViewModelFactory.Create(
                        descriptor, _context, lookups, scriptSlotHost, ResourceChoices))
                    .ToList();
                var editorGroup = new EditorGroup(group.Title, fields);
                Groups.Add(editorGroup);
                tabbedGroups.Add((group.Tab, editorGroup));
            }

            if (schema.HasVarTable)
            {
                VarTableSection = new VarTableSectionViewModel(
                    _context.RunEdit, new VarTable(_session.Document.Root), gameCodeIndex);
            }

            // A creature's appearance is the most visual choice in the module, and it was the one
            // place still picked from a list of names. The gallery is the same control the door
            // editor uses; only the rows differ.
            AppearanceGallery = appearanceGallery?.Invoke(_context, RunEdit);
            TintMapEditor = tintMapEditor?.Invoke(_context, RunEdit);

            PlaceableSections = placeableSections?.Invoke(
                _context, RunEdit, _scriptSlotHost, ResourceChoices);
            if (PlaceableSections != null)
            {
                PlaceableSections.Appearance.AppearanceChanged += AfterHistoryChange;
                PlaceableSections.Behavior.BehaviorChanged += OnBehaviorChanged;
            }

            BuildTabs(tabbedGroups);
            UpdateTitle();
        }

        /// <summary>
        /// Lays the tabs out: schema tabs in declared order, then the placeable's own two, then
        /// Variables - which is present only when it has something to hold.
        /// </summary>
        private void BuildTabs(IReadOnlyList<(string Tab, EditorGroup Group)> tabbedGroups)
        {
            Tabs.Clear();

            foreach (var tabTitle in tabbedGroups.Select(entry => entry.Tab).Distinct())
            {
                // A placeable's raw conversation slot and event scripts belong to the Custom
                // behavior, so a fully initialized editor shows them there rather than on a second
                // page of fields. Without game data there is no Behavior tab, and they stay here.
                var groups = tabbedGroups
                    .Where(entry => entry.Tab == tabTitle)
                    .Where(entry => PlaceableSections == null ||
                                    !Domain.Editors.Schemas.UtpSchema.CustomBehaviorGroupTitles
                                        .Contains(entry.Group.Title, StringComparer.Ordinal))
                    .Select(entry => entry.Group);

                var title = string.IsNullOrEmpty(tabTitle) ? "Properties" : tabTitle;
                Tabs.Add(new EditorTabViewModel(title, new FieldGroupsViewModel(groups)));
            }

            if (PlaceableSections != null)
            {
                // Appearance and Behavior belong right beside Basic: they are the two things a
                // placeable is.
                Tabs.Insert(Math.Min(1, Tabs.Count),
                    new EditorTabViewModel("Behavior", PlaceableSections.Behavior));
                Tabs.Insert(Math.Min(1, Tabs.Count),
                    new EditorTabViewModel("Appearance", PlaceableSections.Appearance));
            }
            else if (AppearanceGallery != null)
            {
                // Beside Basic for the same reason: what the thing looks like is one of the two
                // facts a builder opens it to change.
                Tabs.Insert(Math.Min(1, Tabs.Count),
                    new EditorTabViewModel("Appearance", AppearanceGallery));
            }

            if (TintMapEditor != null)
            {
                var appearanceIndex = Tabs
                    .Select((tab, index) => (tab, index))
                    .Where(entry => entry.tab.Title == "Appearance")
                    .Select(entry => entry.index)
                    .DefaultIfEmpty(0)
                    .First();
                Tabs.Insert(Math.Min(appearanceIndex + 1, Tabs.Count),
                    new EditorTabViewModel("Tints", TintMapEditor));
            }

            if (VarTableSection != null && ShouldShowVariablesTab())
                Tabs.Add(new EditorTabViewModel("Variables", VarTableSection));

            if (Source != null)
                Tabs.Add(new EditorTabViewModel("Source", Source));

            NotifyTabsChanged();
        }

        private void NotifyTabsChanged()
        {
            if (SelectedTab == null || !Tabs.Contains(SelectedTab))
                SelectedTab = Tabs.FirstOrDefault();

            OnPropertyChanged(nameof(HasMultipleTabs));
            OnPropertyChanged(nameof(SingleTabContent));
        }

        /// <summary>
        /// Whether the raw variable grid gets a tab. For a placeable that is the Custom behavior, or
        /// any behavior sitting on variables it does not own - hiding stored data would be worse
        /// than an extra tab. Every other blueprint type keeps the grid it has always had.
        /// </summary>
        private bool ShouldShowVariablesTab()
        {
            if (PlaceableSections == null)
                return true;

            if (PlaceableSections.Behavior.AllowsRawEditing)
                return true;

            return Domain.Placeables.PlaceableBehaviorDetector
                .UnmanagedVariables(_session.Document.Root, PlaceableSections.Behavior.Current)
                .Count > 0;
        }

        /// <summary>
        /// A behavior switch can add or remove the Variables tab and rewrites script slots, so the
        /// tab strip has to catch up.
        /// </summary>
        private void OnBehaviorChanged()
        {
            RefreshAllFields();
            RebuildVariablesTab();
            AfterHistoryChange();
        }

        private void RebuildVariablesTab()
        {
            var existing = Tabs.FirstOrDefault(tab => tab.Title == "Variables");
            var wanted = VarTableSection != null && ShouldShowVariablesTab();

            if (wanted && existing == null)
            {
                var sourceTab = Tabs.FirstOrDefault(tab => ReferenceEquals(tab.Content, Source));
                var index = sourceTab == null ? Tabs.Count : Tabs.IndexOf(sourceTab);
                Tabs.Insert(index, new EditorTabViewModel("Variables", VarTableSection!));
            }
            else if (wanted && !ReferenceEquals(existing!.Content, VarTableSection))
                Tabs[Tabs.IndexOf(existing)] = new EditorTabViewModel("Variables", VarTableSection!);
            else if (!wanted && existing != null)
                Tabs.Remove(existing);

            NotifyTabsChanged();
        }

        private FieldViewModel CreateFieldViewModel(FieldDescriptor descriptor, LookupOptionProvider lookups)
        {
            return descriptor.Kind switch
            {
                EditorKind.Integer => new IntegerFieldViewModel(descriptor, _context),
                EditorKind.Float => new FloatFieldViewModel(descriptor, _context),
                EditorKind.Check => new CheckFieldViewModel(descriptor, _context),
                EditorKind.LocString => new LocStringFieldViewModel(descriptor, _context),
                EditorKind.TwoDaDropdown => new DropdownFieldViewModel(
                    descriptor, _context, lookups.GetOptions(descriptor.LookupKey)),
                EditorKind.ScriptSlot => new ScriptFieldViewModel(descriptor, _context),
                _ => new TextFieldViewModel(descriptor, _context)
            };
        }

        /// <summary>
        /// Every resource of one kind in the module, for a picker. Conversations merge NUI graphs
        /// with the small set of explicit legacy DLG exceptions.
        /// </summary>
        private IReadOnlyList<string> ResourceChoices(string? lookupKey)
        {
            var workspace = _resourceLister?.Invoke();
            if (workspace == null || !ResourceTypeExtensions.TryFromExtension(lookupKey, out var type))
                return Array.Empty<string>();

            var resRefs = type == ResourceType.Dlg
                ? workspace.EnumerateConversationGraphResRefs()
                    .Concat(workspace.EnumerateResRefs(ResourceType.Dlg))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
                : workspace.EnumerateResRefs(type);

            return resRefs.OrderBy(resRef => resRef, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private bool RunEdit(string description, Action mutation)
        {
            try
            {
                _session.Execute(description, mutation);

                AfterHistoryChange();
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Edit failed ({description}): {ex.Message}");
                return false;
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            await TrySaveAsync().ConfigureAwait(true);
        }

        /// <summary>Unwinds every unsaved edit - the placeable footer's Revert action.</summary>
        [RelayCommand(CanExecute = nameof(IsDirty))]
        private void Revert()
        {
            // Back to the saved version on disk, not back to position zero. Save leaves the
            // history intact, so unwinding the whole stack also unwound edits already committed.
            _session.RevertToSaved();

            RefreshAllFields(reclassifyAmbiguousBehavior: true);
            PlaceableSections?.Behavior.MarkSavedBaseline();
            AfterHistoryChange();
        }

        /// <summary>Saves this editor, returning false when the user cancels or the write fails.</summary>
        public async Task<bool> TrySaveAsync()
        {
            if (!IsDirty && PlaceableSections?.Behavior.NeedsSaveNormalization != true)
                return true;

            try
            {
                if (_session.HasExternalChange())
                {
                    var choice = await _prompts.ConfirmExternalChangeAsync(_session.FilePath).ConfigureAwait(true);
                    if (choice == ExternalChangeChoice.Cancel)
                        return false;

                    if (choice == ExternalChangeChoice.Reload)
                    {
                        _session.ReloadFromDisk();
                        RecreateVarTableSection();
                        RefreshAllFields(reclassifyAmbiguousBehavior: true);
                        PlaceableSections?.Behavior.MarkSavedBaseline();
                        AfterHistoryChange();
                        CatalogEntryChanged?.Invoke();
                        _log.AppendLine($"Reloaded externally changed file {_session.FilePath}.");
                        return true;
                    }

                    // The builder accepted the generation that is on disk now. Adopt that exact
                    // generation so the final check below rejects only a later external write.
                    _session.RecordCurrentFileState();
                }

                if (PlaceableSections != null &&
                    !PlaceableSections.Behavior.EnsureExpectedValuesForSave())
                {
                    return false;
                }

                var targetResRef =
                    _session.Document.Root.GetStringOrNull("TemplateResRef")?.Trim().ToLowerInvariant()
                    ?? string.Empty;
                if (!NwnResRef.IsCanonical(targetResRef))
                {
                    _log.AppendLine(
                        $"Cannot save {_resRef}: ResRef '{targetResRef}' must be " +
                        $"1-{NwnResRef.MaxLength} characters " +
                        "of a-z, 0-9, or underscore.");
                    return false;
                }

                if (!string.Equals(
                        _session.Document.Root.GetStringOrNull("TemplateResRef"),
                        targetResRef,
                        StringComparison.Ordinal))
                {
                    _session.Execute(
                        "Normalize ResRef",
                        () => _session.Document.Root.SetString(
                            "TemplateResRef",
                            GffFieldType.ResRef,
                            targetResRef));
                    RefreshAllFields();
                }

                var renaming = !string.Equals(
                    targetResRef,
                    _resRef,
                    StringComparison.OrdinalIgnoreCase);
                if (renaming && _saveCoordinator == null)
                {
                    _log.AppendLine(
                        $"Cannot rename {_resRef}: this editor has no blueprint save coordinator.");
                    return false;
                }

                var saveBytes = _session.ToBytes();
                var oldResRef = _resRef;
                var oldPath = _session.FilePath;
                var outcome = _saveCoordinator?.Save(
                    _session,
                    BlueprintType,
                    oldResRef,
                    targetResRef);
                if (outcome != null && !outcome.Saved)
                    return false;
                if (outcome == null && !Services.SaveService.TryWriteAtomicIfUnchanged(_session, saveBytes))
                {
                    _log.AppendLine(
                        $"Save stopped because {_session.FilePath} changed after the overwrite decision.");
                    return false;
                }

                if (outcome?.Renamed == true)
                {
                    _resRef = targetResRef;
                    Id = $"editor:{_session.FilePath}";
                    Source?.SetResRef(targetResRef);
                }

                _session.UndoStack.MarkSaved();
                _session.RecordCurrentFileState(_session.ToBytes());
                PlaceableSections?.Behavior.MarkSavedBaseline();
                AfterHistoryChange();
                CatalogEntryChanged?.Invoke();
                if (outcome?.Renamed == true)
                {
                    Renamed?.Invoke(this, oldResRef, oldPath);
                    _log.AppendLine(
                        $"Saved {oldPath} as {_session.FilePath} and updated " +
                        $"{outcome.UpdatedInstances} placed instance" +
                        $"{(outcome.UpdatedInstances == 1 ? string.Empty : "s")}.");
                }
                else
                {
                    _log.AppendLine($"Saved {_session.FilePath}.");
                }
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Save failed for {_session.FilePath}: {ex.Message}");
                return false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        public void Undo()
        {
            _session.Undo();
            RefreshAllFields();
            AfterHistoryChange();
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        public void Redo()
        {
            _session.Redo();
            RefreshAllFields();
            AfterHistoryChange();
        }

        public bool CanUndo => _session.UndoStack.CanUndo;

        public bool CanRedo => _session.UndoStack.CanRedo;

        /// <summary>Raised when the tab closes so the editor registry can forget this instance.</summary>
        public event Action<BlueprintEditorViewModel>? Closed;

        /// <summary>Raised after an async close prompt approves closing this tab.</summary>
        public event Action<BlueprintEditorViewModel>? CloseRequested;

        /// <summary>Suppresses a second tab-level prompt after the window-level discard decision.</summary>
        internal void ApproveApplicationClose() => _closeApproved = true;

        public override bool OnClose()
        {
            if (!_closeApproved && IsDirty)
            {
                if (!_closePromptOpen)
                {
                    _closePromptOpen = true;
                    _ = ConfirmCloseAsync();
                }

                return false;
            }

            if (_disposed)
                return base.OnClose();

            _disposed = true;
            PlaceableSections?.Appearance.Dispose();
            PlaceableSections?.Behavior.Dispose();
            _session.Dispose();
            Closed?.Invoke(this);
            return base.OnClose();
        }

        private async Task ConfirmCloseAsync()
        {
            try
            {
                var choice = await _prompts.ConfirmCloseAsync(Title ?? _resRef).ConfigureAwait(true);
                var approved = choice == UnsavedChangesChoice.Discard ||
                    choice == UnsavedChangesChoice.Save && await TrySaveAsync().ConfigureAwait(true);
                if (!approved)
                    return;

                _closeApproved = true;
                CloseRequested?.Invoke(this);
            }
            finally
            {
                _closePromptOpen = false;
            }
        }

        private void RecreateVarTableSection()
        {
            if (VarTableSection == null)
                return;

            VarTableSection = new VarTableSectionViewModel(
                _context.RunEdit, new VarTable(_session.Document.Root), _gameCodeIndex);
            OnPropertyChanged(nameof(VarTableSection));
            RebuildVariablesTab();
        }

        private void RefreshAllFields(bool reclassifyAmbiguousBehavior = false)
        {
            foreach (var group in Groups)
            foreach (var field in group.Fields)
                field.RefreshFromDocument();

            VarTableSection?.RefreshFromDocument();
            AppearanceGallery?.ReloadFromDocument();
            TintMapEditor?.Reload();

            if (PlaceableSections == null)
                return;

            // Undo can move a placeable back across a behavior switch, so the behavior section
            // re-detects rather than trusting what it last showed.
            PlaceableSections.Behavior.RefreshFromDocument(reclassifyAmbiguousBehavior);
            PlaceableSections.Appearance.RefreshFromDocument();
            RebuildVariablesTab();
        }

        /// <summary>Re-resolves custom-TLK watermarks after the shared table is regenerated.</summary>
        public void RefreshTlkLabels()
        {
            foreach (var field in Groups.SelectMany(group => group.Fields).OfType<LocStringFieldViewModel>())
                field.RefreshFromDocument();
            foreach (var field in Groups.SelectMany(group => group.Fields).OfType<DropdownFieldViewModel>())
                field.RefreshOptions(_lookups.GetOptions(field.Descriptor.LookupKey));
        }

        private void AfterHistoryChange()
        {
            TintMapEditor?.Reload();
            UpdateTitle();
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
            RevertCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(IsDirty));
            // The shell's Edit menu mirrors this tab's history, so it needs the change too.
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        private void UpdateTitle()
        {
            Title = IsDirty ? $"{_resRef} *" : _resRef;
        }

    }
}
