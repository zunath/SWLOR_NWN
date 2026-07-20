using System.Diagnostics;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// Holds the currently open <see cref="ModuleWorkspace"/> and its background-building
    /// <see cref="BlueprintCatalog"/>, and raises <see cref="WorkspaceOpened"/> when a module is
    /// (re)opened so panels built before a module root was known (or before a different one is
    /// opened later) can refresh themselves. A thin app-layer wrapper - all the actual
    /// enumeration/parsing logic lives in the Domain project.
    /// </summary>
    public sealed class WorkspaceContext
    {
        private readonly Func<string, ModuleWorkspace> _workspaceFactory;
        private readonly OutputLogService _log;

        public ModuleWorkspace? Workspace { get; private set; }
        public BlueprintCatalog? Catalog { get; private set; }

        public event Action? WorkspaceOpened;

        public WorkspaceContext(Func<string, ModuleWorkspace> workspaceFactory, OutputLogService log)
        {
            _workspaceFactory = workspaceFactory ?? throw new ArgumentNullException(nameof(workspaceFactory));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Opens the module root, timing the open, then kicks off the background catalog build
        /// (also timed, with progress logged periodically). Returns once the workspace itself is
        /// open - the catalog build continues in the background after this method returns.
        /// </summary>
        public void Open(string moduleRoot)
        {
            var openStopwatch = Stopwatch.StartNew();
            Workspace = _workspaceFactory(moduleRoot);
            openStopwatch.Stop();
            _log.AppendLine($"Opened module root '{moduleRoot}' in {openStopwatch.ElapsedMilliseconds}ms.");

            var catalogStopwatch = Stopwatch.StartNew();
            var lastLoggedPercent = -1;

            Catalog = new BlueprintCatalog(Workspace, (processed, total) =>
            {
                if (total <= 0)
                    return;

                var percent = processed * 100 / total;
                if (percent == lastLoggedPercent || percent % 20 != 0)
                    return;

                lastLoggedPercent = percent;
                _log.AppendLine($"Catalog build: {processed}/{total} ({percent}%).");
            });

            Catalog.BuildTask.ContinueWith(_ =>
            {
                catalogStopwatch.Stop();
                _log.AppendLine(
                    $"Catalog build complete: {Catalog.Entries.Count} entries in {catalogStopwatch.ElapsedMilliseconds}ms.");
            });

            WorkspaceOpened?.Invoke();
        }
    }
}
