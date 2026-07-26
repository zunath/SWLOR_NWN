using System.Diagnostics;
using SWLOR.Toolset.Domain.GameData.Tlk;
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
        private readonly TlkService? _tlkService;

        public ModuleWorkspace? Workspace { get; private set; }
        public BlueprintCatalog? Catalog { get; private set; }

        public event Action? WorkspaceOpened;
        public event Action<ResourceType, string>? CatalogEntryRefreshed;
        public event Action? ScriptUsagesInvalidated;

        public WorkspaceContext(
            Func<string, ModuleWorkspace> workspaceFactory,
            OutputLogService log,
            TlkService? tlkService = null)
        {
            _workspaceFactory = workspaceFactory ?? throw new ArgumentNullException(nameof(workspaceFactory));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _tlkService = tlkService;
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

            var catalog = new BlueprintCatalog(
                Workspace,
                (processed, total) =>
                {
                    if (total <= 0)
                        return;

                    var percent = processed * 100 / total;
                    if (percent == lastLoggedPercent || percent % 20 != 0)
                        return;

                    lastLoggedPercent = percent;
                    _log.AppendLine($"Catalog build: {processed}/{total} ({percent}%).");
                },
                _tlkService == null ? null : _tlkService.GetString);
            Catalog = catalog;

            _ = catalog.BuildTask.ContinueWith(task =>
            {
                catalogStopwatch.Stop();
                if (task.IsCompletedSuccessfully)
                {
                    _log.AppendLine(
                        $"Catalog build complete: {catalog.Entries.Count} entries in {catalogStopwatch.ElapsedMilliseconds}ms.");
                }
                else if (task.Exception != null)
                {
                    _log.AppendLine(
                        $"Catalog build failed after {catalogStopwatch.ElapsedMilliseconds}ms: " +
                        task.Exception.GetBaseException().Message);
                }
            }, TaskScheduler.Default);

            WorkspaceOpened?.Invoke();
            InvalidateScriptUsages();
        }

        /// <summary>
        /// Re-indexes one saved resource and tells catalog-backed panels to refresh immediately.
        /// </summary>
        public void RefreshCatalogEntry(ResourceType type, string resRef)
        {
            InvalidateTagIndexWhenRelevant(type);
            InvalidateScriptUsagesWhenRelevant(type);
            Catalog?.RefreshEntry(type, resRef);
            CatalogEntryRefreshed?.Invoke(type, resRef);
        }

        /// <summary>
        /// Drops one deleted resource from the catalog and tells catalog-backed panels to refresh.
        /// Without this, Explorer and Search keep offering a resource whose file is gone, and opening
        /// that row fails against the missing file.
        /// </summary>
        public void RemoveCatalogEntry(ResourceType type, string resRef)
        {
            InvalidateTagIndexWhenRelevant(type);
            InvalidateScriptUsagesWhenRelevant(type);
            Catalog?.RemoveEntry(type, resRef);
            CatalogEntryRefreshed?.Invoke(type, resRef);
        }

        /// <summary>
        /// Drops the lazy transition-tag lookup after a paired GIT file changes. GIT is not a
        /// first-class <see cref="ResourceType"/>, so the file watcher calls this directly.
        /// </summary>
        public void InvalidateTagIndex() => Workspace?.TagIndex.Invalidate();

        /// <summary>
        /// Drops the lazy script-usage snapshot. Paired GIT files are not first-class resource types,
        /// so the file watcher calls this directly when a placed-instance script slot changes.
        /// </summary>
        public void InvalidateScriptUsages() => ScriptUsagesInvalidated?.Invoke();

        private void InvalidateTagIndexWhenRelevant(ResourceType type)
        {
            if (type is ResourceType.Area or ResourceType.Utd or ResourceType.Utw)
                InvalidateTagIndex();
        }

        private void InvalidateScriptUsagesWhenRelevant(ResourceType type)
        {
            if (Domain.Script.ScriptUsageIndex.ScriptedTypes.Contains(type))
                InvalidateScriptUsages();
        }
    }
}
