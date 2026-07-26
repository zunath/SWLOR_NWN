using SWLOR.Toolset.Domain.Editors;
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
        private readonly DoorTypeService? _doorTypes;
        private readonly WaypointAppearanceService? _waypointAppearances;
        private readonly Domain.GameData.TwoDa.TwoDaService? _twoDaService;
        private Triggers.ChoicePreviewService? _choicePreviews;

        /// <summary>Backs the placeable Appearance tab's model grid; null degrades it to an empty grid.</summary>
        private readonly PlaceableModelCatalog? _placeableModels;
        private readonly ThumbnailService? _thumbnails;
        private readonly PlaceableIndexService? _placeableIndexes;

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
            Domain.GameData.TwoDa.TwoDaService? twoDaService = null)
        {
            _placeableModels = placeableModels;
            _thumbnails = thumbnails;
            _placeableIndexes = placeableIndexes;
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
        public async Task CompileScriptAsync(string resRef)
        {
            if (_compileService == null || !_compileService.IsAvailable)
            {
                _log.AppendLine("Cannot compile: nwn_script_comp.exe is missing from tools/SWLOR.CLI.");
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
                var editor = new ScriptEditorViewModel(filePath, resRef, _log, _prompts, _scriptLanguage);
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

            try
            {
                if (!CanRepresentEveryValue(filePath, resRef, schema))
                    return;

                // Triggers get the behavior editor rather than the generic schema form: what a
                // trigger is for drives which fields it even has.
                if (type == ResourceType.Utt)
                {
                    OpenTriggerEditor(filePath, resRef);
                    return;
                }

                var editor = new BlueprintEditorViewModel(
                    filePath, resRef, type, schema, _lookups, _gameCodeIndex, _log, _prompts,
                    // So a localized field that carries a strref but no language-0 override can show what
                    // that strref says, instead of a blank the builder reads as missing data.
                    _tlkService == null ? null : _tlkService.GetString,
                    CreateScriptSlotHost($"{type.SingularDisplayName()} '{resRef}'"),
                    type == ResourceType.Utp ? CreatePlaceableSections : null,
                    () => _workspaceContext.Workspace);
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

            var source = workspace.GetResourcePath(ResourceType.Nss, resRef);
            if (!File.Exists(source) ||
                Domain.Script.ScriptStalenessScanner.IsEntryPoint(
                    Domain.Script.ScriptTextDocument.Load(source).Text))
                return outcome.Succeeded;

            var dependents = _compileService.DependentsOf(resRef);
            if (dependents.Count == 0)
                return outcome.Succeeded;

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                editor.OfferDependentRebuild(
                    dependents,
                    () => _compileService.BuildDependentsAsync(dependents)));

            return outcome.Succeeded;
        }

        /// <summary>
        /// Builds the placeable's Appearance and Behavior tabs. The module-wide scans they validate
        /// against are kicked off here rather than at startup, so a session that never opens a
        /// placeable never pays for them.
        /// </summary>
        private Placeables.PlaceableEditorSections? CreatePlaceableSections(
            EditorFieldContext context, Func<string, Action, bool> runEdit)
        {
            // No 2DA layer means no model grid, so the placeable opens with the plain tabs rather
            // than an Appearance tab that could only ever be empty.
            if (_placeableModels == null)
                return null;

            _placeableIndexes?.EnsureBuilt();

            var values = new Placeables.BehaviorValueSourceProvider(
                _gameCodeIndex,
                () => _placeableIndexes?.Tags);

            var appearance = new Placeables.AppearanceSectionViewModel(
                context,
                _placeableModels,
                _thumbnails,
                () => _placeableIndexes?.Usage ?? Domain.Workspace.PlaceableAppearanceUsageIndex.Empty,
                runEdit,
                _resourceIndex,
                // The same cache the area viewport builds its models through, so a model a builder
                // has already seen in an area costs nothing to preview here.
                modelResRef => _tileModelCache?.GetOrBuild(modelResRef));

            var behavior = new Placeables.PlaceableBehaviorSectionViewModel(
                context, values, _prompts, runEdit);

            return new Placeables.PlaceableEditorSections(appearance, behavior);
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
            _choicePreviews ??= new Triggers.ChoicePreviewService(_resourceIndex);
            var editor = new Triggers.TriggerDocumentViewModel(
                filePath, resRef, _gameCodeIndex, _log, _prompts, ResolveTagArea, ResolveTriggerChoices,
                _choicePreviews);
            editor.Closed += _ => _openTriggerEditors.Remove(filePath);
            editor.CloseRequested += _ => _factory.CloseDocument(editor);
            editor.CatalogEntryChanged += () =>
                _workspaceContext.RefreshCatalogEntry(ResourceType.Utt, resRef);
            _openTriggerEditors[filePath] = editor;
            _factory.OpenDocument(editor);
        }

        /// <summary>
        /// Game-data choice sets a trigger row asks for. Most forward to the shared lookup provider,
        /// whose keys these deliberately match; the palette categories come from the module's own
        /// .itp, which no 2DA lookup covers.
        /// </summary>
        private IReadOnlyList<Domain.Editors.Triggers.TriggerChoice> ResolveTriggerChoices(string key)
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
                .Select(option => new Domain.Editors.Triggers.TriggerChoice(option.Id, option.Display))
                .ToList();
        }

        /// <summary>
        /// The trigger palette's categories, named rather than numbered. A missing or unreadable
        /// palette yields an empty list, which shows as an empty picker instead of a bare id.
        /// </summary>
        private IReadOnlyList<Domain.Editors.Triggers.TriggerChoice> ResolveTriggerCategories()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<Domain.Editors.Triggers.TriggerChoice>();

            try
            {
                var path = Path.Combine(workspace.ModuleRoot, "itp", "triggerpalcus.itp.json");
                if (!File.Exists(path))
                    return Array.Empty<Domain.Editors.Triggers.TriggerChoice>();

                return Domain.Editors.Triggers.PaletteCategoryReader.Read(
                    Domain.Documents.ItpDocument.Load(path),
                    _tlkService != null ? _tlkService.GetString : null);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read the trigger palette categories: {ex.Message}");
                return Array.Empty<Domain.Editors.Triggers.TriggerChoice>();
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
        /// Tags of every blueprint of one kind, so a store or waypoint argument is a list rather than
        /// a remembered string. Read from the background catalog, which already parsed them.
        /// </summary>
        private IReadOnlyList<string> TagsFor(string extension)
        {
            if (!ResourceTypeExtensions.TryFromExtension(extension, out var type))
                return Array.Empty<string>();

            var entries = _workspaceContext.Catalog?.Entries;
            if (entries == null)
                return Array.Empty<string>();

            return entries
                .Where(entry => entry.ResourceType == type && !string.IsNullOrWhiteSpace(entry.Tag))
                .Select(entry => entry.Tag!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
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
                        : null);
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

            var entry = _workspaceContext.Catalog?.Entries
                .FirstOrDefault(candidate =>
                    candidate.ResourceType == type &&
                    string.Equals(candidate.ResRef, resRef, StringComparison.OrdinalIgnoreCase));

            return string.IsNullOrWhiteSpace(entry?.Name) ? null : entry.Name;
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
