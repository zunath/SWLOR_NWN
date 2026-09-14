using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Merchants
{
    /// <summary>One placed instance of the merchant, including its owning area and sync state.</summary>
    public sealed record MerchantInstancePlacement(
        string AreaName,
        string AreaResRef,
        string Tag,
        int InstanceIndex,
        int OutOfDateMerchantRecords,
        int OutOfDateItemRecords,
        float XPosition = 0f,
        float YPosition = 0f,
        float ZPosition = 0f)
    {
        public bool IsCurrent => OutOfDateMerchantRecords == 0 && OutOfDateItemRecords == 0;
        public string SyncState => IsCurrent ? "Up to date" : "Out of date";
        public string Status => IsCurrent
            ? "Up to date"
            : $"{RecordCount(OutOfDateMerchantRecords, "merchant")}, " +
              $"{RecordCount(OutOfDateItemRecords, "item")} out of date";

        private static string RecordCount(int count, string kind) =>
            $"{count} {kind} record{(count == 1 ? string.Empty : "s")}";
    }

    /// <summary>
    /// Finds placed stores and updates them with the same canonical expansion used by the CLI.
    /// </summary>
    public sealed class MerchantInstanceService
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly OutputLogService _log;
        private readonly Func<string, bool>? _hasUnsavedAreaInstances;
        private readonly Action<string>? _reloadOpenAreaInstances;

        public MerchantInstanceService(
            WorkspaceContext workspaceContext,
            OutputLogService log,
            Func<string, bool>? hasUnsavedAreaInstances = null,
            Action<string>? reloadOpenAreaInstances = null)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _hasUnsavedAreaInstances = hasUnsavedAreaInstances;
            _reloadOpenAreaInstances = reloadOpenAreaInstances;
        }

        public Task<IReadOnlyList<MerchantInstancePlacement>> FindAsync(string merchantResRef)
        {
            var workspace = _workspaceContext.Workspace;
            var catalog = _workspaceContext.Catalog;
            if (workspace == null)
                return Task.FromResult<IReadOnlyList<MerchantInstancePlacement>>(
                    Array.Empty<MerchantInstancePlacement>());

            return Task.Run(() => Find(workspace, catalog, merchantResRef));
        }

        public async Task<int> UpdateOutOfDateAsync(
            string merchantResRef,
            IReadOnlyCollection<string> targetAreaResRefs)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || targetAreaResRefs.Count == 0)
                return 0;

            // The Placed Instances tab already paid for a module-wide discovery scan. Updating only
            // the areas displayed as out of date avoids reopening every GIT in the module here, and
            // it gives the operation snapshot semantics: Refresh explicitly discovers later placements.
            var availableAreas = workspace.EnumerateAreaResRefs()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var requestedAreas = targetAreaResRefs
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (requestedAreas.Count == 0)
                return 0;
            var missingArea = requestedAreas.FirstOrDefault(area => !availableAreas.Contains(area));
            if (missingArea != null)
            {
                throw new InvalidOperationException(
                    $"Placed area '{missingArea}' changed after the status scan. Refresh the list and try again.");
            }
            var targetAreas = requestedAreas;

            var protectedAreas = _hasUnsavedAreaInstances == null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : targetAreas
                    .Where(_hasUnsavedAreaInstances)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var result = await Task.Run(
                () => Update(workspace, merchantResRef, targetAreas, protectedAreas)).ConfigureAwait(true);
            foreach (var areaResRef in result.Areas)
                _reloadOpenAreaInstances?.Invoke(areaResRef);
            if (result.Count > 0)
            {
                _workspaceContext.InvalidateGitIndexes();
                _workspaceContext.InvalidateScriptUsages();
                _log.AppendLine(
                    $"Updated {result.Count} placed instance{(result.Count == 1 ? string.Empty : "s")} of merchant " +
                    $"'{merchantResRef}'.");
            }

            return result.Count;
        }

        private static IReadOnlyList<MerchantInstancePlacement> Find(
            ModuleWorkspace workspace,
            BlueprintCatalog? catalog,
            string merchantResRef)
        {
            var merchant = JsonGffDocument.Load(
                workspace.GetResourcePath(ResourceType.Utm, merchantResRef));
            var itemCache = new Dictionary<string, JsonGffDocument?>(StringComparer.OrdinalIgnoreCase);
            JsonGffDocument? LoadItem(string resRef) =>
                itemCache.TryGetValue(resRef, out var cached)
                    ? cached
                    : itemCache[resRef] = TryLoadItem(workspace, resRef);

            var placements = new List<MerchantInstancePlacement>();
            foreach (var areaResRef in workspace.EnumerateAreaResRefs())
            {
                GitDocument git;
                try
                {
                    git = workspace.LoadGit(areaResRef);
                }
                catch
                {
                    continue;
                }

                var areaName = ResolveAreaName(workspace, catalog, areaResRef);
                for (var index = 0; index < git.Stores.Count; index++)
                {
                    var store = git.Stores[index];
                    if (!string.Equals(
                            store.GetStringOrNull("ResRef"),
                            merchantResRef,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var status = StoreInstanceSynchronizer.Inspect(
                        merchant,
                        store,
                        merchantResRef,
                        LoadItem);
                    placements.Add(new MerchantInstancePlacement(
                        areaName,
                        areaResRef,
                        store.GetStringOrNull("Tag") ?? string.Empty,
                        index,
                        status.OutOfDateMerchantRecords,
                        status.OutOfDateItemRecords,
                        store.GetOrNull("XPosition")?.GetSingle() ?? 0f,
                        store.GetOrNull("YPosition")?.GetSingle() ?? 0f,
                        store.GetOrNull("ZPosition")?.GetSingle() ?? 0f));
                }
            }

            return placements
                .OrderBy(placement => placement.AreaName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(placement => placement.AreaResRef, StringComparer.OrdinalIgnoreCase)
                .ThenBy(placement => placement.InstanceIndex)
                .ToList();
        }

        private static (int Count, IReadOnlyList<string> Areas) Update(
            ModuleWorkspace workspace,
            string merchantResRef,
            IReadOnlyList<string> targetAreas,
            IReadOnlySet<string> protectedAreas)
        {
            // Synchronization is one read/modify/write operation across the merchant, the selected
            // GITs, and the referenced inventory blueprints. Keep pack/unpack and other writers out
            // for the entire snapshot instead of acquiring only around staged file operations.
            using var moduleWriteLock = ModuleWriteLock.Acquire(workspace.ModuleRoot);
            var merchant = JsonGffDocument.Load(
                workspace.GetResourcePath(ResourceType.Utm, merchantResRef));
            var itemCache = new Dictionary<string, JsonGffDocument?>(StringComparer.OrdinalIgnoreCase);
            JsonGffDocument? LoadItem(string resRef) =>
                itemCache.TryGetValue(resRef, out var cached)
                    ? cached
                    : itemCache[resRef] = TryLoadItem(workspace, resRef);

            var staged = new List<SaveService.StagedWrite>();
            var areas = new List<string>();
            var updated = 0;
            try
            {
                foreach (var areaResRef in targetAreas)
                {
                    var path = Path.Combine(workspace.ModuleRoot, "git", areaResRef + ".git.json");
                    GitDocument git;
                    try
                    {
                        git = GitDocument.Load(path);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Placed area '{areaResRef}' could not be loaded. Refresh the list and try again.",
                            ex);
                    }

                    var replacements = new List<(int Index, JsonGffStruct Expected)>();
                    for (var index = 0; index < git.Stores.Count; index++)
                    {
                        var store = git.Stores[index];
                        if (!string.Equals(
                                store.GetStringOrNull("ResRef"),
                                merchantResRef,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var expected = StoreInstanceSynchronizer.BuildExpected(
                            merchant, store, merchantResRef, LoadItem);
                        if (!StoreInstanceSynchronizer.Equivalent(store, expected))
                            replacements.Add((index, expected));
                    }

                    if (replacements.Count == 0)
                        continue;

                    if (protectedAreas.Contains(areaResRef))
                    {
                        throw new InvalidOperationException(
                            $"Save or revert the unsaved instance edits in area '{areaResRef}' before " +
                            "updating its placed merchant instance.");
                    }

                    using (EditScope.EnterConstruction())
                    {
                        var list = git.Fields.GetOrNull("StoreList")
                                   ?? throw new InvalidDataException(
                                       $"Area '{areaResRef}' has stores but no StoreList field.");
                        foreach (var replacement in replacements.OrderByDescending(value => value.Index))
                        {
                            list.RemoveElementAt(replacement.Index);
                            list.InsertElement(replacement.Index, replacement.Expected);
                        }
                    }

                    staged.Add(SaveService.Stage(path, git.ToBytes()));
                    areas.Add(areaResRef);
                    updated += replacements.Count;
                }

                SaveService.CommitAll(staged);
                return (updated, areas);
            }
            catch
            {
                foreach (var write in staged)
                    SaveService.Discard(write);
                throw;
            }
        }

        private static JsonGffDocument? TryLoadItem(ModuleWorkspace workspace, string resRef)
        {
            try
            {
                return workspace.LoadBlueprint(ResourceType.Uti, resRef).Document;
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveAreaName(
            ModuleWorkspace workspace,
            BlueprintCatalog? catalog,
            string areaResRef)
        {
            if (catalog?.TryGetEntry(ResourceType.Area, areaResRef, out var entry) == true &&
                !string.IsNullOrWhiteSpace(entry.Name))
            {
                return entry.Name!;
            }

            try
            {
                return AreDocument.Load(workspace.GetResourcePath(ResourceType.Area, areaResRef))
                           .Name.Text
                       ?? areaResRef;
            }
            catch
            {
                return areaResRef;
            }
        }
    }
}
