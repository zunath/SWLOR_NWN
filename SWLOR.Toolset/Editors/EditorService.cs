using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Schemas;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Shell;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// Opens blueprint editors as document tabs. One editor per file: requesting an already
    /// open blueprint activates its existing tab. Types without a schema yet log a notice
    /// instead of opening until a schema for that type is available.
    /// </summary>
    public sealed class EditorService
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly LookupOptionProvider _lookups;
        private readonly Domain.GameData.Tlk.TlkService? _tlkService;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly OutputLogService _log;
        private readonly ToolsetDockFactory _factory;
        private readonly IEditorPromptService _prompts;
        private readonly TilesetCatalog? _tilesetCatalog;
        private readonly TileModelCache? _tileModelCache;
        private readonly ResourceIndex? _resourceIndex;
        private readonly PlaceableAppearanceService? _placeableAppearances;

        /// <summary>Backs the creature Appearance grid; null degrades it to the schema field alone.</summary>
        private readonly AppearanceService? _appearances;
        private readonly DoorTypeService? _doorTypes;
        private readonly PortraitService? _portraits;
        private readonly WaypointAppearanceService? _waypointAppearances;
        private readonly Domain.GameData.TwoDa.TwoDaService? _twoDaService;
        private Behaviors.ChoicePreviewService? _choicePreviews;

        /// <summary>
        /// The picker option sets, shared by every editor rather than rebuilt per tab. Each one is a
        /// module scan or a 2DA read, and opening a second door used to redo all of them.
        /// </summary>
        private readonly Dictionary<string, IReadOnlyList<BehaviorChoice>> _choiceSets =
            new(StringComparer.Ordinal);

        /// <summary>
        /// The placeable behavior pickers' option sets. One provider for the session: the tag source
        /// alone offers five figures of options, and a per-editor copy multiplied both the scan and
        /// the memory by however many tabs were open.
        /// </summary>
        private Placeables.BehaviorValueSourceProvider? _behaviorValues;

        /// <summary>
        /// The creature appearance rows, projected once. appearance.2da is thousands of rows and
        /// every creature editor asks for the same list.
        /// </summary>
        private IReadOnlyList<Appearance.AppearanceOption>? _creatureAppearanceOptions;

        /// <summary>Backs the placeable Appearance tab's model grid; null degrades it to an empty grid.</summary>
        private readonly PlaceableModelCatalog? _placeableModels;

        /// <summary>Raised while a pack, validation, or Build All is running; handed to script tabs.</summary>
        private readonly Services.ModuleMutationLock? _mutationLock;
        private readonly ThumbnailService? _thumbnails;
        private readonly PlaceableIndexService? _placeableIndexes;
        private readonly Placeables.VfxPreviewService? _vfxPreviews;

        /// <summary>Supplies the area editor its placement-ghost geometry; null degrades the ghost to a marker.</summary>
        private readonly Workspace.BlueprintPreviewRenderer? _previewRenderer;

        /// <summary>Shared engine-symbol database driving script completion; null disables it.</summary>
        private readonly Workspace.ScriptLanguageService? _scriptLanguage;

        /// <summary>Where script diagnostics land; null in a shell without the panel.</summary>
        private readonly Shell.Panels.ProblemsViewModel? _problems;

        /// <summary>Rebuilds a script's .ncs on save; null when no compiler is vendored.</summary>
        private readonly Services.ScriptCompileService? _compileService;

        /// <summary>
        /// Built in the background on first use, then invalidated when a scripted resource changes.
        /// Scanning all script slots is expensive; builders who never request usages still pay
        /// nothing at startup.
        /// </summary>
        private readonly ScriptUsageIndexCache _scriptUsageIndex;
        private readonly TileWalkmeshCache? _tileWalkmeshCache;
        private readonly Dictionary<string, BlueprintEditorViewModel> _openEditors = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AreaEditorViewModel> _openAreaEditors = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Triggers.TriggerDocumentViewModel> _openTriggerEditors = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Waypoints.WaypointDocumentViewModel> _openWaypointEditors = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _openingWaypointEditors = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Doors.DoorDocumentViewModel> _openDoorEditors = new(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyList<Domain.Editors.Doors.DoorAppearanceChoice>? _doorAppearances;
        private readonly Dictionary<string, Sounds.SoundDocumentViewModel> _openSoundEditors = new(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyList<string>? _soundResources;

        // Keyed by path like the blueprint map rather than by resref like the area map: a script is
        // one file, so the path is its identity and there is no are/git/gic triplet to name.
        private readonly Dictionary<string, ScriptEditorViewModel> _openScriptEditors = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ConversationEditorViewModel> _openConversations = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The conversation snippet registry, read once from the game code. Built lazily because
        /// reflecting over every <c>ISnippetListDefinition</c> is wasted work in a session that
        /// never opens a dialog.
        /// </summary>
        private SnippetCatalog? _snippets;

        public EditorService(
            WorkspaceContext workspaceContext,
            LookupOptionProvider lookups,
            OutputLogService log,
            ToolsetDockFactory factory,
            IEditorPromptService prompts,
            IGameCodeIndex? gameCodeIndex = null,
            TilesetCatalog? tilesetCatalog = null,
            TileModelCache? tileModelCache = null,
            ResourceIndex? resourceIndex = null,
            PlaceableAppearanceService? placeableAppearances = null,
            DoorTypeService? doorTypes = null,
            TileWalkmeshCache? tileWalkmeshCache = null,
            Domain.GameData.Tlk.TlkService? tlkService = null,
            WaypointAppearanceService? waypointAppearances = null,
            Workspace.BlueprintPreviewRenderer? previewRenderer = null,
            Workspace.ScriptLanguageService? scriptLanguage = null,
            Shell.Panels.ProblemsViewModel? problems = null,
            Services.ScriptCompileService? compileService = null,
            PlaceableModelCatalog? placeableModels = null,
            ThumbnailService? thumbnails = null,
            PlaceableIndexService? placeableIndexes = null,
            Domain.GameData.TwoDa.TwoDaService? twoDaService = null,
            Placeables.VfxPreviewService? vfxPreviews = null,
            PortraitService? portraits = null,
            AppearanceService? appearances = null,
            Services.ModuleMutationLock? mutationLock = null)
        {
            _mutationLock = mutationLock;
            _placeableModels = placeableModels;
            _thumbnails = thumbnails;
            _placeableIndexes = placeableIndexes;
            _vfxPreviews = vfxPreviews;
            _appearances = appearances;
            _workspaceContext = workspaceContext;
            _lookups = lookups;
            _log = log;
            _factory = factory;
            _prompts = prompts;
            _gameCodeIndex = gameCodeIndex;
            _tilesetCatalog = tilesetCatalog;
            _tileModelCache = tileModelCache;
            _resourceIndex = resourceIndex;
            _placeableAppearances = placeableAppearances;
            _doorTypes = doorTypes;
            _portraits = portraits;
            _tileWalkmeshCache = tileWalkmeshCache;
            _tlkService = tlkService;
            _waypointAppearances = waypointAppearances;
            _previewRenderer = previewRenderer;
            _twoDaService = twoDaService;
            _scriptLanguage = scriptLanguage;
            _problems = problems;
            _compileService = compileService;
            _scriptUsageIndex = new ScriptUsageIndexCache(() =>
            {
                var workspace = _workspaceContext.Workspace;
                if (workspace == null)
                    return (Domain.Script.ScriptUsageIndex?)null;

                try
                {
                    return Domain.Script.ScriptUsageIndex.Build(workspace);
                }
                catch (Exception ex)
                {
                    _log.AppendLine($"Could not index script usages: {ex.Message}");
                    return null;
                }
            });
            _workspaceContext.ScriptUsagesInvalidated += _scriptUsageIndex.Invalidate;

            // Opening another module invalidates every module-derived picker; saving a blueprint
            // invalidates only the ones built out of the module's own content.
            _workspaceContext.WorkspaceOpened += () =>
            {
                _choiceSets.Clear();
                _doorAppearances = null;
                _behaviorValues?.InvalidateModuleSources();
            };
            _workspaceContext.CatalogEntryRefreshed += (_, _) =>
                _behaviorValues?.InvalidateModuleSources();
        }

        /// <summary>
        /// Focuses the script named by a Problems row and puts the caret on the reported line.
        /// </summary>
        public void NavigateToScriptLine(string resRef, int line)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            TryOpenEditor(ResourceType.Nss, resRef);

            if (_openScriptEditors.TryGetValue(workspace.GetResourcePath(ResourceType.Nss, resRef), out var editor))
                Avalonia.Threading.Dispatcher.UIThread.Post(() => editor.GoToLineRequested?.Invoke(line));
        }

        /// <summary>Compiles a script by resref, for callers outside an open tab (the explorer).</summary>
        /// <remarks>
        /// An open tab with unsaved edits is routed through its own compile path, which saves first.
        /// The compiler reads the file from disk, so without this the explorer's Compile produced
        /// bytecode for source the builder had already replaced on screen - and for a dirty include
        /// it rebuilt every dependent entry point from the old version, which is a far larger wrong
        /// answer than one stale script.
        /// </remarks>
        public async Task CompileScriptAsync(string resRef)
        {
            if (_compileService == null || !_compileService.IsAvailable)
            {
                _log.AppendLine("Cannot compile: nwn_script_comp.exe is missing from tools/SWLOR.CLI.");
                return;
            }

            var workspace = _workspaceContext.Workspace;
            if (workspace != null &&
                _openScriptEditors.TryGetValue(
                    workspace.GetResourcePath(ResourceType.Nss, resRef), out var open))
            {
                await open.CompileCommand.ExecuteAsync(null).ConfigureAwait(true);
                return;
            }

            var outcome = await _compileService.CompileAsync(resRef).ConfigureAwait(true);
            if (!outcome.Succeeded)
                _factory.ShowProblems();
        }

        /// <summary>Backs the script slots on one editor, describing its owner for the picker title.</summary>
        private ScriptSlotHost CreateScriptSlotHost(string ownerDescription) =>
            new(
                _workspaceContext,
                () => this,
                _log,
                ownerDescription,
                _scriptUsageIndex.GetAsync,
                CreateNewScriptAsync);

        /// <summary>
        /// Creates an NSS resource for the script picker's "New script..." action and returns the
        /// resref the picker should place in the slot.
        /// </summary>
        private async Task<string?> CreateNewScriptAsync()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return null;

            var name = await _prompts.PromptForTextAsync(
                "New script",
                "Name for the new script. Its resref is derived from this.",
                string.Empty,
                "Create").ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var resRef = ModuleResourceTemplateFactory.ToResRef(name);
            if (resRef.Length == 0)
            {
                _log.AppendLine("Could not create script: that name has no letters or digits.");
                return null;
            }

            var path = workspace.GetResourcePath(ResourceType.Nss, resRef);
            if (File.Exists(path))
            {
                _log.AppendLine($"Could not create script: '{resRef}' already exists.");
                return null;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                SaveService.WriteNewAtomic(
                    path,
                    ModuleResourceTemplateFactory.CreateFileContent(ResourceType.Nss, resRef, name.Trim()));
                _workspaceContext.RefreshCatalogEntry(ResourceType.Nss, resRef);
                _log.AppendLine($"Created script '{resRef}' from the script-slot picker.");
                return resRef;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not create script '{resRef}': {ex.Message}");
                return null;
            }
        }

        /// <summary>Opens a NWScript source file as a text editor tab, or activates its open tab.</summary>
        private void OpenScriptEditor(ModuleWorkspace workspace, string resRef)
        {
            var filePath = workspace.GetResourcePath(ResourceType.Nss, resRef);
            if (!File.Exists(filePath))
            {
                _log.AppendLine($"File not found: {filePath}");
                return;
            }

            if (_openScriptEditors.TryGetValue(filePath, out var existing))
            {
                _factory.ActivateDocument(existing);
                return;
            }

            try
            {
                var editor = new ScriptEditorViewModel(filePath, resRef, _log, _prompts, _scriptLanguage)
                {
                    // The tab's own Compile button writes a .ncs, so it follows the same module-wide
                    // lock that the Build menu does.
                    MutationLock = _mutationLock
                };
                editor.Closed += _ =>
                {
                    _openScriptEditors.Remove(filePath);
                    // A closed tab's findings must not linger in Problems - they refer to a buffer
                    // nobody can see or navigate to any more.
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => _problems?.Clear(resRef));
                };
                editor.CloseRequested += _ => _factory.CloseDocument(editor);
                editor.CompileOnSave = _compileService != null && _compileService.IsAvailable
                    ? name => CompileOnSaveAsync(workspace, editor, name)
                    : null;

                // Compile belongs to the document, not to a module-wide menu. The tab owns the button
                // and Ctrl+B; this just gives it the service.
                editor.CompileRequested = _compileService != null && _compileService.IsAvailable
                    ? async name => (await _compileService.CompileAsync(name).ConfigureAwait(true)).Succeeded
                    : null;
                editor.ShowProblemsRequested = () => _factory.ShowProblems();
                editor.FindUsages = async name =>
                {
                    var index = await _scriptUsageIndex.GetAsync().ConfigureAwait(true);
                    return index?.UsagesOf(name) ?? Array.Empty<Domain.Script.ScriptUsage>();
                };
                editor.DiagnosticsChanged += diagnostics =>
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        _problems?.SetDiagnostics(resRef, Domain.Script.Syntax.ScriptDiagnosticSource.Editor, diagnostics));

                // Go-to-definition across an include opens that script and lands on the symbol.
                editor.OpenIncludeRequested += (includeResRef, offset) =>
                {
                    OpenScriptEditor(workspace, includeResRef);
                    if (_openScriptEditors.TryGetValue(
                            workspace.GetResourcePath(ResourceType.Nss, includeResRef), out var opened))
                    {
                        // Posted so the tab's view has attached and wired GoToOffsetRequested first.
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            opened.GoToOffsetRequested?.Invoke(offset));
                    }
                };
                _openScriptEditors[filePath] = editor;
                _factory.OpenDocument(editor);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not open {filePath}: {ex.Message}");
            }
        }

        public void TryOpenEditor(ResourceType type, string resRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            if (type == ResourceType.Area)
            {
                OpenAreaEditor(workspace, resRef);
                return;
            }

            if (type == ResourceType.Nss)
            {
                OpenScriptEditor(workspace, resRef);
                return;
            }

            if (type == ResourceType.Dlg)
            {
                OpenConversationEditor(workspace, resRef);
                return;
            }

            var schema = GetSchema(type);
            if (schema == null)
            {
                _log.AppendLine($"No editor available yet for {type.DisplayName()}.");
                return;
            }

            var filePath = workspace.GetResourcePath(type, resRef);
            if (!File.Exists(filePath))
            {
                _log.AppendLine($"File not found: {filePath}");
                return;
            }

            if (_openEditors.TryGetValue(filePath, out var existing))
            {
                _factory.ActivateDocument(existing);
                return;
            }

            if (_openTriggerEditors.TryGetValue(filePath, out var existingTrigger))
            {
                _factory.ActivateDocument(existingTrigger);
                return;
            }

            if (_openWaypointEditors.TryGetValue(filePath, out var existingWaypoint))
            {
                _factory.ActivateDocument(existingWaypoint);
                return;
            }

            if (_openDoorEditors.TryGetValue(filePath, out var existingDoor))
            {
                _factory.ActivateDocument(existingDoor);
                return;
            }

            if (_openSoundEditors.TryGetValue(filePath, out var existingSound))
            {
                _factory.ActivateDocument(existingSound);
                return;
            }

            try
            {
                if (!CanRepresentEveryValue(filePath, resRef, schema))
                    return;

                // Doors use the behavior editor for both blueprints and area placements.
                if (type == ResourceType.Utd)
                {
                    OpenDoorEditor(filePath, resRef);
                    return;
                }

                // Sounds use the behavior editor and its dedicated ordered Sounds-list control.
                if (type == ResourceType.Uts)
                {
                    OpenSoundEditor(filePath, resRef);
                    return;
                }

                // Triggers get the behavior editor rather than the generic schema form: what a
                // trigger is for drives which fields it even has.
                if (type == ResourceType.Utt)
                {
                    OpenTriggerEditor(filePath, resRef);
                    return;
                }

                if (type == ResourceType.Utw)
                {
                    OpenWaypointEditor(filePath, resRef);
                    return;
                }

                var editor = new BlueprintEditorViewModel(
                    filePath, resRef, type, schema, _lookups, _gameCodeIndex, _log, _prompts,
                    // So a localized field that carries a strref but no language-0 override can show what
                    // that strref says, instead of a blank the builder reads as missing data.
                    _tlkService == null ? null : _tlkService.GetString,
                    CreateScriptSlotHost($"{type.SingularDisplayName()} '{resRef}'"),
                    type == ResourceType.Utp ? CreatePlaceableSections : null,
                    () => _workspaceContext.Workspace,
                    type == ResourceType.Utc ? CreateCreatureAppearanceGallery : null);
                editor.Closed += _ => _openEditors.Remove(filePath);
                editor.CloseRequested += _ => _factory.CloseDocument(editor);
                editor.CatalogEntryChanged += () =>
                    _workspaceContext.RefreshCatalogEntry(editor.BlueprintType, resRef);
                _openEditors[filePath] = editor;
                _factory.OpenDocument(editor);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Failed to open editor for {resRef}: {ex.Message}");
            }
        }

        private async Task<bool> CompileOnSaveAsync(
            ModuleWorkspace workspace,
            ScriptEditorViewModel editor,
            string resRef)
        {
            if (_compileService == null)
                return false;

            var outcome = await _compileService.CompileAsync(resRef).ConfigureAwait(false);

            // Saving an include compile-checks it and rebuilds every transitive entry point, inside
            // CompileAsync. This used to then offer the builder a second, identical build - which
            // for a widely included header is a full compilation pass repeated for nothing, and
            // which read as "your dependents are still stale" when they were not. Said, not offered.
            if (outcome.RebuiltDependents > 0)
            {
                var count = outcome.RebuiltDependents;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    editor.ReportDependentRebuild(count));
            }

            return outcome.Succeeded;
        }

        /// <summary>
        /// Builds the placeable's Appearance and Behavior tabs. The module-wide scans they validate
        /// against are kicked off here rather than at startup, so a session that never opens a
        /// placeable never pays for them.
        /// </summary>
        private Placeables.PlaceableEditorSections? CreatePlaceableSections(
            EditorFieldContext context,
            Func<string, Action, bool> runEdit,
            IScriptSlotHost? scriptSlotHost,
            Func<string?, IReadOnlyList<string>> resourceChoices)
        {
            // No 2DA layer means no model grid, so the placeable opens with the plain tabs rather
            // than an Appearance tab that could only ever be empty.
            if (_placeableModels == null)
                return null;

            _placeableIndexes?.EnsureBuilt();

            var values = _behaviorValues ??= new Placeables.BehaviorValueSourceProvider(
                _gameCodeIndex,
                () => _placeableIndexes?.Tags,
                BlueprintChoices,
                _thumbnails,
                _vfxPreviews);

            var appearance = new Placeables.AppearanceSectionViewModel(
                context,
                _placeableModels,
                _thumbnails,
                () => _placeableIndexes?.Usage ?? Domain.Workspace.PlaceableAppearanceUsageIndex.Empty,
                runEdit,
                _resourceIndex,
                // The same resource/cache layer the area viewport builds through, with the preview's
                // transform tracks and emitter metadata added in its separate one-model cache.
                modelResRef => _tileModelCache?.GetOrBuildPlaceablePreview(modelResRef));

            var behavior = new Placeables.PlaceableBehaviorSectionViewModel(
                context, values, _prompts, runEdit, scriptSlotHost, resourceChoices);

            // The scan kicked off just above finishes after these fields are built, so the first
            // placeable opened in a session built every tag field against an empty list - which is
            // what makes a choice field degrade to plain free text with no suggestions and no
            // resolution check. Nothing put the real options back, so a Teleporter destination
            // stayed bare until the tab was closed and reopened.
            if (_placeableIndexes != null)
            {
                var indexes = _placeableIndexes;
                void OnIndexUpdated()
                {
                    appearance.RefreshUsage();
                    values.InvalidateModuleSources();
                    behavior.RefreshChoiceSources();
                }

                indexes.Updated += OnIndexUpdated;
                behavior.Detach = () => indexes.Updated -= OnIndexUpdated;
            }

            return new Placeables.PlaceableEditorSections(appearance, behavior);
        }

        /// <summary>
        /// The creature editor's appearance grid. Null without the 2DA layer, which leaves the
        /// schema's own appearance field as the only way to set it - the same degradation every
        /// other game-data-backed control makes.
        /// </summary>
        private Appearance.AppearanceGallerySectionViewModel? CreateCreatureAppearanceGallery(
            EditorFieldContext context,
            Func<string, Action, bool> runEdit)
        {
            if (_appearances == null)
                return null;

            var options = _creatureAppearanceOptions ??= _appearances.GetAll()
                .Select(row => new Appearance.AppearanceOption(
                    row.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    row.DisplayName,
                    // The label rather than the model column: half of appearance.2da names a
                    // phenotype there, and "H" tells a builder nothing about what they picked.
                    $"row {row.Id} \u00b7 {row.Label}",
                    CreatureAppearanceId: row.Id))
                .ToList();

            return new Appearance.AppearanceGallerySectionViewModel(
                options,
                _thumbnails,
                () => (context.Document.Root.GetOrNull("Appearance_Type")?.GetInteger() ?? 0)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture),
                option => runEdit(
                    $"Change appearance to {option.Caption}",
                    () => WriteCreatureAppearance(context, option)),
                noun: "appearance");
        }

        private static void WriteCreatureAppearance(
            EditorFieldContext context,
            Appearance.AppearanceOption option)
        {
            var id = option.CreatureAppearanceId ?? 0;
            var field = context.Document.Root.GetOrNull("Appearance_Type");
            if (field == null)
            {
                var raw = System.Text.Encoding.ASCII.GetBytes(
                    id.ToString(System.Globalization.CultureInfo.InvariantCulture));
                context.Document.Root.Add(
                    "Appearance_Type",
                    Domain.Gff.JsonGffField.CreateScalar(Domain.Gff.GffFieldType.Word, raw));
                return;
            }

            field.SetInteger(id);
        }

        private IReadOnlyList<CatalogEntry> BlueprintChoices(ResourceType type)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<CatalogEntry>();

            var catalog = _workspaceContext.Catalog;
            var choices = new List<CatalogEntry>();
            foreach (var resRef in workspace.EnumerateResRefs(type))
            {
                // The catalog is keyed by type and resref, so this is a lookup rather than a scan of
                // every indexed blueprint per requested type.
                choices.Add(catalog != null && catalog.TryGetEntry(type, resRef, out var entry)
                    ? entry
                    : new CatalogEntry(
                        type, resRef, null, null, workspace.GetResourcePath(type, resRef)));
            }

            return choices;
        }

        /// <summary>
        /// Whether <paramref name="resRef"/> is open in an editor right now.
        /// </summary>
        /// <remarks>
        /// Deleting a file out from under an open editor leaves that editor holding a live
        /// DocumentSession: the next save sees the missing file as an external change, and Overwrite
        /// recreates the blueprint that was supposedly deleted while Reload fails outright. Callers
        /// check this and refuse instead.
        /// </remarks>
        public bool IsOpen(ResourceType type, string resRef)
        {
            if (type == ResourceType.Area)
                return _openAreaEditors.ContainsKey(resRef);

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return false;

            var path = workspace.GetResourcePath(type, resRef);
            return _openEditors.ContainsKey(path)
                   || _openTriggerEditors.ContainsKey(path)
                   || _openWaypointEditors.ContainsKey(path)
                   || _openDoorEditors.ContainsKey(path)
                   || _openSoundEditors.ContainsKey(path)
                   || _openConversations.ContainsKey(path);
        }

        /// <summary>
        /// Saves every open editor. Returns false as soon as a save fails or the user cancels an
        /// external-change prompt, allowing validation and packing to abort safely.
        /// </summary>
        public async Task<bool> SaveAllAsync()
        {
            foreach (var editor in _openEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openTriggerEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openWaypointEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openDoorEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openSoundEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openAreaEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openConversations.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            return await SaveScriptsAsync().ConfigureAwait(true);
        }

        /// <summary>
        /// Saves every open script buffer. Explicit bulk compilation suppresses per-file
        /// compile-on-save so each entry point is written exactly once by the subsequent build.
        /// </summary>
        public async Task<bool> SaveScriptsAsync(bool compileOnSave = true)
        {
            foreach (var editor in _openScriptEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync(compileOnSave).ConfigureAwait(true))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Prompts before application shutdown when any editor is dirty. The caller may close the
        /// window only when this returns true; saving still honors each document's external-change
        /// check and cancels shutdown if any save is declined or fails.
        /// </summary>
        public async Task<bool> TryPrepareApplicationCloseAsync()
        {
            if (!_openEditors.Values.Any(editor => editor.IsDirty) &&
                !_openTriggerEditors.Values.Any(editor => editor.IsDirty) &&
                !_openWaypointEditors.Values.Any(editor => editor.IsDirty) &&
                !_openDoorEditors.Values.Any(editor => editor.IsDirty) &&
                !_openSoundEditors.Values.Any(editor => editor.IsDirty) &&
                !_openAreaEditors.Values.Any(editor => editor.IsDirty) &&
                !_openScriptEditors.Values.Any(editor => editor.IsDirty) &&
                !_openConversations.Values.Any(editor => editor.IsDirty))
                return true;

            var choice = await _prompts.ConfirmCloseAsync("all open editors").ConfigureAwait(true);
            if (choice == UnsavedChangesChoice.Save)
                return await SaveAllAsync().ConfigureAwait(true);

            if (choice != UnsavedChangesChoice.Discard)
                return false;

            foreach (var editor in _openEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openTriggerEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openWaypointEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openDoorEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openSoundEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openAreaEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openScriptEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openConversations.Values)
                editor.ApproveApplicationClose();

            return true;
        }

        /// <summary>Trigger blueprints open in the behavior editor, as a document tab.</summary>
        private void OpenTriggerEditor(string filePath, string resRef)
        {
            var editor = new Triggers.TriggerDocumentViewModel(
                filePath, resRef, _gameCodeIndex, _log, _prompts, ResolveTagArea, ResolveTriggerChoices,
                ChoicePreviews());
            editor.Closed += _ => _openTriggerEditors.Remove(filePath);
            editor.CloseRequested += _ => _factory.CloseDocument(editor);
            editor.CatalogEntryChanged += () =>
                _workspaceContext.RefreshCatalogEntry(ResourceType.Utt, resRef);
            _openTriggerEditors[filePath] = editor;
            _factory.OpenDocument(editor);
        }

        /// <summary>Waypoint blueprints open in the behavior editor, as a document tab.</summary>
        private async void OpenWaypointEditor(string filePath, string resRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || !_openingWaypointEditors.Add(filePath))
                return;

            try
            {
                // The transition classifier needs a module-wide GIT scan. Await its background
                // warm-up instead of parsing hundreds of area files on Avalonia's UI thread.
                var transitionDestinationTags =
                    await workspace.TagIndex.GetTransitionDestinationTagsAsync().ConfigureAwait(true);
                if (!ReferenceEquals(workspace, _workspaceContext.Workspace))
                    return;

                if (_openWaypointEditors.TryGetValue(filePath, out var existing))
                {
                    _factory.ActivateDocument(existing);
                    return;
                }

                var catalog = new Domain.Editors.Waypoints.WaypointBehaviorCatalog(
                    _gameCodeIndex,
                    transitionDestinationTags);
                var editor = new Waypoints.WaypointDocumentViewModel(
                    filePath,
                    resRef,
                    _gameCodeIndex,
                    _log,
                    _prompts,
                    catalog,
                    ResolveWaypointChoices);
                editor.Closed += _ => _openWaypointEditors.Remove(filePath);
                editor.CloseRequested += _ => _factory.CloseDocument(editor);
                editor.CatalogEntryChanged += () =>
                    _workspaceContext.RefreshCatalogEntry(ResourceType.Utw, resRef);
                _openWaypointEditors[filePath] = editor;
                _factory.OpenDocument(editor);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Failed to open waypoint editor for {resRef}: {ex.Message}");
            }
            finally
            {
                _openingWaypointEditors.Remove(filePath);
            }
        }

        /// <summary>Door blueprints open in the same behavior editor used by door placements.</summary>
        private void OpenDoorEditor(string filePath, string resRef)
        {
            var editor = new Doors.DoorDocumentViewModel(
                filePath,
                resRef,
                _gameCodeIndex,
                _log,
                _prompts,
                ResolveDoorTag,
                ResolveDoorChoices,
                DoorAppearances(),
                _resourceIndex,
                _previewRenderer != null
                    ? door => _previewRenderer.BuildModel(ResourceType.Utd, door)
                    : null,
                _thumbnails,
                ChoicePreviews());
            editor.Closed += _ => _openDoorEditors.Remove(filePath);
            editor.CloseRequested += _ => _factory.CloseDocument(editor);
            editor.CatalogEntryChanged += () =>
                _workspaceContext.RefreshCatalogEntry(ResourceType.Utd, resRef);
            _openDoorEditors[filePath] = editor;
            _factory.OpenDocument(editor);
        }

        private IReadOnlyList<Domain.Editors.Doors.DoorAppearanceChoice> DoorAppearances() =>
            _doorAppearances ??= Domain.Editors.Doors.DoorAppearanceCatalog.Read(_doorTypes);

        private Behaviors.ChoicePreviewService ChoicePreviews() =>
            _choicePreviews ??= new Behaviors.ChoicePreviewService(_resourceIndex);

        /// <summary>
        /// One option set, built on first use and kept. Every one of these is a 2DA read, a palette
        /// file parse, or a module scan; before this, opening a second door of the same kind redid
        /// all of them.
        /// </summary>
        private IReadOnlyList<BehaviorChoice> Cached(
            string scope,
            string key,
            Func<string, IReadOnlyList<BehaviorChoice>> build)
        {
            var cacheKey = scope + ":" + key;
            if (_choiceSets.TryGetValue(cacheKey, out var cached))
                return cached;

            var built = build(key);
            _choiceSets[cacheKey] = built;
            return built;
        }

        private IReadOnlyList<BehaviorChoice> ResolveDoorChoices(string key) =>
            Cached("door", key, BuildDoorChoices);

        private IReadOnlyList<Domain.Editors.Behaviors.BehaviorChoice> BuildDoorChoices(string key)
        {
            if (key == Domain.Editors.Doors.DoorChoiceKeys.DoorPaletteCategories)
                return ResolveDoorCategories();

            if (key == Domain.Editors.Doors.DoorChoiceKeys.LoadScreens)
                return Domain.Editors.Triggers.LoadScreenCatalog.Read(_twoDaService);

            if (key == Domain.Editors.Doors.DoorChoiceKeys.TrapTypes)
                return Domain.Editors.Triggers.TrapTypeCatalog.Read(_twoDaService);

            if (key == Domain.Editors.Doors.DoorChoiceKeys.Portraits && _portraits != null)
            {
                return _portraits.GetAll()
                    .Select(row => new Domain.Editors.Behaviors.BehaviorChoice(
                        row.Id,
                        row.BaseResRef,
                        PortraitService.GetTgaVariants(row.BaseResRef).Medium))
                    .ToList();
            }

            return _lookups.GetOptions(key)
                .Select(option => new Domain.Editors.Behaviors.BehaviorChoice(option.Id, option.Display))
                .ToList();
        }

        private IReadOnlyList<Domain.Editors.Behaviors.BehaviorChoice> ResolveDoorCategories()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();

            try
            {
                var path = Path.Combine(workspace.ModuleRoot, "itp", "doorpalcus.itp.json");
                if (!File.Exists(path))
                    return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();

                return Domain.Editors.Behaviors.PaletteCategoryReader.Read(
                    Domain.Documents.ItpDocument.Load(path),
                    _tlkService != null ? _tlkService.GetString : null);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read the door palette categories: {ex.Message}");
                return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();
            }
        }

        private string? ResolveDoorTag(Domain.Editors.Behaviors.BehaviorTagScope scope, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return null;

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return null;

            if (scope == Domain.Editors.Behaviors.BehaviorTagScope.Item)
            {
                var itemResRef = workspace.TagIndex.FindItemBlueprintDefiningTag(tag);
                return itemResRef == null ? null : $"item blueprint {itemResRef}";
            }

            if (scope == Domain.Editors.Behaviors.BehaviorTagScope.Waypoint)
            {
                var area = workspace.TagIndex.FindAreaDefiningTag(tag, ResourceType.Utw);
                return area == null ? null : $"waypoint in {area}";
            }

            if (scope == Domain.Editors.Behaviors.BehaviorTagScope.Door)
            {
                var area = workspace.TagIndex.FindAreaDefiningTag(tag, ResourceType.Utd);
                return area == null ? null : $"door in {area}";
            }

            var doorArea = workspace.TagIndex.FindAreaDefiningTag(tag, ResourceType.Utd);
            if (doorArea != null)
                return $"door in {doorArea}";

            var waypointArea = workspace.TagIndex.FindAreaDefiningTag(tag, ResourceType.Utw);
            return waypointArea == null ? null : $"waypoint in {waypointArea}";
        }

        /// <summary>Ambient-sound blueprints open in the behavior editor.</summary>
        private void OpenSoundEditor(string filePath, string resRef)
        {
            var editor = new Sounds.SoundDocumentViewModel(
                filePath,
                resRef,
                _gameCodeIndex,
                _log,
                _prompts,
                ResolveSoundChoices,
                SoundResources());
            editor.Closed += _ => _openSoundEditors.Remove(filePath);
            editor.CloseRequested += _ => _factory.CloseDocument(editor);
            editor.CatalogEntryChanged += () =>
                _workspaceContext.RefreshCatalogEntry(ResourceType.Uts, resRef);
            _openSoundEditors[filePath] = editor;
            _factory.OpenDocument(editor);
        }

        private IReadOnlyList<string> SoundResources() =>
            _soundResources ??= Domain.Editors.Sounds.SoundResourceCatalog.Read(_resourceIndex);

        private IReadOnlyList<BehaviorChoice> ResolveSoundChoices(string key) =>
            Cached("sound", key, BuildSoundChoices);

        private IReadOnlyList<BehaviorChoice> BuildSoundChoices(string key)
        {
            if (key != Domain.Editors.Sounds.SoundChoiceKeys.PaletteCategories)
                return Array.Empty<BehaviorChoice>();

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<BehaviorChoice>();

            try
            {
                var path = Path.Combine(workspace.ModuleRoot, "itp", "soundpalcus.itp.json");
                if (!File.Exists(path))
                    return Array.Empty<BehaviorChoice>();

                return Domain.Editors.Sounds.SoundPaletteCategoryReader.Read(
                    Domain.Documents.ItpDocument.Load(path),
                    _tlkService != null ? _tlkService.GetString : null);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read the sound palette categories: {ex.Message}");
                return Array.Empty<BehaviorChoice>();
            }
        }

        /// <summary>
        /// Game-data choice sets a trigger row asks for. Most forward to the shared lookup provider,
        /// whose keys these deliberately match; the palette categories come from the module's own
        /// .itp, which no 2DA lookup covers.
        /// </summary>
        private IReadOnlyList<BehaviorChoice> ResolveTriggerChoices(string key) =>
            Cached("trigger", key, BuildTriggerChoices);

        private IReadOnlyList<Domain.Editors.Behaviors.BehaviorChoice> BuildTriggerChoices(string key)
        {
            if (key == Domain.Editors.Triggers.TriggerChoiceKeys.PaletteCategories)
                return ResolveTriggerCategories();

            // Not routed through the shared lookup: the picker shows each screen's artwork, and the
            // generic 2DA lookup returns labels only.
            if (key == Domain.Editors.Triggers.TriggerChoiceKeys.LoadScreens)
                return Domain.Editors.Triggers.LoadScreenCatalog.Read(_twoDaService);

            if (key == Domain.Editors.Triggers.TriggerChoiceKeys.TrapTypes)
                return Domain.Editors.Triggers.TrapTypeCatalog.Read(_twoDaService);

            return _lookups.GetOptions(key)
                .Select(option => new Domain.Editors.Behaviors.BehaviorChoice(option.Id, option.Display))
                .ToList();
        }

        /// <summary>
        /// The trigger palette's categories, named rather than numbered. A missing or unreadable
        /// palette yields an empty list, which shows as an empty picker instead of a bare id.
        /// </summary>
        private IReadOnlyList<Domain.Editors.Behaviors.BehaviorChoice> ResolveTriggerCategories()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();

            try
            {
                var path = Path.Combine(workspace.ModuleRoot, "itp", "triggerpalcus.itp.json");
                if (!File.Exists(path))
                    return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();

                return Domain.Editors.Behaviors.PaletteCategoryReader.Read(
                    Domain.Documents.ItpDocument.Load(path),
                    _tlkService != null ? _tlkService.GetString : null);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read the trigger palette categories: {ex.Message}");
                return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();
            }
        }

        private IReadOnlyList<BehaviorChoice> ResolveWaypointChoices(string key) =>
            Cached("waypoint", key, BuildWaypointChoices);

        private IReadOnlyList<Domain.Editors.Behaviors.BehaviorChoice> BuildWaypointChoices(string key)
        {
            if (key == Domain.Editors.Waypoints.WaypointChoiceKeys.PaletteCategories)
                return ResolveWaypointCategories();

            if (key == Domain.Editors.Waypoints.WaypointChoiceKeys.Appearances)
                return _waypointAppearances == null
                    ? Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>()
                    : _waypointAppearances.GetAll()
                    .Select(row => new Domain.Editors.Behaviors.BehaviorChoice(row.Id, row.DisplayName))
                    .ToList();

            return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();
        }

        private IReadOnlyList<Domain.Editors.Behaviors.BehaviorChoice> ResolveWaypointCategories()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();

            try
            {
                var path = Path.Combine(workspace.ModuleRoot, "itp", "waypointpalcus.itp.json");
                if (!File.Exists(path))
                    return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();

                return Domain.Editors.Behaviors.PaletteCategoryReader.Read(
                    Domain.Documents.ItpDocument.Load(path),
                    _tlkService != null ? _tlkService.GetString : null);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read the waypoint palette categories: {ex.Message}");
                return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();
            }
        }

        /// <summary>
        /// Names the area a waypoint or door tag lives in, or null when nothing defines it — what
        /// puts a tick or a cross beside a transition's destination.
        /// </summary>
        private string? ResolveTagArea(string tag) =>
            _workspaceContext.Workspace == null || string.IsNullOrWhiteSpace(tag)
                ? null
                : _workspaceContext.Workspace.TagIndex.FindAreaDefiningTag(tag);
        /// <summary>
        /// Conversations open in the Play-it editor. The 255 <c>dialogN</c> shells are refused: they
        /// are generated for the C# <c>Dialog</c> service's runtime menus and editing one by hand
        /// would be edited back over on the next generation.
        /// </summary>
        private void OpenConversationEditor(Domain.Workspace.ModuleWorkspace workspace, string resRef)
        {
            if (Domain.Validation.UnreferencedConversationRule.IsGeneratedShell(resRef))
            {
                _log.AppendLine(
                    $"'{resRef}' is one of the 255 conversation shells the C# Dialog service generates, "
                    + "not hand-authored content. Open the C# dialog class instead.");
                return;
            }

            var filePath = workspace.GetResourcePath(ResourceType.Dlg, resRef);
            if (!File.Exists(filePath))
            {
                _log.AppendLine($"File not found: {filePath}");
                return;
            }

            if (_openConversations.TryGetValue(filePath, out var existing))
            {
                _factory.ActivateDocument(existing);
                return;
            }

            try
            {
                // Refused rather than half-shown. See ConversationCompatibility for why that is the
                // safer failure; the file is left untouched either way.
                var support = Domain.Conversations.ConversationCompatibility.Check(
                    Domain.Documents.DlgDocument.Load(filePath));
                if (!support.IsSupported)
                {
                    _log.AppendLine($"{resRef}: {support.Reason}");
                    Shell.Views.ErrorDialog.Show($"Cannot open '{resRef}'", support.Reason, null);
                    return;
                }

                _snippets ??= SnippetCatalog.Build();
                var editor = new ConversationEditorViewModel(
                    filePath, resRef, _snippets, _gameCodeIndex, _log, _prompts,
                    extension => TagsFor(extension));
                editor.Closed += _ => _openConversations.Remove(filePath);
                editor.CloseRequested += _ => _factory.CloseDocument(editor);
                editor.CatalogEntryChanged += () =>
                    _workspaceContext.RefreshCatalogEntry(ResourceType.Dlg, resRef);
                _openConversations[filePath] = editor;
                _factory.OpenDocument(editor);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Failed to open conversation {resRef}: {ex.Message}");
            }
        }

        /// <summary>
        /// Tags of placed objects of one kind, so a store or waypoint argument names something the
        /// runtime can actually find. Instance overrides win; blank tags fall back to the blueprint.
        /// </summary>
        private IReadOnlyList<string> TagsFor(string extension)
        {
            if (!ResourceTypeExtensions.TryFromExtension(extension, out var type))
                return Array.Empty<string>();

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<string>();

            return workspace.TagIndex.TagsFor(type).ToList();
        }

        /// <summary>Areas open in the composite editor (.are properties + .git instance lists).</summary>
        private void OpenAreaEditor(Domain.Workspace.ModuleWorkspace workspace, string resRef)
        {
            if (_openAreaEditors.TryGetValue(resRef, out var existing))
            {
                _factory.ActivateDocument(existing);
                return;
            }

            var arePath = workspace.GetResourcePath(ResourceType.Area, resRef);
            var gitPath = Path.Combine(workspace.ModuleRoot, "git", resRef + ".git.json");
            if (!File.Exists(arePath) || !File.Exists(gitPath))
            {
                _log.AppendLine($"Area files not found for '{resRef}' (.are/.git pair required).");
                return;
            }

            try
            {
                var editor = new AreaEditorViewModel(
                    resRef, workspace, _lookups, _gameCodeIndex, _log,
                    _tilesetCatalog, _tileModelCache, _resourceIndex,
                    _placeableAppearances, _doorTypes, _tileWalkmeshCache, _prompts,
                    ResolveBlueprintName, TryOpenEditor,
                    _tlkService != null ? _tlkService.GetString : null, _waypointAppearances,
                    _previewRenderer != null
                        ? (type, blueprintResRef, useIndexed) =>
                            _previewRenderer.BuildModel(type, blueprintResRef, useIndexed)
                        : null,
                    CreateScriptSlotHost($"Area '{resRef}'"),
                    _previewRenderer != null
                        ? instance => _previewRenderer.BuildModel(ResourceType.Utc, instance)
                        : null,
                    new Doors.DoorEditorServices(
                        resRef,
                        ResolveDoorTag,
                        ResolveDoorChoices,
                        DoorAppearances(),
                        _resourceIndex,
                        _previewRenderer != null
                            ? door => _previewRenderer.BuildModel(ResourceType.Utd, door)
                            : null,
                        _thumbnails,
                        ChoicePreviews()),
                    ResolveSoundChoices,
                    SoundResources());
                editor.Closed += _ => _openAreaEditors.Remove(resRef);
                editor.TilesetChanged += () => _factory.NotifyActiveAreaChanged();
                editor.CloseRequested += _ => _factory.CloseDocument(editor);
                editor.CatalogEntryChanged += () =>
                    _workspaceContext.RefreshCatalogEntry(ResourceType.Area, resRef);
                _openAreaEditors[resRef] = editor;
                _factory.OpenDocument(editor);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Failed to open area editor for {resRef}: {ex.Message}");
            }
        }

        /// <summary>
        /// A blueprint's display name from the background catalog, or null while the catalog is still
        /// building or when the resref isn't indexed (hak-provided blueprints are not). Callers fall
        /// back to the resref, which is why this returns null rather than inventing a label.
        /// </summary>
        private string? ResolveBlueprintName(ResourceType? type, string? resRef)
        {
            if (type == null || string.IsNullOrWhiteSpace(resRef))
                return null;

            // A lookup, not a scan: this answers the area editor's selection bar, which asks on
            // every click, and the snapshot holds ~17,900 records.
            if (_workspaceContext.Catalog?.TryGetEntry(type.Value, resRef, out var entry) != true)
                return null;

            return string.IsNullOrWhiteSpace(entry!.Name) ? null : entry.Name;
        }

        /// <summary>
        /// Refuses to open a blueprint whose dropdown-backed fields hold values their lookup tables
        /// cannot represent, reporting exactly which ones instead.
        ///
        /// A combo box can only select values its lookup knows about, so an unknown id renders as a
        /// blank box - the stored value becomes invisible, and touching that field would overwrite
        /// it sight-unseen. Aborting the open leaves the file completely untouched, which is the
        /// safest outcome: the data is preserved and the problem is stated rather than hidden.
        /// Returns true when the blueprint is safe to open.
        /// </summary>
        private bool CanRepresentEveryValue(string filePath, string resRef, EditorSchema schema)
        {
            IReadOnlyList<UnresolvedFieldValue> unresolved;
            try
            {
                var document = JsonGffDocument.Load(filePath);
                unresolved = DropdownValueValidator.FindUnresolved(
                    document, schema,
                    lookupKey => _lookups.GetOptions(lookupKey).Select(option => option.Id).ToHashSet());
            }
            catch (Exception ex)
            {
                // Never let the safety check itself block an otherwise-openable file.
                _log.AppendLine($"Could not validate lookup values for {resRef}: {ex.Message}");
                return true;
            }

            if (unresolved.Count == 0)
                return true;

            var details = unresolved
                .Select(u => $"{u.Label} ({u.FieldName}) = {u.Value}   [not found in {u.LookupKey}]")
                .ToList();

            foreach (var line in details)
                _log.AppendLine($"{resRef}: {line}");

            Shell.Views.ErrorDialog.Show(
                $"Cannot open '{resRef}'",
                "This blueprint stores values that are not present in the game data tables the editor "
                + "uses to fill its dropdowns. Opening it would show those fields as blank, and editing "
                + "anything else could overwrite them without you seeing the original value.\n\n"
                + "The file has NOT been modified. Fix the referenced rows in the 2DA data (or correct "
                + "the blueprint outside the toolset), then reopen it.",
                details);

            return false;
        }

        /// <summary>
        /// The schema a blueprint type opens with, or null when the type has no editor yet.
        /// </summary>
        /// <remarks>
        /// Public because it is the one fact about this class worth asserting from outside: every
        /// type the module explorer lists has to open as something, and a type that quietly falls
        /// through to "No editor available yet" is a blank double-click a builder has to guess at.
        /// </remarks>
        public static EditorSchema? SchemaFor(ResourceType type) => GetSchema(type);

        private static EditorSchema? GetSchema(ResourceType type)
        {
            return type switch
            {
                ResourceType.Utc => UtcSchema.Build(),
                ResourceType.Uti => UtiSchema.Build(),
                ResourceType.Utp => UtpSchema.Build(),
                ResourceType.Utd => UtdSchema.Build(),
                ResourceType.Utw => UtwSchema.Build(),
                ResourceType.Uts => UtsSchema.Build(),
                ResourceType.Utt => UttSchema.Build(),
                ResourceType.Utm => UtmSchema.Build(),
                _ => null
            };
        }
    }
}
