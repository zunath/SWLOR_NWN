using System.ComponentModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
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

        public bool IsModuleMutationLocked => IsPacking || IsValidationRunning;

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
        private readonly Editors.EditorService _editorService;
        private readonly PackService _packService;
        private readonly ScriptCompileService? _compileService;
        private readonly ScriptReferenceViewModel? _scriptReference;
        private readonly ProblemsViewModel? _problems;

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
            Editors.EditorService editorService,
            PackService packService,
            OutputViewModel output,
            ValidationViewModel validation,
            ThumbnailService thumbnails,
            ScriptCompileService? compileService = null,
            ScriptReferenceViewModel? scriptReference = null,
            ProblemsViewModel? problems = null,
            StartupNotice? startupNotice = null)
        {
            _startupNotice = startupNotice;
            _compileService = compileService;
            _scriptReference = scriptReference;
            _problems = problems;
            _thumbnails = thumbnails ?? throw new ArgumentNullException(nameof(thumbnails));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
            _explorer = explorer ?? throw new ArgumentNullException(nameof(explorer));
            _search = search ?? throw new ArgumentNullException(nameof(search));
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            Display = display ?? throw new ArgumentNullException(nameof(display));
            _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
            _packService = packService ?? throw new ArgumentNullException(nameof(packService));
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
                    _editorService.NavigateToScriptLine(row.ResRef, row.Diagnostic.Line);

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

            // The right dock follows the tab: the Palette lists the front AREA's tileset, so it has
            // nothing to say while a script is in front, and Script Reference has nothing to say
            // while an area is. Insert-at-cursor is retargeted at the same moment.
            var script = document as Editors.ScriptEditorViewModel;
            _activeScript = script;
            _scriptReference?.SetInsertTarget(script != null
                ? text => script.InsertAtCursorRequested?.Invoke(text)
                : null);
            _factory.ShowRightTool(script != null);

            NotifyActiveEditorCommandsChanged();
            OnPropertyChanged(nameof(StatusDetail));
            CompileScriptCommand.NotifyCanExecuteChanged();
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
            NotifyActiveEditorCommandsChanged();
            OnPropertyChanged(nameof(StatusDetail));
        }

        private void NotifyActiveEditorCommandsChanged()
        {
            SaveCommand.NotifyCanExecuteChanged();
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

        private bool CanSave() => !IsModuleMutationLocked && _activeEditor != null;

        [RelayCommand(CanExecute = nameof(CanMutateModule))]
        private async Task SaveAll()
        {
            var saved = await _editorService.SaveAllAsync().ConfigureAwait(true);
            StatusText = saved ? "All open editors saved." : "Save cancelled or failed - see Output.";
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo() => _activeEditor?.Undo();

        private bool CanUndo() => !IsModuleMutationLocked && _activeEditor?.CanUndo == true;

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo() => _activeEditor?.Redo();

        private bool CanRedo() => !IsModuleMutationLocked && _activeEditor?.CanRedo == true;

        /// <summary>
        /// The window title, which names the module rather than just the program - a builder often has
        /// more than one module checked out and the title bar is the only place that distinguishes them.
        /// </summary>
        [ObservableProperty]
        private string _windowTitle = "SWLOR Toolset";

        [RelayCommand]
        private void FocusExplorer() => _factory.Focus(_explorer);

        [RelayCommand]
        private void FocusPalette() => _factory.Focus(_palette);

        [RelayCommand]
        private void FocusOutput() => _factory.Focus(_output);

        [RelayCommand]
        private void FocusValidation() => _factory.Focus(_validation);

        // ----- scripts -----

        private bool CanCompileScript => _activeScript != null && _compileService?.IsAvailable == true;

        /// <summary>Compiles the script in front. Joins the Build menu beside Pack Module.</summary>
        [RelayCommand(CanExecute = nameof(CanCompileScript))]
        private async Task CompileScript()
        {
            if (_activeScript == null || _compileService == null)
                return;

            // Compiling the file on disk, so unsaved work would silently not be built.
            if (!await _activeScript.TrySaveAsync().ConfigureAwait(true))
            {
                StatusText = "Compile cancelled: the script could not be saved.";
                return;
            }

            var resRef = Path.GetFileNameWithoutExtension(_activeScript.FilePath);
            StatusText = $"Compiling {resRef}...";
            _factory.Focus(_output);

            var outcome = await _compileService.CompileAsync(resRef).ConfigureAwait(true);
            StatusText = outcome.Succeeded
                ? $"Compiled {resRef}."
                : $"Could not compile {resRef} - see Problems.";
        }

        /// <summary>Compiles every entry-point script in the module.</summary>
        [RelayCommand]
        private async Task BuildAllScripts()
        {
            if (_compileService == null)
                return;

            StatusText = "Building all scripts...";
            _factory.Focus(_output);

            var (compiled, failed) = await _compileService.BuildAllAsync().ConfigureAwait(true);
            StatusText = failed == 0
                ? $"Built {compiled} script(s)."
                : $"Built {compiled} script(s); {failed} failed - see Output.";
        }

        /// <summary>
        /// Reports compiled scripts that would ship stale. Deliberately a Validation result rather
        /// than a Problem: the code is fine, the build artifact is not.
        /// </summary>
        [RelayCommand]
        private void CheckScriptStaleness()
        {
            if (_compileService == null)
                return;

            var stale = _compileService.ScanStale();
            _factory.Focus(_validation);

            if (stale.Count == 0)
            {
                _log.AppendLine("All compiled scripts are up to date.");
                StatusText = "All compiled scripts are up to date.";
                return;
            }

            foreach (var entry in stale)
                _log.AppendLine($"  {entry.Describe()}");

            _log.AppendLine($"{stale.Count} script(s) would ship stale. Build ▸ Build All Scripts fixes this.");
            StatusText = $"{stale.Count} script(s) need recompiling.";
        }

        [RelayCommand]
        private void About() =>
            StatusText = "SWLOR Toolset - an Aurora replacement for area, instance and blueprint editing.";

        /// <summary>
        /// True while previews are being rendered. Deliberately not part of
        /// <see cref="IsModuleMutationLocked"/>: the build only reads blueprints, so the builder keeps
        /// working through it - it just must not be started twice at once.
        /// </summary>
        [ObservableProperty]
        private bool _isBuildingPreviewCache;

        [RelayCommand(CanExecute = nameof(CanBuildPreviewCache))]
        private Task BuildPreviewCacheAsync() => RunPreviewCacheBuildAsync(fromScratch: false);

        [RelayCommand(CanExecute = nameof(CanBuildPreviewCache))]
        private Task RebuildPreviewCacheAsync() => RunPreviewCacheBuildAsync(fromScratch: true);

        private bool CanBuildPreviewCache() => !IsBuildingPreviewCache && _thumbnails.IsAvailable;

        partial void OnIsBuildingPreviewCacheChanged(bool value)
        {
            BuildPreviewCacheCommand.NotifyCanExecuteChanged();
            RebuildPreviewCacheCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Renders every missing palette preview and stores it on disk. Runs in the background with
        /// progress in the Output pane, the same way the catalog build reports itself.
        /// </summary>
        private async Task RunPreviewCacheBuildAsync(bool fromScratch)
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
                if (fromScratch)
                {
                    var removed = await Task.Run(_thumbnails.ClearCache).ConfigureAwait(true);
                    _log.AppendLine($"Preview cache: cleared {removed} cached file(s).");
                }

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
            if (IsModuleMutationLocked)
            {
                StatusText = "Wait for the active module operation to finish before closing.";
                return Task.FromResult(false);
            }

            return _editorService.TryPrepareApplicationCloseAsync();
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

            IsPacking = true;
            try
            {
                if (!await _editorService.SaveAllAsync().ConfigureAwait(true))
                {
                    StatusText = "Pack cancelled because an open editor could not be saved.";
                    _log.AppendLine("Pack aborted: one or more open editors were not saved.");
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

        private bool CanMutateModule() => !IsModuleMutationLocked;

        partial void OnIsPackingChanged(bool value)
        {
            NotifyMutationStateChanged();
        }

        partial void OnIsValidationRunningChanged(bool value)
        {
            NotifyMutationStateChanged();
        }

        private void NotifyMutationStateChanged()
        {
            OnPropertyChanged(nameof(IsModuleMutationLocked));
            // The palette writes directly to the module, so its own write controls follow this lock.
            _palette.NotifyWriteAvailabilityChanged();
            SaveAllCommand.NotifyCanExecuteChanged();
            PackModuleCommand.NotifyCanExecuteChanged();
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
                await Task.Run(() => _workspaceContext.Open(moduleRoot)).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                StatusText = $"Failed to open module: {ex.Message}";
                _log.AppendLine($"Failed to open module root '{moduleRoot}': {ex.Message}");
                return;
            }

            _settings.AddRecentModule(moduleRoot);
            WindowTitle = $"SWLOR Toolset - {Path.GetFileName(Path.GetDirectoryName(moduleRoot)) ?? "Module"}";
            _explorer.Initialize();
            _palette.Refresh();
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

                    // ContinueWith runs on failure too. Announcing "Catalog ready" over a faulted build
                    // invites the builder to trust search and Explorer results that are missing whatever
                    // the fault skipped.
                    if (task.IsFaulted)
                    {
                        var reason = task.Exception?.GetBaseException().Message ?? "unknown error";
                        StatusText = $"Catalog build failed: {reason}. Search and Module Contents may be incomplete.";
                        _log.AppendLine($"Catalog build failed: {reason}");
                        return;
                    }
                    _explorer.RefreshFromCatalog(catalog);
                    _search.Refresh();
                    // Names for the palette tiles come from the catalog, so it only reads properly
                    // once the background build has published them.
                    _palette.Refresh();
                    StatusText = $"Catalog ready: {catalog.Entries.Count} entries indexed.";

                    // Previews last: it is the longest job and the only one the builder can work
                    // through, so it starts once everything they might click is already usable.
                    _ = RunPreviewCacheBuildAsync(fromScratch: false);
                });
            });
        }
    }
}
