using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
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
        private DispatcherTimer? _progressTimer;

        [ObservableProperty]
        private IRootDock? _layout;

        [ObservableProperty]
        private string _statusText = "Starting...";

        private readonly Editors.EditorService _editorService;
        private readonly PackService _packService;

        public ShellViewModel(
            ToolsetSettings settings,
            WorkspaceContext workspaceContext,
            OutputLogService log,
            ModuleFileWatcher fileWatcher,
            ModuleExplorerViewModel explorer,
            SearchViewModel search,
            ToolsetDockFactory factory,
            Editors.EditorService editorService,
            PackService packService)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
            _explorer = explorer ?? throw new ArgumentNullException(nameof(explorer));
            _search = search ?? throw new ArgumentNullException(nameof(search));
            _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
            _packService = packService ?? throw new ArgumentNullException(nameof(packService));

            if (factory == null) throw new ArgumentNullException(nameof(factory));

            Layout = factory.CreateLayout();
            if (Layout != null)
                factory.InitLayout(Layout);
        }

        [RelayCommand]
        private async Task SaveAll()
        {
            var saved = await _editorService.SaveAllAsync().ConfigureAwait(true);
            StatusText = saved ? "All open editors saved." : "Save cancelled or failed - see Output.";
        }

        /// <summary>Returns true when the main window may close after handling unsaved editors.</summary>
        public Task<bool> TryCloseAsync() => _editorService.TryPrepareApplicationCloseAsync();

        [RelayCommand]
        private async Task PackModuleAsync()
        {
            var moduleRoot = _workspaceContext.Workspace?.ModuleRoot;
            if (moduleRoot == null)
            {
                _log.AppendLine("No module open to pack.");
                return;
            }

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
                    StatusText = $"Catalog ready: {catalog.Entries.Count} entries indexed.";
                });
            });
        }
    }
}
