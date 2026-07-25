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
        private readonly WorkspaceContext _workspaceContext;
        private readonly OutputLogService _log;
        private readonly ModuleFileWatcher _fileWatcher;
        private readonly ModuleExplorerViewModel _explorer;
        private readonly SearchViewModel _search;
        private readonly PaletteViewModel _palette;
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

        private readonly Editors.EditorService _editorService;
        private readonly PackService _packService;

        public ShellViewModel(
            ToolsetSettings settings,
            WorkspaceContext workspaceContext,
            OutputLogService log,
            ModuleFileWatcher fileWatcher,
            ModuleExplorerViewModel explorer,
            SearchViewModel search,
            PaletteViewModel palette,
            ToolsetDockFactory factory,
            Editors.EditorService editorService,
            PackService packService,
            ValidationViewModel validation)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
            _explorer = explorer ?? throw new ArgumentNullException(nameof(explorer));
            _search = search ?? throw new ArgumentNullException(nameof(search));
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
            _packService = packService ?? throw new ArgumentNullException(nameof(packService));
            ArgumentNullException.ThrowIfNull(validation);
            IsValidationRunning = validation.IsRunning;
            validation.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ValidationViewModel.IsRunning))
                    IsValidationRunning = validation.IsRunning;
            };

            if (factory == null) throw new ArgumentNullException(nameof(factory));

            factory.ActiveDocumentChanged += SetActiveEditor;

            Layout = factory.CreateLayout();
            if (Layout != null)
                factory.InitLayout(Layout);
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

            NotifyActiveEditorCommandsChanged();
            OnPropertyChanged(nameof(StatusDetail));
        }

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

            _ = catalog.BuildTask.ContinueWith(_ =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _progressTimer?.Stop();
                    _progressTimer = null;
                    _explorer.RefreshFromCatalog(catalog);
                    _search.Refresh();
                    // Names for the palette tiles come from the catalog, so it only reads properly
                    // once the background build has published them.
                    _palette.Refresh();
                    StatusText = $"Catalog ready: {catalog.Entries.Count} entries indexed.";
                });
            });
        }
    }
}
