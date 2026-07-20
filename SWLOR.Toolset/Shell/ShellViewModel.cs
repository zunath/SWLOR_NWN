using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Controls;
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
        private DispatcherTimer? _progressTimer;

        [ObservableProperty]
        private IRootDock? _layout;

        [ObservableProperty]
        private string _statusText = "Starting...";

        public ShellViewModel(
            ToolsetSettings settings,
            WorkspaceContext workspaceContext,
            OutputLogService log,
            ModuleFileWatcher fileWatcher,
            ModuleExplorerViewModel explorer,
            ToolsetDockFactory factory)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
            _explorer = explorer ?? throw new ArgumentNullException(nameof(explorer));

            if (factory == null) throw new ArgumentNullException(nameof(factory));

            Layout = factory.CreateLayout();
            if (Layout != null)
                factory.InitLayout(Layout);
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
                    StatusText = $"Catalog ready: {catalog.Entries.Count} entries indexed.";
                });
            });
        }
    }
}
