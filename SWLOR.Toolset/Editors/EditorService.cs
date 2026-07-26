using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Editors.Schemas;
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

        /// <summary>Supplies the area editor its placement-ghost geometry; null degrades the ghost to a marker.</summary>
        private readonly Workspace.BlueprintPreviewRenderer? _previewRenderer;

        /// <summary>Shared engine-symbol database driving script completion; null disables it.</summary>
        private readonly Workspace.ScriptLanguageService? _scriptLanguage;

        /// <summary>Where script diagnostics land; null in a shell without the panel.</summary>
        private readonly Shell.Panels.ProblemsViewModel? _problems;

        /// <summary>
        /// Built once, in the background, on first use. Scanning every blueprint and area for script
        /// slots is expensive, and the picker is the only thing that needs it — so it must not be
        /// paid for at startup by builders who never open one.
        /// </summary>
        private readonly Lazy<Task<Domain.Script.ScriptUsageIndex?>> _scriptUsageIndex;
        private readonly TileWalkmeshCache? _tileWalkmeshCache;
        private readonly Dictionary<string, BlueprintEditorViewModel> _openEditors = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AreaEditorViewModel> _openAreaEditors = new(StringComparer.OrdinalIgnoreCase);

        // Keyed by path like the blueprint map rather than by resref like the area map: a script is
        // one file, so the path is its identity and there is no are/git/gic triplet to name.
        private readonly Dictionary<string, ScriptEditorViewModel> _openScriptEditors = new(StringComparer.OrdinalIgnoreCase);

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
            Shell.Panels.ProblemsViewModel? problems = null)
        {
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
            _scriptLanguage = scriptLanguage;
            _problems = problems;
            _scriptUsageIndex = new Lazy<Task<Domain.Script.ScriptUsageIndex?>>(() => Task.Run(() =>
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
            }));
        }

        /// <summary>Backs the script slots on one editor, describing its owner for the picker title.</summary>
        private ScriptSlotHost CreateScriptSlotHost(string ownerDescription) =>
            new(_workspaceContext, () => this, _log, ownerDescription, _scriptUsageIndex);

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
                editor.DiagnosticsChanged += diagnostics =>
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => _problems?.SetDiagnostics(resRef, diagnostics));
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

            try
            {
                if (!CanRepresentEveryValue(filePath, resRef, schema))
                    return;

                var editor = new BlueprintEditorViewModel(
                    filePath, resRef, type, schema, _lookups, _gameCodeIndex, _log, _prompts,
                    CreateScriptSlotHost($"{type.SingularDisplayName()} '{resRef}'"));
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
            return workspace != null &&
                   _openEditors.ContainsKey(workspace.GetResourcePath(type, resRef));
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

            foreach (var editor in _openAreaEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
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
                !_openAreaEditors.Values.Any(editor => editor.IsDirty))
                return true;

            var choice = await _prompts.ConfirmCloseAsync("all open editors").ConfigureAwait(true);
            if (choice == UnsavedChangesChoice.Save)
                return await SaveAllAsync().ConfigureAwait(true);

            if (choice != UnsavedChangesChoice.Discard)
                return false;

            foreach (var editor in _openEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openAreaEditors.Values)
                editor.ApproveApplicationClose();

            return true;
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
                    _previewRenderer != null ? _previewRenderer.BuildModel : null,
                    CreateScriptSlotHost($"Area '{resRef}'"));
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
