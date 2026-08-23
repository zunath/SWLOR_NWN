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
        public event Action? CatalogBuildCompleted;
        /// <summary>
        /// Raised for every saved, reloaded, created, or removed resource so content-dependent
        /// caches can invalidate even when its catalog Name/Tag did not change.
        /// </summary>
        public event Action<ResourceType, string>? CatalogEntryRefreshed;
        /// <summary>
        /// Raised only when the ordered catalog's indexed metadata or membership actually changed.
        /// Explorer and Search subscribe here so an ordinary content-only save does not make them
        /// regroup and requery the entire catalog.
        /// </summary>
        public event Action<ResourceType, string>? CatalogEntriesChanged;
        public event Action? PlacementIndexInvalidated;
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
            // A logical resource delete can span several files (and an area's IFO registration).
            // Restore an interrupted transaction before any workspace enumeration can observe only
            // the companions that had not moved when the prior process exited.
            foreach (var recovered in Services.ModuleResourceDeletionService
                         .RecoverInterruptedDeletes(moduleRoot))
            {
                _log.AppendLine($"Recovered {recovered} from an interrupted delete.");
            }

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

            // A ResRef rename may contain one of the grouped GIT saves recovered above. Restore that
            // inner transaction first, then the outer rename transaction can reliably see either
            // every original companion or every installed companion instead of a half-moved set.
            foreach (var recovered in Services.ItemRenameRecovery.RecoverInterruptedRenames(moduleRoot))
                _log.AppendLine($"Recovered '{recovered}' from an interrupted blueprint rename.");

            foreach (var recovered in Services.ErfArchiveService.RecoverInterruptedImports(moduleRoot))
                _log.AppendLine($"Recovered '{recovered}' from an interrupted ERF import.");

            var openStopwatch = Stopwatch.StartNew();
            var replacementWorkspace = _workspaceFactory(moduleRoot);
            Workspace?.PlacementIndex.Invalidate();
            Workspace = replacementWorkspace;
            Catalog = null;
            openStopwatch.Stop();
            _log.AppendLine($"Opened module root '{moduleRoot}' in {openStopwatch.ElapsedMilliseconds}ms.");

            var placementIndex = Workspace.PlacementIndex;
            placementIndex.AreaReadFailed += (areaResRef, ex) =>
                _log.AppendLine(
                    $"Placement index area read failed. AreaResRef='{areaResRef}'. Exception={ex}");
            var placementIndexStopwatch = Stopwatch.StartNew();
            _ = placementIndex.WarmAsync().ContinueWith(task =>
            {
                placementIndexStopwatch.Stop();
                if (task.IsCompletedSuccessfully)
                {
                    _log.AppendLine(
                        "Placement index ready. " +
                        $"DurationMs={placementIndexStopwatch.ElapsedMilliseconds}.");
                }
                else if (task.IsCanceled)
                {
                    _log.AppendLine(
                        "Placement index warm-up canceled because its snapshot was invalidated. " +
                        $"DurationMs={placementIndexStopwatch.ElapsedMilliseconds}.");
                }
                else if (task.Exception != null)
                {
                    _log.AppendLine(
                        "Placement index warm-up failed. " +
                        $"DurationMs={placementIndexStopwatch.ElapsedMilliseconds}. " +
                        $"Exception={task.Exception.GetBaseException()}");
                }
            }, TaskScheduler.Default);

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
                    if (ReferenceEquals(catalog, Catalog))
                        CatalogBuildCompleted?.Invoke();
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
            var catalogChanged = false;
            if (IsCatalogIndexedType(type) && Catalog is { } catalog)
                catalog.RefreshEntry(type, resRef, out catalogChanged);
            CatalogEntryRefreshed?.Invoke(type, resRef);
            if (catalogChanged)
                CatalogEntriesChanged?.Invoke(type, resRef);
        }

        /// <summary>
        /// Drops one deleted resource from the catalog and tells catalog-backed panels to refresh.
        /// Without this, Explorer and Search keep offering a resource whose file is gone, and opening
        /// that row fails against the missing file.
        /// </summary>
        public void RemoveCatalogEntry(ResourceType type, string resRef)
        {
            InvalidateTagIndexWhenRelevant(type);
            // Removing an area also removes every placement in its paired GIT. Ordinary ARE saves
            // do not affect placements and deliberately avoid this module-wide rebuild.
            if (type == ResourceType.Area)
                InvalidatePlacementIndex();
            InvalidateScriptUsagesWhenRelevant(type);
            var catalogChanged = false;
            if (IsCatalogIndexedType(type) && Catalog is { } catalog)
                catalogChanged = catalog.RemoveEntry(type, resRef);
            CatalogEntryRefreshed?.Invoke(type, resRef);
            if (catalogChanged)
                CatalogEntriesChanged?.Invoke(type, resRef);
        }

        /// <summary>
        /// Drops the lazy transition-tag lookup after a resource that contributes behavior tags
        /// changes. Blueprint and ARE changes affect tags without changing any placed-instance row,
        /// so placement invalidation is deliberately separate.
        /// </summary>
        public void InvalidateTagIndex()
        {
            Workspace?.TagIndex.Invalidate();
            TagIndexInvalidated?.Invoke();
        }

        /// <summary>
        /// Drops every index whose source is a paired GIT file. GIT is not a first-class
        /// <see cref="ResourceType"/>, so file-watcher, import, and merchant-update paths call this
        /// explicitly instead of routing through catalog refresh.
        /// </summary>
        public void InvalidateGitIndexes()
        {
            InvalidateTagIndex();
            InvalidatePlacementIndex();
            InvalidateScriptUsages();
        }

        /// <summary>
        /// Drops the module-wide placement snapshot and tells open Source tabs to reload it.
        /// </summary>
        public void InvalidatePlacementIndex()
        {
            if (Workspace == null)
                return;

            Workspace.PlacementIndex.Invalidate();
            PlacementIndexInvalidated?.Invoke();
        }

        /// <summary>
        /// Drops the lazy script-usage snapshot. Paired GIT files are not first-class resource types,
        /// so their grouped invalidation routes here directly when a placed-instance script slot changes.
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
            {
                Workspace?.TagIndex.Invalidate();
                TagIndexInvalidated?.Invoke();
            }
        }

        private void InvalidateScriptUsagesWhenRelevant(ResourceType type)
        {
            if (Domain.Script.ScriptUsageIndex.ScriptedTypes.Contains(type))
                InvalidateScriptUsages();
        }
    }
}
