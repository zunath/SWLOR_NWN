using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using SWLOR.Toolset.AreaGeneration;
using SWLOR.Toolset.Archives;
using SWLOR.Toolset.Factions;
using SWLOR.Toolset.Domain.AreaGeneration.Authoring;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell
{
    /// <summary>
    /// The shell's top-level view model: owns the Dock <see cref="Layout"/>, the status-bar text
    /// (module open / catalog build progress), and the startup orchestration - load settings, open
    /// the module root, kick off the background catalog build - keeping the UI thread free
    /// throughout (<see cref="InitializeAsync"/> awaits the slow parts on background threads).
    /// </summary>
    public partial class ShellViewModel : ObservableObject
    {
        private readonly ToolsetSettings _settings;

        /// <summary>What service registration discovered and had nowhere to say at the time.</summary>
        private readonly StartupNotice? _startupNotice;
        private readonly WorkspaceContext _workspaceContext;
        private readonly OutputLogService _log;
        private readonly ModuleFileWatcher _fileWatcher;
        private readonly ModuleExplorerViewModel _explorer;
        private readonly SearchViewModel _search;
        private readonly PaletteViewModel _palette;

        /// <summary>Display switches for the quick-access bar; shared by every area viewport.</summary>
        public Viewport.ViewportDisplayOptions Display { get; }
        private readonly ThumbnailService _thumbnails;
        private DispatcherTimer? _progressTimer;

        [ObservableProperty]
        private IRootDock? _layout;

        [ObservableProperty]
        private string _statusText = "Starting...";

        [ObservableProperty]
        private bool _isPacking;

        [ObservableProperty]
        private bool _isValidationRunning;

        [ObservableProperty]
        private bool _isBuildingScripts;

        [ObservableProperty]
        private bool _isManagingErfArchives;

        [ObservableProperty]
        private bool _isManagingFactions;

        [ObservableProperty]
        private bool _isGeneratingArea;

        private bool IsShellModuleMutationLocked =>
            IsPacking || IsValidationRunning || IsBuildingScripts ||
            IsManagingErfArchives || IsManagingFactions || IsGeneratingArea;
        public bool IsModuleMutationLocked =>
            IsShellModuleMutationLocked || _mutationLock.IsResourceDeletionActive;
        public bool IsActiveEditorBusy => _activeEditor?.IsBusy == true;
        public bool IsInteractionBlocked => IsModuleMutationLocked || IsActiveEditorBusy;

        /// <summary>
        /// The shared answer to <see cref="IsModuleMutationLocked"/>, published for the panels and
        /// editor tabs that also write to the module. The shell publishes its own operations; Module
        /// Contents adds scoped resource deletions so shared open/save/close routes see them too.
        /// </summary>
        private readonly ModuleMutationLock _mutationLock;

        /// <summary>
        /// The editor tab the File/Edit menus act on, or null when no document is open. Tracked
        /// from the dock's active document so Save/Undo/Redo always target what the user is looking
        /// at; the shell listens to its property changes to keep the menu items enabled correctly.
        /// </summary>
        private IEditorDocument? _activeEditor;

        private INotifyPropertyChanged? _activeEditorNotifier;

        private IDocumentStatusSource? _activeStatusSource;

        /// <summary>Raised when the File menu's Exit item asks the window to close (which still runs the unsaved-changes prompt).</summary>
        public event Action? ExitRequested;

        private readonly ToolsetDockFactory _factory;
        private readonly OutputViewModel _output;
        private readonly ValidationViewModel _validation;
        private readonly Lazy<Editors.EditorService> _editorService;
        private readonly PackService _packService;
        private readonly IEditorPromptService _prompts;
        private readonly ScriptCompileService? _compileService;
        private readonly ScriptReferenceViewModel? _scriptReference;
        private readonly ProblemsViewModel? _problems;
        private readonly AreaContentsViewModel? _areaContents;
        private readonly ErfArchiveService? _erfArchiveService;
        private readonly ModuleCustomContentService? _moduleCustomContent;
        private readonly TilesetCatalog? _tilesetCatalog;
        private readonly ResourceIndex? _resourceIndex;

        /// <summary>Guards against a second rescan starting while one is already reopening the catalog.</summary>
        private bool _isRescanningAfterWatcherOverflow;
        private bool _rescanRequestedWhileRunning;

        public ShellViewModel(
            ToolsetSettings settings,
            WorkspaceContext workspaceContext,
            OutputLogService log,
            ModuleFileWatcher fileWatcher,
            ModuleExplorerViewModel explorer,
            SearchViewModel search,
            PaletteViewModel palette,
            Viewport.ViewportDisplayOptions display,
            ToolsetDockFactory factory,
            Func<Editors.EditorService> editorService,
            PackService packService,
            IEditorPromptService prompts,
            OutputViewModel output,
            ValidationViewModel validation,
            ThumbnailService thumbnails,
            ScriptCompileService? compileService = null,
            ScriptReferenceViewModel? scriptReference = null,
            ProblemsViewModel? problems = null,
            AreaContentsViewModel? areaContents = null,
            StartupNotice? startupNotice = null,
            ModuleMutationLock? mutationLock = null,
            ErfArchiveService? erfArchiveService = null,
            ModuleCustomContentService? moduleCustomContent = null,
            TilesetCatalog? tilesetCatalog = null,
            ResourceIndex? resourceIndex = null)
        {
            _resourceIndex = resourceIndex;
            _tilesetCatalog = tilesetCatalog;
            _moduleCustomContent = moduleCustomContent;
            _areaContents = areaContents;
            _erfArchiveService = erfArchiveService;
            _mutationLock = mutationLock ?? new ModuleMutationLock();
            _mutationLock.Changed += OnPublishedMutationStateChanged;

            // Every module write is checked against this, wherever it comes from. The eight editor
            // tabs each have their own Save button that goes straight to their own TrySaveAsync,
            // and greying the shell's menu never reached any of them.
            ModuleMutationLock.ModuleWrites = _mutationLock;
            _startupNotice = startupNotice;
            _compileService = compileService;
            _scriptReference = scriptReference;
            _problems = problems;
            _thumbnails = thumbnails ?? throw new ArgumentNullException(nameof(thumbnails));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
            // A watcher error most often means its native buffer overflowed and dropped events (a
            // bulk Git checkout is the ordinary trigger) - the catalog and indexes can only be trusted
            // again after the same full rescan InitializeAsync runs at startup.
            _fileWatcher.RescanRequested += OnFileWatcherRescanRequested;
            _explorer = explorer ?? throw new ArgumentNullException(nameof(explorer));
            _search = search ?? throw new ArgumentNullException(nameof(search));
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            Display = display ?? throw new ArgumentNullException(nameof(display));
            ArgumentNullException.ThrowIfNull(editorService);
            _editorService = new Lazy<Editors.EditorService>(
                editorService,
                LazyThreadSafetyMode.ExecutionAndPublication);
            _packService = packService ?? throw new ArgumentNullException(nameof(packService));
            _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _validation = validation ?? throw new ArgumentNullException(nameof(validation));
            ArgumentNullException.ThrowIfNull(validation);
            IsValidationRunning = validation.IsRunning;
            validation.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ValidationViewModel.IsRunning))
                    IsValidationRunning = validation.IsRunning;
            };

            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _factory = factory;

            factory.ActiveDocumentChanged += SetActiveEditor;
            factory.ProportionsChanged += QueueLayoutSave;

            // Clicking a Problems row focuses that script on that line.
            if (_problems != null)
            {
                _problems.NavigateRequested += row =>
                    _editorService.Value.NavigateToScriptLine(row.ResRef, row.Diagnostic.Line);

                // The authoritative tier. Compiler findings replace only the compiler's own rows, so
                // they survive the editor's quarter-second idle re-analysis instead of being wiped by it.
                if (_compileService != null)
                    _compileService.DiagnosticsProduced += (resRef, diagnostics) =>
                        Dispatcher.UIThread.Post(() =>
                        {
                            _problems.SetDiagnostics(
                                resRef, Domain.Script.Syntax.ScriptDiagnosticSource.Compiler, diagnostics);

                            if (diagnostics.Any(d => d.Severity == Domain.Script.Syntax.ScriptDiagnosticSeverity.Error))
                                _factory.ShowProblems();
                        });
            }

            Layout = factory.CreateLayout();
            if (Layout != null)
                factory.InitLayout(Layout);

            if (_moduleCustomContent != null)
            {
                _moduleCustomContent.Reloaded += result => Dispatcher.UIThread.Post(() =>
                {
                    // Clear first: open galleries re-request their visible page during the editor
                    // reload below. Clearing afterward discarded those fresh results and left the
                    // tiles on their letter placeholders until the editor was reopened.
                    _thumbnails.ClearCache();
                    if (_editorService.IsValueCreated)
                        _editorService.Value.ReloadOpenGameResources();
                    _palette.Refresh();
                    // The stock dynamic-race rows do not have a fixed model resref. Their generic
                    // segmented previews were warmed at startup, but the cache clear above correctly
                    // invalidates those pixels after the module's final HAK stack is installed. Queue
                    // fresh representatives now so Dwarf through Human cannot remain placeholders.
                    _thumbnails.WarmGenericSegmentedCreaturePreviews();
                    StatusText = result.MissingHaks.Count == 0
                        ? $"Custom content reloaded: {result.LoadedHakCount} HAK layer(s)."
                        : $"Custom content reloaded with {result.MissingHaks.Count} missing HAK(s).";
                });
            }
        }

        /// <summary>Opens the native generator and commits its result through the normal area writer.</summary>
        [RelayCommand(CanExecute = nameof(CanMutateModule))]
        private async Task AreaGenerator()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || _tilesetCatalog == null)
            {
                StatusText = "Area Generator needs an open module and loaded game data.";
                return;
            }

            if (ScriptCompileService.AnyCompilationActive)
            {
                StatusText = "Area Generator will be available after the active script compile finishes.";
                return;
            }

            IsGeneratingArea = true;
            try
            {
                using (ModuleMutationLock.AllowModuleWrites())
                {
                    if (!await _editorService.Value.SaveAllAsync().ConfigureAwait(true))
                    {
                        StatusText = "Area Generator cancelled: an open editor could not be saved.";
                        return;
                    }
                }

                StatusText = "Opening Area Generator...";
                string? createdResref;
                using (ModuleMutationLock.AllowModuleWrites())
                {
                    var authoring = new AreaGenerationAuthoringService(_tilesetCatalog);
                    var renderer = new AreaGenerationPreviewRenderer(_resourceIndex);
                    createdResref = await AreaGeneratorWindow.ShowAsync(
                        authoring,
                        renderer,
                        _tilesetCatalog,
                        workspace).ConfigureAwait(true);
                }

                if (string.IsNullOrWhiteSpace(createdResref))
                {
                    StatusText = "Area Generator closed.";
                    return;
                }

                _workspaceContext.RefreshCatalogEntry(ResourceType.Area, createdResref);
                _workspaceContext.InvalidatePlacementIndex();
                _explorer.Refresh();
                _editorService.Value.TryOpenEditor(ResourceType.Area, createdResref);
                StatusText = $"Created generated area '{createdResref}'.";
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Area Generator failed: {ex.Message}");
                StatusText = "Area Generator could not be opened; see Output.";
            }
            finally
            {
                IsGeneratingArea = false;
            }
        }

        /// <summary>
        /// Coalesces the burst of proportion changes a single divider drag produces into one settings
        /// write, a short moment after the builder lets go.
        /// </summary>
        private DispatcherTimer? _layoutSaveTimer;

        private void QueueLayoutSave()
        {
            if (_layoutSaveTimer == null)
            {
                _layoutSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                _layoutSaveTimer.Tick += (_, _) =>
                {
                    _layoutSaveTimer!.Stop();
                    SaveLayout();
                };
            }

            // Restarting rather than letting it run means the file is written once the drag has settled,
            // not part-way through it.
            _layoutSaveTimer.Stop();
            _layoutSaveTimer.Start();
        }

        /// <summary>
        /// Writes the current divider positions out now. Called on shutdown as well as from the debounce,
        /// so a window closed straight after a drag still remembers it.
        /// </summary>
        public void SaveLayout()
        {
            _layoutSaveTimer?.Stop();
            _settings.SetDockProportions(_factory.CaptureProportions());
        }

        private void SetActiveEditor(Dock.Model.Mvvm.Controls.Document? document)
        {
            if (_activeEditorNotifier != null)
                _activeEditorNotifier.PropertyChanged -= OnActiveEditorPropertyChanged;

            _activeEditor = document as IEditorDocument;
            _activeStatusSource = document as IDocumentStatusSource;
            _activeEditorNotifier = document as INotifyPropertyChanged;

            if (_activeEditorNotifier != null)
                _activeEditorNotifier.PropertyChanged += OnActiveEditorPropertyChanged;

            // Area Contents follows the front tab. Pointed at null while a blueprint or script is in
            // front rather than left showing the last area's objects, which would be a list of
            // things Delete no longer reaches.
            _areaContents?.SetEditor(document as Editors.AreaEditorViewModel);

            // The right dock follows the tab: the Palette lists the front AREA's tileset, so it has
            // nothing to say while a script is in front, and Script Reference has nothing to say
            // while an area is. Insert-at-cursor is retargeted at the same moment.
            var script = document as Editors.ScriptEditorViewModel;
            _activeScript = script;
            _scriptReference?.SetInsertTarget(script != null
                ? text => script.InsertAtCursorRequested?.Invoke(text)
                : null);
            _factory.ShowRightTool(script != null);

            NotifyInteractionStateChanged();
            OnPropertyChanged(nameof(StatusDetail));
            CompileActiveScriptCommand.NotifyCanExecuteChanged();
        }

        private Editors.ScriptEditorViewModel? _activeScript;

        /// <summary>
        /// The active document's own status contribution - the area editor puts the selection's
        /// coordinates here, which is where Aurora kept them too.
        /// </summary>
        public string StatusDetail => _activeStatusSource?.StatusDetail ?? string.Empty;

        // Any property change on an editor may have moved its undo history (the editors raise their
        // own Can*Undo/Redo notifications), so re-evaluate rather than matching property names that
        // differ between the blueprint and area editors.
        private void OnActiveEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IEditorDocument.IsBusy))
                NotifyInteractionStateChanged();
            else
                NotifyActiveEditorCommandsChanged();
            OnPropertyChanged(nameof(StatusDetail));
        }

        private void NotifyActiveEditorCommandsChanged()
        {
            SaveCommand.NotifyCanExecuteChanged();
            // Ctrl+B must go dead while a compile is already running, and come back when it ends.
            CompileActiveScriptCommand.NotifyCanExecuteChanged();
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task Save()
        {
            if (_activeEditor == null)
                return;

            var saved = await _activeEditor.TrySaveAsync().ConfigureAwait(true);
            StatusText = saved ? "Saved." : "Save cancelled or failed - see Output.";
        }

        private bool CanSave() => !IsInteractionBlocked && _activeEditor != null;

        [RelayCommand(CanExecute = nameof(CanMutateModule))]
        private async Task SaveAll()
        {
            var saved = await _editorService.Value.SaveAllAsync().ConfigureAwait(true);
            StatusText = saved ? "All open editors saved." : "Save cancelled or failed - see Output.";
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo() => _activeEditor?.Undo();

        private bool CanUndo() => !IsInteractionBlocked && _activeEditor?.CanUndo == true;

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo() => _activeEditor?.Redo();

        private bool CanRedo() => !IsInteractionBlocked && _activeEditor?.CanRedo == true;

        /// <summary>
        /// The window title, which names the module rather than just the program - a builder often has
        /// more than one module checked out and the title bar is the only place that distinguishes them.
        /// </summary>
        [ObservableProperty]
        private string _windowTitle = "SWLOR Toolset";

        [RelayCommand]
        private void FocusValidation() => _factory.Focus(_validation);

        [RelayCommand(CanExecute = nameof(CanMutateModule))]
        private void ModuleProperties()
        {
            _editorService.Value.OpenModuleProperties(new Editors.Module.ModulePropertiesActions(
                RunValidationFromModulePropertiesAsync,
                RunBuildAllFromModulePropertiesAsync,
                RunPackFromModulePropertiesAsync,
                OpenBuildConfiguration));
        }

        private async Task RunValidationFromModulePropertiesAsync()
        {
            _factory.Focus(_validation);
            if (_validation.RunCommand.CanExecute(null))
                await _validation.RunCommand.ExecuteAsync(null).ConfigureAwait(true);
        }

        private async Task RunBuildAllFromModulePropertiesAsync()
        {
            if (BuildAllScriptsCommand.CanExecute(null))
                await BuildAllScriptsCommand.ExecuteAsync(null).ConfigureAwait(true);
        }

        private async Task RunPackFromModulePropertiesAsync()
        {
            if (PackModuleCommand.CanExecute(null))
                await PackModuleCommand.ExecuteAsync(null).ConfigureAwait(true);
        }

        private void OpenBuildConfiguration(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not open build configuration '{path}': {ex.Message}");
            }
        }

        /// <summary>
        /// Saves every open document before entering the modal archive workflow. That makes export's
        /// "saved snapshot" literal, and prevents an import from replacing a file while an older
        /// unsaved generation remains open in an editor tab.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanMutateModule))]
        private async Task ErfArchives()
        {
            if (_erfArchiveService == null || _workspaceContext.Workspace == null)
            {
                StatusText = "ERF Manager needs an open module.";
                return;
            }

            if (ScriptCompileService.AnyCompilationActive)
            {
                StatusText = "ERF Manager will be available after the active script compile finishes.";
                return;
            }

            IsManagingErfArchives = true;
            try
            {
                using (ModuleMutationLock.AllowModuleWrites())
                {
                    if (!await _editorService.Value.SaveAllAsync().ConfigureAwait(true))
                    {
                        StatusText = "ERF Manager cancelled: an open editor could not be saved.";
                        return;
                    }
                }

                StatusText = "Opening ERF Manager...";
                await ErfArchiveWindow.ShowAsync(_erfArchiveService, _settings).ConfigureAwait(true);
                StatusText = "ERF Manager closed.";
            }
            finally
            {
                IsManagingErfArchives = false;
            }
        }

        /// <summary>
        /// Opens the module-wide faction workflow after saving every open editor. The window owns a
        /// grouped transaction which writes repute.fac and any remapped blueprint/area references
        /// together, so no other module writer may run while it is open.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanMutateModule))]
        private async Task FactionEditor()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
            {
                StatusText = "Faction Editor needs an open module.";
                return;
            }

            if (ScriptCompileService.AnyCompilationActive)
            {
                StatusText = "Faction Editor will be available after the active script compile finishes.";
                return;
            }

            IsManagingFactions = true;
            try
            {
                using (ModuleMutationLock.AllowModuleWrites())
                {
                    if (!await _editorService.Value.SaveAllAsync().ConfigureAwait(true))
                    {
                        StatusText = "Faction Editor cancelled: an open editor could not be saved.";
                        return;
                    }
                }

                StatusText = "Opening Faction Editor...";
                IReadOnlyCollection<string> changedPaths;
                using (ModuleMutationLock.AllowModuleWrites())
                {
                    changedPaths = await FactionEditorWindow.ShowAsync(
                        workspace.ModuleRoot,
                        _log,
                        _prompts).ConfigureAwait(true);
                }

                _editorService.Value.RefreshAfterFactionSave(changedPaths);
                StatusText = changedPaths.Count == 0
                    ? "Faction Editor closed."
                    : $"Faction Editor saved {changedPaths.Count} module file" +
                      $"{(changedPaths.Count == 1 ? string.Empty : "s")}.";
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Faction Editor failed: {ex.Message}");
                StatusText = "Faction Editor could not be opened; see Output.";
            }
            finally
            {
                IsManagingFactions = false;
            }
        }

        // ----- scripts -----

        private bool CanCompileActiveScript => _activeScript?.CanCompile == true;

        /// <summary>
        /// Compiles the script in front (Ctrl+B).
        /// </summary>
        /// <remarks>
        /// A command here rather than only on the document because a window-level shortcut needs one:
        /// Ctrl+B has to work with focus in the explorer or the reference panel, not just in the
        /// buffer. It is deliberately <b>not</b> in any menu — compiling one script is an act on the
        /// open document, and as a Build-menu item it sat greyed out whenever no script was open.
        /// The work itself stays on the editor, which owns the save-then-compile sequence and the
        /// status strip; this only forwards.
        /// </remarks>
        [RelayCommand(CanExecute = nameof(CanCompileActiveScript))]
        private async Task CompileActiveScript()
        {
            if (_activeScript?.CompileCommand.CanExecute(null) == true)
                await _activeScript.CompileCommand.ExecuteAsync(null).ConfigureAwait(true);
        }

        // Everything below is genuinely module-scoped, which is what the Build menu is for.

        /// <summary>Compiles every entry-point script in the module.</summary>
        [RelayCommand(CanExecute = nameof(CanMutateModule))]
        private async Task BuildAllScripts()
        {
            if (_compileService == null || IsModuleMutationLocked)
                return;

            if (ScriptCompileService.AnyCompilationActive)
            {
                _log.AppendLine("A script compile is still running; Build All will be available when it finishes.");
                return;
            }

            IsBuildingScripts = true;
            try
            {
                using (ModuleMutationLock.AllowModuleWrites())
                {
                    if (!await _editorService.Value.SaveScriptsAsync(compileOnSave: false).ConfigureAwait(true))
                    {
                        StatusText = "Build cancelled: an open script could not be saved.";
                        return;
                    }
                }

                StatusText = "Building all scripts...";
                _factory.Focus(_output);

                var outcome = await _compileService.BuildAllAsync().ConfigureAwait(true);
                StatusText = !outcome.Ran
                    ? "Cannot build scripts: no module is open, or no script compiler is vendored."
                    : outcome.Failed == 0
                        ? $"Built {outcome.Compiled} script(s)."
                        : $"Built {outcome.Compiled} script(s); {outcome.Failed} failed - see Output.";
            }
            finally
            {
                IsBuildingScripts = false;
            }
        }

        /// <summary>
        /// True while previews are being rendered. Deliberately not part of
        /// <see cref="IsModuleMutationLocked"/>: the build only reads blueprints, so the builder keeps
        /// working through it - it just must not be started twice at once.
        /// </summary>
        [ObservableProperty]
        private bool _isBuildingPreviewCache;

        [RelayCommand(CanExecute = nameof(CanBuildPreviewCache))]
        private Task BuildPreviewCacheAsync() => RunPreviewCacheBuildAsync();

        private bool CanBuildPreviewCache() => !IsBuildingPreviewCache && _thumbnails.IsAvailable;

        partial void OnIsBuildingPreviewCacheChanged(bool value)
        {
            BuildPreviewCacheCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Renders every missing palette preview and stores it on disk. Runs in the background with
        /// progress in the Output pane, the same way the catalog build reports itself.
        /// </summary>
        private async Task RunPreviewCacheBuildAsync()
        {
            if (IsBuildingPreviewCache)
                return;

            if (!_thumbnails.IsAvailable)
            {
                _log.AppendLine("Preview cache: skipped (game data is not loaded, so no artwork can be resolved).");
                return;
            }

            IsBuildingPreviewCache = true;
            try
            {
                var pruned = await Task.Run(_thumbnails.PruneSupersededCaches).ConfigureAwait(true);
                if (pruned > 0)
                    _log.AppendLine($"Preview cache: removed {pruned} folder(s) from an older render version.");

                _log.AppendLine($"Preview cache: building in '{_thumbnails.CachePath ?? "(memory only)"}'.");

                var progress = new Progress<PreviewCacheProgress>(report =>
                    _log.AppendLine(
                        $"Preview cache: {report.Processed}/{report.Total} ({report.PercentComplete}%)."));

                var result = await _thumbnails.WarmAsync(progress).ConfigureAwait(true);

                _log.AppendLine(
                    $"Preview cache complete: {result.Rendered} rendered, {result.Reused} already cached, " +
                    $"{result.WithoutArtwork} with no artwork (shown as type symbols).");
                if (result.Failed > 0)
                    _log.AppendLine($"Preview cache: {result.Failed} blueprint(s) failed to render and will be retried.");
                StatusText = $"Preview cache ready: {result.Rendered + result.Reused} previews available.";
                _palette.Refresh();
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Preview cache build failed: {ex.Message}");
                StatusText = "Preview cache build failed - see Output.";
            }
            finally
            {
                IsBuildingPreviewCache = false;
            }
        }

        /// <summary>Closes the application, going through the window's normal unsaved-changes prompt.</summary>
        [RelayCommand]
        private void Exit() => ExitRequested?.Invoke();

        /// <summary>Returns true when the main window may close after handling unsaved editors.</summary>
        public Task<bool> TryCloseAsync()
        {
            var compilationActive = ScriptCompileService.AnyCompilationActive;
            if (IsInteractionBlocked || compilationActive)
            {
                StatusText = compilationActive
                    ? "Wait for the active script compile to finish before closing."
                    : IsActiveEditorBusy
                        ? "Wait for the active editor operation to finish before closing."
                        : "Wait for the active module operation to finish before closing.";
                return Task.FromResult(false);
            }

            return _editorService.IsValueCreated
                ? _editorService.Value.TryPrepareApplicationCloseAsync()
                : Task.FromResult(true);
        }

        [RelayCommand(CanExecute = nameof(CanMutateModule))]
        private async Task PackModuleAsync()
        {
            var moduleRoot = _workspaceContext.Workspace?.ModuleRoot;
            if (moduleRoot == null)
            {
                _log.AppendLine("No module open to pack.");
                return;
            }

            if (ScriptCompileService.AnyCompilationActive)
            {
                _log.AppendLine("A script compile is still running; pack when it finishes so no .ncs is copied mid-replacement.");
                return;
            }

            IsPacking = true;
            try
            {
                using (ModuleMutationLock.AllowModuleWrites())
                {
                    if (!await _editorService.Value.SaveAllAsync().ConfigureAwait(true))
                    {
                        StatusText = "Pack cancelled because an open editor could not be saved.";
                        _log.AppendLine("Pack aborted: one or more open editors were not saved.");
                        return;
                    }
                }

                if (_compileService != null &&
                    !await ResolveStaleScriptsBeforePackAsync().ConfigureAwait(true))
                {
                    return;
                }

                StatusText = "Packing module...";
                var exitCode = await _packService.PackAsync(moduleRoot).ConfigureAwait(true);
                StatusText = exitCode == 0 ? "Pack completed." : $"Pack failed (exit code {exitCode}) — see Output.";
            }
            finally
            {
                IsPacking = false;
            }
        }

        private async Task<bool> ResolveStaleScriptsBeforePackAsync()
        {
            var stale = _compileService!.ScanStale();
            var warning = ScriptPackReadiness.Evaluate(stale);
            if (warning == null)
                return true;

            _factory.Focus(_output);
            _log.AppendLine(warning.Headline + ".");
            foreach (var line in warning.OutputLines)
                _log.AppendLine($"  {line}");

            StatusText = $"{stale.Count} script(s) need rebuilding before pack.";

            var buildFirst = await _prompts.ConfirmDestructiveAsync(
                warning.Headline,
                warning.Message,
                warning.ConfirmLabel).ConfigureAwait(true);

            if (!buildFirst)
            {
                _log.AppendLine("Pack cancelled: stale compiled scripts were not rebuilt.");
                StatusText = "Pack cancelled: stale scripts need rebuilding.";
                return false;
            }

            StatusText = "Building all scripts before pack...";
            var buildOutcome = await _compileService.BuildAllAsync().ConfigureAwait(true);
            if (!buildOutcome.Ran)
            {
                _log.AppendLine(
                    "Pack cancelled: stale compiled scripts remain and no script compiler is vendored.");
                StatusText = "Pack cancelled: stale scripts remain.";
                return false;
            }

            var failed = buildOutcome.Failed;

            var remaining = _compileService.ScanStale();
            var remainingWarning = ScriptPackReadiness.Evaluate(remaining);
            if (ScriptPackReadiness.CanPackAfterBuild(failed, remaining))
                return true;

            _log.AppendLine(failed == 0
                ? "Pack cancelled: stale compiled scripts remain after Build All Scripts."
                : $"Pack cancelled: Build All Scripts failed for {failed} script(s).");
            if (remainingWarning != null)
            {
                foreach (var line in remainingWarning.OutputLines)
                    _log.AppendLine($"  {line}");
            }

            StatusText = failed == 0
                ? "Pack cancelled: stale scripts remain."
                : "Pack cancelled: script build failed.";
            return false;
        }

        private bool CanMutateModule() => !IsInteractionBlocked;

        partial void OnIsPackingChanged(bool value)
        {
            NotifyMutationStateChanged();
        }

        partial void OnIsValidationRunningChanged(bool value)
        {
            NotifyMutationStateChanged();
        }

        partial void OnIsBuildingScriptsChanged(bool value)
        {
            NotifyMutationStateChanged();
        }

        partial void OnIsManagingErfArchivesChanged(bool value)
        {
            NotifyMutationStateChanged();
        }

        partial void OnIsManagingFactionsChanged(bool value)
        {
            NotifyMutationStateChanged();
        }

        partial void OnIsGeneratingAreaChanged(bool value)
        {
            NotifyMutationStateChanged();
        }

        private void NotifyMutationStateChanged()
        {
            OnPropertyChanged(nameof(IsModuleMutationLocked));
            // Publish before the local notifications: every other panel and open editor tab learns
            // about the lock from here, and they should all flip in the same frame the menu does.
            // The palette, explorer, validation panel and script tabs all subscribe to it.
            _mutationLock.Set(IsShellModuleMutationLocked);
            NotifyInteractionStateChanged();
        }

        private void OnPublishedMutationStateChanged()
        {
            OnPropertyChanged(nameof(IsModuleMutationLocked));
            NotifyInteractionStateChanged();
        }

        private void NotifyInteractionStateChanged()
        {
            OnPropertyChanged(nameof(IsActiveEditorBusy));
            OnPropertyChanged(nameof(IsInteractionBlocked));
            SaveAllCommand.NotifyCanExecuteChanged();
            ErfArchivesCommand.NotifyCanExecuteChanged();
            FactionEditorCommand.NotifyCanExecuteChanged();
            AreaGeneratorCommand.NotifyCanExecuteChanged();
            PackModuleCommand.NotifyCanExecuteChanged();
            BuildAllScriptsCommand.NotifyCanExecuteChanged();
            ModulePropertiesCommand.NotifyCanExecuteChanged();
            NotifyActiveEditorCommandsChanged();
        }

        /// <summary>
        /// Runs the startup flow: resolve the module root (settings, else auto-detect), open it,
        /// start the background catalog build, and watch the module root for external changes.
        /// Safe to call from the UI thread - all slow work happens on background threads.
        /// </summary>
        public async Task InitializeAsync()
        {
            _log.AppendLine($"Settings loaded from '{ToolsetSettings.SettingsFilePath}'.");

            if (_startupNotice != null)
                _log.AppendLine(_startupNotice.Message);

            var moduleRoot = _settings.ModuleRoot;
            if (string.IsNullOrWhiteSpace(moduleRoot))
            {
                var detected = ToolsetSettings.AutoDetectModuleRoot();
                if (detected != null)
                {
                    moduleRoot = detected;
                    _settings.ModuleRoot = detected;
                }
            }

            if (string.IsNullOrWhiteSpace(moduleRoot) || !Directory.Exists(moduleRoot))
            {
                StatusText = "No module root found. Set ModuleRoot in settings.json and restart.";
                _log.AppendLine("No module root found: auto-detection failed and none is configured.");
                return;
            }

            StatusText = $"Opening module '{moduleRoot}'...";

            try
            {
                // OpenAsync runs all crash recovery and cross-process lease waits on a worker, then
                // resumes on this Avalonia context before replacing the workspace and raising
                // WorkspaceOpened. Subscribers such as EditorService can keep their UI-thread-only
                // collections without making module startup freeze behind another process's lease.
                await _workspaceContext.OpenAsync(moduleRoot);
            }
            catch (Exception ex)
            {
                StatusText = $"Failed to open module: {ex.Message}";
                _log.AppendLine($"Failed to open module root '{moduleRoot}': {ex.Message}");
                return;
            }

            _settings.AddRecentModule(moduleRoot);
            WindowTitle = $"SWLOR Toolset - {Path.GetFileName(Path.GetDirectoryName(moduleRoot)) ?? "Module"}";
            // Areas, dialogs, and scripts require only directory enumeration. Publish them now so the
            // builder can begin work while friendly blueprint names and search data are still indexing.
            _explorer.Initialize();
            _fileWatcher.Watch(moduleRoot);

            var catalog = _workspaceContext.Catalog;
            if (catalog == null)
                return;

            StatusText = $"Module opened. Building catalog (0/{catalog.TotalCount})...";

            _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _progressTimer.Tick += (_, _) =>
            {
                StatusText = $"Building catalog ({catalog.ProcessedCount}/{catalog.TotalCount})...";
            };
            _progressTimer.Start();

            _ = catalog.BuildTask.ContinueWith(task =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _progressTimer?.Stop();
                    _progressTimer = null;

                    // A file-watcher rescan (OnFileWatcherRescanRequested) can replace
                    // _workspaceContext.Catalog with a freshly reopened one while this startup build is
                    // still running. Whichever of the two finishes last would otherwise publish over the
                    // other; only the build for the catalog that is still current may touch Explorer,
                    // Search, the palette or the status line.
                    if (!ReferenceEquals(catalog, _workspaceContext.Catalog))
                        return;

                    // ContinueWith runs on failure too. Announcing "Catalog ready" over a faulted build
                    // invites the builder to trust search and Explorer results that are missing whatever
                    // the fault skipped.
                    if (task.IsFaulted)
                    {
                        var reason = task.Exception?.GetBaseException().Message ?? "unknown error";
                        // The catalog supplies friendly names, not the only route to the module.
                        // Fall back to direct directory enumeration so a partial indexing failure
                        // does not leave both navigation panels permanently empty.
                        _explorer.Initialize();
                        _palette.Refresh();
                        StatusText = $"Catalog build failed: {reason}. Search and Module Contents may be incomplete.";
                        _log.AppendLine($"Catalog build failed: {reason}");
                        return;
                    }
                    _explorer.RefreshFromCatalog(catalog);
                    _search.Refresh();
                    // Explorer is refreshed with friendly names after its immediate directory-backed
                    // population. Palette stays deferred because materializing thousands of tiles on the
                    // UI thread is not part of the time-to-usable path.
                    _palette.Refresh();
                    StatusText = $"Catalog ready: {catalog.Entries.Count} entries indexed.";

                    // Preview controls load artwork as their virtualized cells enter the viewport.
                    // A complete cache warm-up remains available from Tools, but starting all 20,000
                    // renders here competes with the first editor the builder opens and makes its
                    // on-demand previews appear stalled.
                });
            });
        }

        /// <summary>
        /// Reopens the current module root and rebuilds the catalog and its dependent panels after
        /// the file watcher reports a lost event (almost always its native buffer overflowing). Some
        /// create/delete/rename notifications are gone by the time this runs, so patching individual
        /// catalog entries has nothing reliable to patch from - only the same full rescan
        /// <see cref="InitializeAsync"/> performs at startup can be trusted again.
        /// </summary>
        private async void OnFileWatcherRescanRequested()
        {
            if (_isRescanningAfterWatcherOverflow)
            {
                _rescanRequestedWhileRunning = true;
                return;
            }

            var moduleRoot = _workspaceContext.Workspace?.ModuleRoot;
            if (moduleRoot == null)
                return;

            _isRescanningAfterWatcherOverflow = true;
            _log.AppendLine(
                "File watcher lost events (buffer overflow) - rescanning the module to resynchronize the catalog.");
            StatusText = "Resynchronizing after a file watcher overflow...";

            try
            {
                // Recovery and lease waits run on OpenAsync's worker. The continuation resumes on
                // this Avalonia context before WorkspaceOpened is raised, matching startup and
                // preserving EditorService's UI-thread-only subscriber state.
                await _workspaceContext.OpenAsync(moduleRoot);
            }
            catch (Exception ex)
            {
                _isRescanningAfterWatcherOverflow = false;
                var runAgain = _rescanRequestedWhileRunning;
                _rescanRequestedWhileRunning = false;
                StatusText = "Rescan failed - see Output.";
                _log.AppendLine($"Rescan after file watcher overflow failed: {ex.Message}");
                if (runAgain)
                    OnFileWatcherRescanRequested();
                return;
            }

            var catalog = _workspaceContext.Catalog;
            if (catalog == null)
            {
                _isRescanningAfterWatcherOverflow = false;
                var runAgain = _rescanRequestedWhileRunning;
                _rescanRequestedWhileRunning = false;
                if (runAgain)
                    OnFileWatcherRescanRequested();
                return;
            }

            _ = catalog.BuildTask.ContinueWith(task =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    // Reset first, and unconditionally: a future rescan must not stay blocked just
                    // because this build turns out to be superseded below.
                    _isRescanningAfterWatcherOverflow = false;
                    var runAgain = _rescanRequestedWhileRunning;
                    _rescanRequestedWhileRunning = false;

                    // Symmetric with the startup continuation above: if the module root was reopened
                    // again after this rescan started, this build's catalog is no longer
                    // _workspaceContext.Catalog, and publishing it would stomp whatever the newer build
                    // already reported (or is about to).
                    if (!ReferenceEquals(catalog, _workspaceContext.Catalog))
                    {
                        if (runAgain)
                            OnFileWatcherRescanRequested();
                        return;
                    }

                    if (task.IsFaulted)
                    {
                        var reason = task.Exception?.GetBaseException().Message ?? "unknown error";
                        _explorer.Initialize();
                        _palette.Refresh();
                        StatusText = $"Rescan failed: {reason}. Search and Module Contents may be incomplete.";
                        _log.AppendLine($"Rescan after file watcher overflow failed: {reason}");
                        if (runAgain)
                            OnFileWatcherRescanRequested();
                        return;
                    }

                    _explorer.RefreshFromCatalog(catalog);
                    _search.Refresh();
                    _palette.Refresh();
                    StatusText = $"Rescan complete: {catalog.Entries.Count} entries indexed.";
                    _log.AppendLine(
                        $"Rescan after file watcher overflow complete: {catalog.Entries.Count} entries indexed.");
                    if (runAgain)
                        OnFileWatcherRescanRequested();
                });
            });
        }
    }
}
