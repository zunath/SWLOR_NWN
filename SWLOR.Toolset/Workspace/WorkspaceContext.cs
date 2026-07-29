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
        public event Action? TagIndexInvalidated;
        public event Action<string>? PaletteChoicesInvalidated;

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
            foreach (var recovered in Services.ErfArchiveService.RecoverInterruptedImports(moduleRoot))
                _log.AppendLine($"Recovered '{recovered}' from an interrupted ERF import.");

            // Before anything reads the folder. A grouped save that was interrupted between moving
            // an original aside and installing its replacement leaves the canonical ARE, GIT, or
            // GIC missing and its only copy sitting beside it under a .save-backup name; opening
            // the area then fails on a file that is, in fact, right there.
            //
            // Deliberately not caught here: if a member of the group cannot be restored (a locked
            // backup or target), RecoverInterruptedSaves throws SaveRecoveryException instead of
            // returning partial success, and that must propagate out of Open so the caller's
            // existing "failed to open" handling refuses the module rather than opening it with an
            // area at mixed ARE/GIT/GIC generations.
            foreach (var recovered in Services.SaveService.RecoverInterruptedSaves(moduleRoot))
                _log.AppendLine($"Recovered '{recovered}' from an interrupted save.");

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

            var tagIndex = Workspace.TagIndex;
            var tagIndexStopwatch = Stopwatch.StartNew();
            _ = tagIndex.GetTransitionDestinationTagsAsync().ContinueWith(task =>
            {
                tagIndexStopwatch.Stop();
                if (task.IsCompletedSuccessfully)
                {
                    _log.AppendLine(
                        $"Transition tag index ready in {tagIndexStopwatch.ElapsedMilliseconds}ms.");
                }
                else if (task.Exception != null)
                {
                    _log.AppendLine(
                        $"Transition tag index failed after {tagIndexStopwatch.ElapsedMilliseconds}ms: " +
                        task.Exception.GetBaseException().Message);
                }
            }, TaskScheduler.Default);

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
        /// The resource kinds <see cref="BlueprintCatalog"/>'s initial build actually indexes - areas
        /// and every blueprint type. Shared by <see cref="RefreshCatalogEntry"/>/
        /// <see cref="RemoveCatalogEntry"/> here and by callers deciding whether to read the catalog or
        /// enumerate the workspace directly (see <c>ModuleExplorerViewModel.IsCatalogIndexed</c>), so
        /// the two can never drift apart.
        /// </summary>
        /// <remarks>
        /// Dialogs and scripts are deliberately excluded. The initial build never indexes them, so
        /// inserting one here on a save/create/external-change event would seed the catalog with
        /// exactly one dialog or script - and Search would then return that single changed resource by
        /// resref while silently omitting every other, unchanged one of the same type until the module
        /// is reopened and the catalog rebuilt from scratch.
        /// </remarks>
        public static bool IsCatalogIndexedType(ResourceType type) =>
            type == ResourceType.Area || ModuleWorkspace.BlueprintTypes.Contains(type);

        /// <summary>
        /// Re-indexes one saved resource and tells catalog-backed panels to refresh immediately.
        /// </summary>
        public void RefreshCatalogEntry(ResourceType type, string resRef)
        {
            InvalidateTagIndexWhenRelevant(type);
            InvalidateScriptUsagesWhenRelevant(type);
            if (IsCatalogIndexedType(type))
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
            if (IsCatalogIndexedType(type))
                Catalog?.RemoveEntry(type, resRef);
            CatalogEntryRefreshed?.Invoke(type, resRef);
        }

        /// <summary>
        /// Drops the lazy transition-tag lookup after a paired GIT file changes. GIT is not a
        /// first-class <see cref="ResourceType"/>, so the file watcher calls this directly.
        /// </summary>
        public void InvalidateTagIndex()
        {
            Workspace?.TagIndex.Invalidate();
            TagIndexInvalidated?.Invoke();
        }

        /// <summary>
        /// Drops the lazy script-usage snapshot. Paired GIT files are not first-class resource types,
        /// so the file watcher calls this directly when a placed-instance script slot changes.
        /// </summary>
        public void InvalidateScriptUsages() => ScriptUsagesInvalidated?.Invoke();

        /// <summary>
        /// Tells behavior editors that one module ITP changed and any materialized category choices
        /// from that palette must be rebuilt.
        /// </summary>
        public void InvalidatePaletteChoices(string paletteResRef)
        {
            if (!string.IsNullOrWhiteSpace(paletteResRef))
                PaletteChoicesInvalidated?.Invoke(paletteResRef);
        }

        private void InvalidateTagIndexWhenRelevant(ResourceType type)
        {
            if (type is ResourceType.Area or ResourceType.Utd or ResourceType.Utw or ResourceType.Uti)
                InvalidateTagIndex();
        }

        private void InvalidateScriptUsagesWhenRelevant(ResourceType type)
        {
            if (Domain.Script.ScriptUsageIndex.ScriptedTypes.Contains(type))
                InvalidateScriptUsages();
        }
    }
}
